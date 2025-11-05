using Microsoft.AspNetCore.Identity;

namespace Authentication.Domain.Entities;

// Create a custom User class where the user’s ID is of type Guid instead of string.
public class User : IdentityUser<Guid>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUTC { get; set; }

    //Factory Method
    public static User Create(string email, string firstname, string lastname)
    {
        return new User
        {
            Email = email,
            UserName = email,
            FirstName = firstname,
            LastName = lastname
        };
    }
    public override string ToString()
    {
        return FirstName + " " + LastName;
    }
}