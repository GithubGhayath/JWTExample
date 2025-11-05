using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class UserRepository :IUserRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public UserRepository(ApplicationDbContext applicationDbContext)
    {
        this._applicationDbContext = applicationDbContext;
    }

    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        var User = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        return User;
        
    }
}