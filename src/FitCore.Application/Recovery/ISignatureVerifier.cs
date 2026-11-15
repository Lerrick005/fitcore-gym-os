namespace FitCore.Application.Recovery;

/// <summary>Verifies RSA-PSS signatures against the embedded vendor public key.</summary>
public interface ISignatureVerifier
{
    /// <summary>Returns true if the signature is valid for the given payload.</summary>
    bool Verify(byte[] payload, byte[] signature);

    /// <summary>Returns the embedded vendor public key (base64) for display / verification.</summary>
    string PublicKeyBase64 { get; }
}
