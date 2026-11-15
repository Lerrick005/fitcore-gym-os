using System.Security.Cryptography;
using FitCore.Application.Recovery;

namespace FitCore.Infrastructure.Cryptography;

/// <summary>
/// Verifies RSA-PSS signatures with the embedded vendor public key.
/// The public key is compiled into this assembly as a resource:
/// Resources/vendor-public-key.txt. The private key never lives here.
/// </summary>
public sealed class RsaSignatureVerifier : ISignatureVerifier
{
    private readonly byte[] _publicKeyBytes;

    public RsaSignatureVerifier()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "vendor-public-key.txt");
        if (!File.Exists(path))
            throw new InvalidOperationException(
                $"Vendor public key not found at {path}. Run FitCore.VendorTool 'generate-keys' to create one.");

        _publicKeyBytes = Convert.FromBase64String(File.ReadAllText(path).Trim());
    }

    public string PublicKeyBase64 => Convert.ToBase64String(_publicKeyBytes);

    public bool Verify(byte[] payload, byte[] signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(_publicKeyBytes, out _);
            return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch
        {
            return false;
        }
    }
}
