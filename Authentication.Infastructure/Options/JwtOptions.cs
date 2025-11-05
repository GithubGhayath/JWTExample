/// <summary>
/// This class represents the configuration settings for JWT (JSON Web Token) authentication.
/// We create this class to strongly type the JWT configuration from appsettings.json, 
/// so we can easily bind the configuration and use it in our authentication setup.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// The key used to locate the JwtOptions section in appsettings.json
    /// Example usage: configuration.GetSection(JwtOptions.JwtOptionsKey)
    /// </summary>
    public const string JwtOptionsKey = "JwtOptions";

    /// <summary>
    /// The secret key used to sign and validate JWT tokens.
    /// This must be kept private and secure. It ensures that tokens cannot be tampered with.
    /// </summary>
    public string Secret { get; set; }

    /// <summary>
    /// The issuer of the token, usually the URL of the API or authority that generates the token.
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// The audience that the token is intended for, usually your client app or front-end URL.
    /// Tokens will be validated against this value to ensure they are used by the correct recipient.
    /// </summary>
    public string Audience { get; set; }

    /// <summary>
    /// The expiration time of the token in minutes.
    /// After this time, the token will be invalid and the user must re-authenticate.
    /// </summary>
    public int ExpirationTimeInMinutes { get; set; }
}
