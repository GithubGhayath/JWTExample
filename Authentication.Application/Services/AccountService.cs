using Authentication.Domain.Entities;
using Microsoft.AspNetCore.Identity;

// This service handles user registration, login, and token management.
// It connects ASP.NET Core Identity (UserManager) with your custom token logic (IAuthTokenProcessor).
public class AccountService : IAccountService
{
    private readonly IAuthTokenProcessor _AuthTokenProcessor; // Handles JWT creation, cookies, and refresh logic.
    private readonly UserManager<User> _UserManager;          // Identity class that manages users in the database.

    private readonly IUserRepository _userRepository;
    // The constructor injects both dependencies via dependency injection.
    public AccountService(IAuthTokenProcessor AuthTokenProcessor, UserManager<User> UserManager,
    IUserRepository userRepository)
    {
        _AuthTokenProcessor = AuthTokenProcessor;
        _UserManager = UserManager;
        _userRepository = userRepository;
    }

    // ---------------------------
    //      REGISTER NEW USER
    // ---------------------------
    public async Task RegistrAsync(RegisterRequest registerRequest)
    {
        // 1️ Check if user with this email already exists in the database.
        var UserExists = await _UserManager.FindByEmailAsync(registerRequest.Email) != null;

        if (UserExists)
        {
            // If user exists, throw a custom exception instead of creating a duplicate.
            throw new UserAlreadyExistsException(registerRequest.Email);
        }

        // 2️ Create a new User entity using the static factory method from the User class.
        var user = User.Create(registerRequest.Email, registerRequest.FirstName, registerRequest.LastName);

        // 3️ Hash the plain-text password using ASP.NET Identity's built-in PasswordHasher.
        user.PasswordHash = _UserManager.PasswordHasher.HashPassword(user, registerRequest.Password);

        // 4️ Save the new user to the database.
        var result = await _UserManager.CreateAsync(user);

        // 5️ If the save operation failed, throw an exception with detailed Identity error messages.
        if (!result.Succeeded)
        {
            throw new RegistrationFaildException(result.Errors.Select(x => x.Description));
        }
    }

    // ---------------------------
    //      LOGIN USER
    // ---------------------------
    public async Task LoginAsync(LoginRequest loginRequest)
    {
        // 1️ Try to find the user by email.
        var user = await _UserManager.FindByEmailAsync(loginRequest.Email);

        // 2️ If user not found or password invalid ==> fail login.
        if (user == null || !await _UserManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            throw new LoginFailedException(loginRequest.Email);
        }

        // 3️ Generate a new JWT access token and a refresh token.
        var NewToken = _AuthTokenProcessor.GenerateJwtToken(user);
        var RefreshToken = _AuthTokenProcessor.GenerateRefreshToken();

        // 4️ Save refresh token and its expiration date in the database.
        var RefreshTokenExpirationDateInUtc = DateTime.UtcNow.AddDays(7);
        user.RefreshToken = RefreshToken;
        user.RefreshTokenExpiresAtUTC = RefreshTokenExpirationDateInUtc;
        await _UserManager.UpdateAsync(user);

        // 5️ Send both tokens back to the client as HttpOnly cookies (secure, not accessible via JS).
        _AuthTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", NewToken.JwtToken, NewToken.expiresAtUtc);
        _AuthTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, RefreshTokenExpirationDateInUtc);
    }

    // ---------------------------
    //  REFRESH TOKEN
    // ---------------------------
   public async Task RefreshTokenAsync(string? refreshToken)
{
    // 1️ Check that a refresh token was provided by the client.
    if (string.IsNullOrEmpty(refreshToken))
    {
        throw new RefreshTokenException("Refresh token is missing!");
    }

    // 2️ Try to find the user who owns this refresh token in the database.
    var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);

    // If no user matches, the token is invalid.
    if (user == null)
    {
        throw new RefreshTokenException("Unable to retrieve user for refresh token");
    }

    // 3️ Ensure the refresh token hasn't expired.
    if (user.RefreshTokenExpiresAtUTC < DateTime.UtcNow)
    {
        throw new RefreshTokenException("Refresh token is expired.");
    }

    // 4️ Create new access (JWT) and refresh tokens.
    var NewToken = _AuthTokenProcessor.GenerateJwtToken(user);
    var RefreshToken = _AuthTokenProcessor.GenerateRefreshToken();

    // 5️ Save the new refresh token and expiration in the database.
    var RefreshTokenExpirationDateInUtc = DateTime.UtcNow.AddDays(7);
    user.RefreshToken = RefreshToken;
    user.RefreshTokenExpiresAtUTC = RefreshTokenExpirationDateInUtc;
    await _UserManager.UpdateAsync(user);

    // 6️ Write the new tokens as secure HttpOnly cookies.
    // ACCESS_TOKEN is the short-lived JWT, REFRESH_TOKEN is the longer-lived backup token.
    _AuthTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", NewToken.JwtToken, NewToken.expiresAtUtc);
    _AuthTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, RefreshTokenExpirationDateInUtc);
}

}
