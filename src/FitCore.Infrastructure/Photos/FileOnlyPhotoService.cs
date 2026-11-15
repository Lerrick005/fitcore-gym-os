using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Photos;

namespace FitCore.Infrastructure.Photos;

/// <summary>
/// File-only photo service. Webcam capture requires hardware access — this class
/// always returns null from CaptureFromWebcamAsync and callers fall back to file upload.
/// To add webcam support: implement CaptureFromWebcamAsync using OpenCvSharp4 or MediaFoundation.
/// </summary>
public sealed class FileOnlyPhotoService : IPhotoCaptureService
{
    private readonly string _photosFolder;

    public FileOnlyPhotoService(string dataFolder)
    {
        _photosFolder = Path.Combine(dataFolder, "photos");
        Directory.CreateDirectory(_photosFolder);
    }

    public bool IsCameraAvailable => false;  // No webcam library wired by default

    public Task<byte[]?> CaptureFromWebcamAsync(CancellationToken ct = default)
        => Task.FromResult<byte[]?>(null);

    public async Task<string> SavePhotoFromFileAsync(string sourcePath, CancellationToken ct = default)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Photo not found.", sourcePath);

        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
            throw new InvalidOperationException("Photo must be JPG, PNG, or WebP.");

        var destName = $"{Guid.NewGuid():N}{ext}";
        var destPath = Path.Combine(_photosFolder, destName);
        File.Copy(sourcePath, destPath);
        return $"photos/{destName}";
    }

    public async Task<string> SavePhotoBytesAsync(byte[] jpegBytes, CancellationToken ct = default)
    {
        var destName = $"{Guid.NewGuid():N}.jpg";
        var destPath = Path.Combine(_photosFolder, destName);
        await File.WriteAllBytesAsync(destPath, jpegBytes, ct);
        return $"photos/{destName}";
    }

    public string GetAbsolutePhotoPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return "";
        var dataFolder = Directory.GetParent(_photosFolder)?.FullName ?? "";
        return Path.Combine(dataFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
