public interface IAccountService
{
    Task RegistrAsync(RegisterRequest registerRequest);
    Task LoginAsync(LoginRequest loginRequest);
    Task RefreshTokenAsync(string? refreshToken);
}