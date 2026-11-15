using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FitCore.Application.Identity;
using Konscious.Security.Cryptography;

namespace FitCore.Infrastructure.Identity;

/// <summary>
/// Argon2id password hasher (PRS §11). Parameters per OWASP 2024 guidance.
/// Parameters + salt stored with each hash to allow future upgrades.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    // OWASP minimum for Argon2id (2024+): 19 MiB memory, t=2, p=1.
    // We use a stronger posture suited for offline single-user workstation.
    private const int MemoryKb = 65536;   // 64 MB
    private const int Iterations = 3;
    private const int Parallelism = 1;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public (string Hash, string Params) Hash(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Password is required.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, MemoryKb, Iterations, Parallelism, HashBytes);

        var hashParams = new HashParams
        {
            Alg = "argon2id",
            M = MemoryKb,
            T = Iterations,
            P = Parallelism,
            Salt = Convert.ToBase64String(salt)
        };
        return (Convert.ToBase64String(hash), JsonSerializer.Serialize(hashParams));
    }

    public bool Verify(string password, string storedHash, string storedParams)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedParams))
            return false;

        HashParams? p;
        try { p = JsonSerializer.Deserialize<HashParams>(storedParams); }
        catch { return false; }
        if (p is null || p.Alg != "argon2id") return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(p.Salt ?? string.Empty);
            expected = Convert.FromBase64String(storedHash);
        }
        catch { return false; }

        var actual = Derive(password, salt, p.M, p.T, p.P, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int m, int t, int p, int outLen)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = m,
            Iterations = t,
            DegreeOfParallelism = p
        };
        var raw = argon2.GetBytes(outLen);
        return raw;
    }

    private sealed class HashParams
    {
        public string Alg { get; set; } = "argon2id";
        public int M { get; set; }
        public int T { get; set; }
        public int P { get; set; }
        public string? Salt { get; set; }
    }
}
