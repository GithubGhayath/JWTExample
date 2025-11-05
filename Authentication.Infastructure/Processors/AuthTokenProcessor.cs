using System.IdentityModel.Tokens.Jwt; // Provides classes for creating and handling JWTs (JSON Web Tokens)
using System.Security.Claims; // Used for creating user identity claims (like email, ID, etc.)
using System.Security.Cryptography; // Used for generating secure random numbers (e.g., refresh tokens)
using System.Text; // Used for encoding strings into bytes
using Authentication.Domain.Entities; // Refers to your User entity
using Microsoft.AspNetCore.Http; // Used for working with HTTP context (like cookies)
using Microsoft.Extensions.Options; // Used for injecting configuration options
using Microsoft.IdentityModel.Tokens; // Provides classes for signing and validating JWTs

// This class handles creation of JWT and refresh tokens, 
// and writes tokens to secure cookies for authentication.
public class AuthTokenProcessor: IAuthTokenProcessor
{
    private readonly IHttpContextAccessor _HttpContextAccessor; // Gives access to the current HTTP request/response context
    private readonly JwtOptions _JwtOptions; // Holds your JWT settings from appsettings.json

    // Constructor — gets JwtOptions (from DI) and HttpContextAccessor (for cookies)
    public AuthTokenProcessor(IOptions<JwtOptions> jwtOptions, IHttpContextAccessor httpContextAccessor)
    {
        this._JwtOptions = jwtOptions.Value;
        this._HttpContextAccessor = httpContextAccessor;
    }

    // Generates a JWT token for a specific user and returns it along with its expiration time
    public (string JwtToken, DateTime expiresAtUtc) GenerateJwtToken(User user)
    {
        // Create a key from your secret to sign the token
        var SigninKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_JwtOptions.Secret));

        // Define how the token should be signed (algorithm + key)
        var Credentials = new SigningCredentials(SigninKey, SecurityAlgorithms.HmacSha256);

        // Define claims (info about the user)
        var Claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()), // Subject = user ID
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()), // Unique token ID
            new Claim(JwtRegisteredClaimNames.Email,user.Email), // User email
            new Claim(ClaimTypes.NameIdentifier,user.ToString()) // User identifier (custom)
        };

        // Set expiration time based on JwtOptions
        var expires = DateTime.UtcNow.AddMinutes(_JwtOptions.ExpirationTimeInMinutes);

        // Create the JWT token
        var Token = new JwtSecurityToken(
                issuer: _JwtOptions.Issuer,
                audience: _JwtOptions.Audience,
                claims: Claims,
                expires: expires,
                signingCredentials: Credentials
        );

        // Convert token object into a compact string format
        var jwtToken = new JwtSecurityTokenHandler().WriteToken(Token);

        // Return token and expiration time
        return (jwtToken, expires);
    }

    // Generates a secure random refresh token (used to request new JWTs)
    public string GenerateRefreshToken()
    {
        var RandomNumber = new byte[64]; // 64-byte random array
        using var rng = RandomNumberGenerator.Create(); // Secure random generator
        rng.GetBytes(RandomNumber); // Fill the array with random bytes
        return Convert.ToBase64String(RandomNumber); // Convert to base64 string
    }

    // Writes token as an HTTP-only cookie (cannot be accessed via JavaScript)
    public void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiration)
    {
        _HttpContextAccessor.HttpContext.Response.Cookies.Append(cookieName, token, new CookieOptions
        {
            HttpOnly = true, // Prevents JavaScript from reading the cookie (security)
            Expires = expiration, // Cookie expiration time
            IsEssential = true, // Required even if user hasn’t consented to optional cookies
            Secure = true, // Only sent over HTTPS
            SameSite = SameSiteMode.Strict // Prevents cross-site requests from sending the cookie
        });
    }
}
