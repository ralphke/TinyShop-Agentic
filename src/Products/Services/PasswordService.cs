namespace Products.Services;

/// <summary>
/// Provides password hashing and verification using BCrypt.
/// </summary>
public static class PasswordService
{
    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password.</returns>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        // BCrypt.Net-Next handles salt generation automatically
        // The work factor is set to 12 (default), providing a good balance between security and performance
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The BCrypt hash to verify against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // BCrypt.Verify can throw if the hash is invalid
            return false;
        }
    }
}
