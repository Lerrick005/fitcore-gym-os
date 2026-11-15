namespace FitCore.Application.Photos;

/// <summary>Abstraction for capturing member photos. File upload is always available as fallback.</summary>
public interface IPhotoCaptureService
{
    bool IsCameraAvailable { get; }
    Task<byte[]?> CaptureFromWebcamAsync(CancellationToken ct = default);
    Task<string> SavePhotoFromFileAsync(string sourcePath, CancellationToken ct = default);
    Task<string> SavePhotoBytesAsync(byte[] jpegBytes, CancellationToken ct = default);
    string GetAbsolutePhotoPath(string relativePath);
}
