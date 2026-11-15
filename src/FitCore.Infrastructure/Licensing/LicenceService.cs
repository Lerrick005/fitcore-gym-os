using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Common;
using FitCore.Application.Licensing;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Licensing;

public sealed class LicenceService : ILicenceService
{
    private readonly FitCoreDbContext _db;
    private readonly string _publicKeyPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LicenceService(FitCoreDbContext db)
    {
        _db = db;
        _publicKeyPath = Path.Combine(AppContext.BaseDirectory, "Resources", "vendor-public-key.txt");
    }

    public async Task<LicenceStatus> GetStatusAsync(CancellationToken ct = default)
    {
        // Latest licence row in DB
        var record = await _db.LicenceRecords
            .AsQueryable()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (record is null)
            return new LicenceStatus(LicenceState.Missing, null, "No licence installed. Running in fallback mode.");

        return await VerifyAsync(record.RawLicenceJson, record.Signature, ct);
    }

    public async Task<Result<LicenceStatus>> LoadLicenceAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return Result<LicenceStatus>.Fail("licence.not_found", "Licence file not found.");

        try
        {
            var raw = await File.ReadAllTextAsync(filePath, ct);
            var envelope = JsonSerializer.Deserialize<LicenceEnvelope>(raw, JsonOptions);
            if (envelope is null || string.IsNullOrWhiteSpace(envelope.Payload) || string.IsNullOrWhiteSpace(envelope.Signature))
                return Result<LicenceStatus>.Fail("licence.malformed", "Licence file is malformed.");

            var status = await VerifyAsync(envelope.Payload, envelope.Signature, ct);

            // Persist the licence (even if tampered, so we know)
            _db.LicenceRecords.Add(new Domain.Entities.LicenceRecord
            {
                RawLicenceJson = envelope.Payload,
                Signature = envelope.Signature,
                VerifiedAtUtc = DateTime.UtcNow,
                Status = MapStatus(status.State)
            });
            await _db.SaveChangesAsync(ct);

            return Result<LicenceStatus>.Ok(status);
        }
        catch (Exception ex)
        {
            return Result<LicenceStatus>.Fail("licence.read.failed", ex.Message);
        }
    }

    public async Task<bool> IsModuleEntitledAsync(string moduleKey, CancellationToken ct = default)
    {
        var status = await GetStatusAsync(ct);
        if (status.State != LicenceState.Valid || status.Payload is null)
            return false; // Safe fallback: no licence = no optional modules

        return status.Payload.EntitledModules.Contains(moduleKey, StringComparer.Ordinal);
    }

    private async Task<LicenceStatus> VerifyAsync(string payloadJson, string signatureBase64, CancellationToken ct)
    {
        LicencePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LicencePayload>(payloadJson, JsonOptions);
        }
        catch
        {
            return new LicenceStatus(LicenceState.Tampered, null, "Licence payload is not valid JSON.");
        }

        if (payload is null)
            return new LicenceStatus(LicenceState.Tampered, null, "Licence payload is empty.");

        // Verify signature
        if (!File.Exists(_publicKeyPath))
            return new LicenceStatus(LicenceState.Tampered, payload, "Vendor public key not found. Cannot verify licence.");

        try
        {
            var pubKeyBytes = Convert.FromBase64String(File.ReadAllText(_publicKeyPath).Trim());
            var sigBytes = Convert.FromBase64String(signatureBase64);
            var payloadBytes = Encoding.UTF8.GetBytes(Canonicalise(payloadJson));

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(pubKeyBytes, out _);
            if (!rsa.VerifyData(payloadBytes, sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
                return new LicenceStatus(LicenceState.Tampered, payload, "Licence signature is invalid.");
        }
        catch (Exception ex)
        {
            return new LicenceStatus(LicenceState.Tampered, payload, $"Signature verification failed: {ex.Message}");
        }

        // Check Installation ID
        var installation = await _db.Installations.AsQueryable().FirstOrDefaultAsync(ct);
        if (installation is null)
            return new LicenceStatus(LicenceState.Tampered, payload, "No installation found.");

        if (!string.Equals(payload.InstallationId, installation.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            return new LicenceStatus(LicenceState.WrongInstallation, payload, "This licence is for a different installation.");

        // Check expiry
        if (payload.ExpiresAtUtc.HasValue && payload.ExpiresAtUtc.Value < DateTime.UtcNow)
            return new LicenceStatus(LicenceState.Expired, payload, "This licence has expired. Contact your vendor.");

        return new LicenceStatus(LicenceState.Valid, payload, "Licence is valid.");
    }

    /// <summary>
    /// Canonical JSON — matches what VendorTool signs. We sign the parsed + re-serialised form
    /// (not the raw bytes) so both sides produce identical output regardless of whitespace.
    /// </summary>
    public static string Canonicalise(string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        // Deterministic ordering: re-serialise via STJ with sorted properties.
        var sorted = SortJsonElement(doc.RootElement);
        return JsonSerializer.Serialize(sorted);
    }

    private static object? SortJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(p => p.Name, p => SortJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(SortJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : (object)element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static Domain.Enums.LicenceStatus MapStatus(LicenceState state) => state switch
    {
        LicenceState.Valid => Domain.Enums.LicenceStatus.Valid,
        LicenceState.Expired => Domain.Enums.LicenceStatus.Expired,
        LicenceState.WrongInstallation => Domain.Enums.LicenceStatus.WrongInstallation,
        LicenceState.Tampered => Domain.Enums.LicenceStatus.Invalid,
        _ => Domain.Enums.LicenceStatus.Missing
    };

    private sealed class LicenceEnvelope
    {
        public string Payload { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public async Task<bool> ShouldShowAttributionAsync(CancellationToken ct = default)
    {
        var status = await GetStatusAsync(ct);
        // No licence / expired / tampered → show attribution (fallback)
        if (status.State != LicenceState.Valid || status.Payload is null)
            return true;

        // Licence may hide attribution
        return !string.Equals(status.Payload.AttributionMode, "hidden", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> GetAttributionTextAsync(CancellationToken ct = default)
    {
        var status = await GetStatusAsync(ct);
        if (status.State != LicenceState.Valid || status.Payload is null)
            return "Powered by FitCore";

        return string.IsNullOrWhiteSpace(status.Payload.AttributionText)
            ? "Powered by FitCore"
            : status.Payload.AttributionText;
    }
}

