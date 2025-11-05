using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;


// Using IdentityDbContext gives you all Identity tables automatically without having to define them manually.
public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{

    //DbContextOptions<T> tells Entity Framework how to connect to your database (pass connection string)
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    //  EF Core calls this when building the model that is,
    //  when it prepares the database schema from your classes.
    protected override void OnModelCreating(ModelBuilder builder)
    {

        //This ensures Identity’s default configuration (users, roles, tokens, etc.) is applied first.

        base.OnModelCreating(builder);

        //Then you can safely add your custom configurations without breaking Identity.


        /*
            builder.Entity<User>() ===> selects your User entity for configuration.
            .Property(u => u.FirstName) ===> selects the FirstName property.
            .HasMaxLength(256) ===> tells EF Core the database column should have max length 256.
        */
        builder.Entity<User>().Property(u => u.FirstName).HasMaxLength(256);
        builder.Entity<User>().Property(u => u.LastName).HasMaxLength(256);

    }
}