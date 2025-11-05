using System.Text;
using Authentication.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


//Identity Services Systems that manage users, passwords, roles, and permissions
builder.Services.AddIdentity<User, IdentityRole<Guid>>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequiredLength = 8;
    opt.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>();//Use Entity Framework Core and this specific ApplicationDbContext to store user and role data.




// Registers your ApplicationDbContext with the dependency injection container (DI).
// So later, your controllers, services, etc. can automatically receive an instance of ApplicationDbContext when needed.
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    // opt is the configuration builder for your context.
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DbconnectionString"))
);






// Bind the "JwtOptions" section from appsettings.json to the JwtOptions class for dependency injection
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.JwtOptionsKey)
);


/*
    How it works at runtime

1. ASP.NET starts up and reads your configuration.
2. It registers ApplicationDbContext in the DI system.
3. When a controller or service asks for it (like through a constructor), the DI system creates one using the connection string.
4. EF Core uses that context to send queries or updates to your SQL Server database.
*/





// Registers the AuthTokenProcessor service with a Scoped lifetime.
// Whenever something (like a controller or service) asks for IAuthTokenProcessor,
// the DI container will create and provide an instance of AuthTokenProcessor.
// "Scoped" means one instance per HTTP request — shared within that request,
// but new for each separate request.
builder.Services.AddScoped<IAuthTokenProcessor, AuthTokenProcessor>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();



// Adds authentication services to the DI container and configures how authentication should work.
builder.Services.AddAuthentication(opt =>
{
    // These three lines tell ASP.NET Core that the default method for
    // authenticating, challenging, and signing in users will use the JWT Bearer scheme.
    // (In short: every time the app needs to check "who is this user?", it uses JWT.)
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;

})
// Adds and configures the JWT Bearer handler — this is what actually reads, validates,
// and processes JWT tokens coming from the client.
.AddJwtBearer(opt =>
{
    // Load the JWT options (Issuer, Audience, Secret, etc.) from appsettings.json.
    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.JwtOptionsKey)
        .Get<JwtOptions>() 
        ?? throw new ArgumentException(nameof(JwtOptions));

    // Configure the rules that will be used to validate incoming tokens.
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        // The server will check that the token’s Issuer, Audience, Lifetime, and Signature are all valid.
        ValidateIssuer = true,  // Check that token came from the correct issuer.
        ValidateAudience = true,  // Check that token is meant for this API.
        ValidateLifetime = true,  // Check that token hasn’t expired.
        ValidateIssuerSigningKey = true,  // Check the signature with the secret key.

        // The expected values from configuration (used in the checks above).
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.Secret)) // The secret key used to verify token signature.
    };

    // Custom logic for how to read the token from incoming requests.
    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Instead of reading the token from the "Authorization" header,
            // this example looks for it in a cookie named "ACCESS_TOKEN".
            context.Token = context.Request.Cookies["ACCESS_TOKEN"];
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseExceptionHandler(_ => { });
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();




app.Run();

/*
================================================================================
EF Core Migration & Database Update Commands Explanation
================================================================================

1️ Create a new migration
--------------------------------------------------------------------------------
Command:
    dotnet ef migrations add init -s .\Authentication.API\ -p .\Authentication.Infrastructure

Meaning:
--------------------------------------------------------------------------------
This command creates a new Entity Framework Core migration named "init".
A migration represents the changes made in the DbContext model (tables, columns, etc.)
so EF Core can update the database schema to match your current model.

Breakdown:
--------------------------------------------------------------------------------
dotnet ef
    → Runs the Entity Framework Core CLI (Command Line Interface) tool.

migrations add init
    → Adds a new migration named "init" to record schema changes.

-s .\Authentication.API\
    → The startup project (-s = startup).
      EF Core uses this project to load configuration and services such as
      the connection string (from appsettings.json) and dependency injection setup.
      Usually this is the Web API project that starts the application.

-p Authentication.Infrastructure
    → The project that contains the DbContext (-p = project).
      EF Core will place the generated migration files inside this project’s
      "Migrations" folder, since it owns the DbContext definition.

In summary:
--------------------------------------------------------------------------------
EF Core will:
  1. Use the DbContext class found in Authentication.Infrastructure.
  2. Use the configuration and startup logic from Authentication.API.
  3. Create a migration file called "init" under the Infrastructure project’s
     Migrations folder.
  4. Use that migration later when applying updates to the database.

Example Folder Structure:
--------------------------------------------------------------------------------
Authentication.API/
  Program.cs
  appsettings.json
Authentication.Infrastructure/
  ApplicationDbContext.cs
  Migrations/
  Authentication.Infrastructure.csproj


2️ Apply the migration to the database
--------------------------------------------------------------------------------
Command:
    dotnet ef database update -s .\Authentication.API\ -p .\Authentication.Infrastructure\

Meaning:
--------------------------------------------------------------------------------
This command applies all pending migrations to the actual database.

Breakdown:
--------------------------------------------------------------------------------
dotnet ef
    → Runs the Entity Framework Core CLI tool.

database update
    → Applies the latest migration(s) to the target database defined in the
      connection string from the startup project.

-s .\Authentication.API\
    → The startup project that provides the configuration (connection string, etc.)
      EF Core needs to connect to the correct database.

-p .\Authentication.Infrastructure\
    → The project containing the DbContext and the migration files.

In summary:
--------------------------------------------------------------------------------
EF Core will:
  1. Find the DbContext inside Authentication.Infrastructure.
  2. Use the connection string from Authentication.API.
  3. Apply all migrations that haven’t yet been applied to the database.
  4. Create or update the database schema to match your current model.

================================================================================
Tip:
If you see version mismatch errors (e.g., EF tools 9.0.9 vs runtime 9.0.10),
update the CLI tools with:
    dotnet tool update --global dotnet-ef
================================================================================
*/





/*
This comment related to appsettins.json
"JwtOptions": {
    "Secret": "mySuper$ecretKey123!@#ForJwtTokenGeneration", // The secret key used to sign and verify JWT tokens (keep it safe and private)
    "Issuer": "https://api.mycompany.com",                   // The server or authority that issues the JWT tokens
    "Audience": "https://myApp.mycompany.com",               // The intended recipient (client/app) that the token is valid for
    "ExpirationTimeInMinutes": 15                            // Token lifetime in minutes before it expires
}

*/