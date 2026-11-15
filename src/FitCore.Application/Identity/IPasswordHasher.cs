namespace FitCore.Application.Identity;

/// <summary>
/// Password hash contract (PRS §11). Argon2id in Infrastructure; abstraction enables future algorithm migration.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Returns (hashBase64, hashParamsJson).</summary>
    (string Hash, string Params) Hash(string password);

    /// <summary>Verifies password against stored hash + params. Constant-time comparison.</summary>
    bool Verify(string password, string storedHash, string storedParams);
}
