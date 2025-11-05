using Authentication.Domain.Entities;

public interface IUserRepository
{
     Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
}