using Authentication.Domain.Entities;

public interface IAuthTokenProcessor
{
    (string JwtToken, DateTime expiresAtUtc) GenerateJwtToken(User user);

    string GenerateRefreshToken();

    void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiration);
}