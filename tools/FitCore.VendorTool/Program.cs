using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace FitCore.VendorTool;

public static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        try
        {
            switch (command)
            {
                case "generate-keys": return GenerateKeys(args);
                case "sign-recovery": return SignRecovery(args);
                case "sign-licence": return SignLicence(args);
                default:
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 2;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("FitCore.VendorTool — commands:");
        Console.WriteLine("  generate-keys <private-key-path> <public-key-path>");
        Console.WriteLine("      Creates an RSA 2048-bit PSS keypair.");
        Console.WriteLine("      Save private key securely; never ship it.");
        Console.WriteLine();
        Console.WriteLine("  sign-recovery <private-key-path> <challenge-id> <installation-id> <user-id> <expiry-unix>");
        Console.WriteLine("      Signs a recovery code. Output is the value the owner types into the app.");
        Console.WriteLine();
        Console.WriteLine("  sign-licence <private-key-path> <customer-name> <installation-id> <edition> <attribution-mode> <attribution-text> <modules-csv> <expires-in-days|never>");
        Console.WriteLine("      Signs a licence file. Saves to the Desktop.");
        Console.WriteLine("      Example: sign-licence key.pem \"Body Craft Gym\" E71EB554-... Standard hidden \"\" trainers,inventory never");
    }

    private static int GenerateKeys(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: generate-keys <private-key-path> <public-key-path>");
            return 1;
        }

        var privatePath = args[1];
        var publicPath = args[2];

        using var rsa = RSA.Create(2048);
        var privateBytes = rsa.ExportPkcs8PrivateKey();
        var publicBytes = rsa.ExportSubjectPublicKeyInfo();

        File.WriteAllText(privatePath, Convert.ToBase64String(privateBytes));
        File.WriteAllText(publicPath, Convert.ToBase64String(publicBytes));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Private key: {privatePath}");
        Console.WriteLine($"Public key:  {publicPath}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1. Keep the private key OFFLINE and backed up.");
        Console.WriteLine("  2. Ship the public key with the desktop app at Resources/vendor-public-key.txt.");
        return 0;
    }

    private static int SignRecovery(string[] args)
    {
        if (args.Length < 6)
        {
            Console.WriteLine("Usage: sign-recovery <private-key-path> <challenge-id> <installation-id> <user-id> <expiry-unix>");
            return 1;
        }

        var privatePath = args[1];
        var challengeId = args[2];
        var installationId = args[3];
        var userId = args[4];
        var expiryUnix = args[5];

        var privateKeyBase64 = File.ReadAllText(privatePath).Trim();
        var privateBytes = Convert.FromBase64String(privateKeyBase64);

        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateBytes, out _);

        var payloadText = $"{challengeId}|{installationId}|{userId}|{expiryUnix}";
        var payloadBytes = Encoding.UTF8.GetBytes(payloadText);

        var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        var signatureBase64 = Convert.ToBase64String(signature);

        var recoveryCode = $"{challengeId}|{signatureBase64}|{expiryUnix}";

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Recovery code (single-use, time-limited):");
        Console.WriteLine(recoveryCode);
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Give this to the owner. They paste it into the FitCore 'Forgot password' screen.");
        return 0;
    }

    private static int SignLicence(string[] args)
    {
        if (args.Length < 8)
        {
            Console.WriteLine($"Received {args.Length} args.");
            Console.WriteLine("Usage: sign-licence <private-key-path> <customer-name> <installation-id> <edition> <attribution-mode> [attribution-text] <modules-csv> <expires|never>");
            return 1;
        }

        var privateKeyPath = args[1];
        var customerName = args[2];
        var installationId = args[3];
        var edition = args[4];
        var attributionMode = args[5];
        var attributionText = args[6];
        var modulesCsv = args[7];
        var expiryArg = args[8];

        if (!File.Exists(privateKeyPath))
        {
            Console.WriteLine($"Private key not found: {privateKeyPath}");
            return 1;
        }

        var modules = string.IsNullOrWhiteSpace(modulesCsv)
            ? Array.Empty<string>()
            : modulesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var issued = DateTime.UtcNow;
        var expires = expiryArg.ToLowerInvariant() switch
        {
            "never" => (DateTime?)null,
            _ when int.TryParse(expiryArg, out var days) => issued.AddDays(days),
            _ => (DateTime?)null
        };

        var payloadObj = new
        {
            LicenceId = Guid.NewGuid().ToString("N"),
            CustomerName = customerName,
            InstallationId = installationId,
            AttributionMode = attributionMode,
            AttributionText = attributionText,
            EditionLabel = edition,
            EntitledModules = modules,
            IssuedAtUtc = issued,
            ExpiresAtUtc = expires
        };

        // Canonicalise: sort JSON properties by name (matches app-side verification)
        var payloadJson = CanonicaliseObject(payloadObj);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        var privateKeyBase64 = File.ReadAllText(privateKeyPath).Trim();
        var privateBytes = Convert.FromBase64String(privateKeyBase64);
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateBytes, out _);
        var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        var envelope = new
        {
            payload = payloadJson,
            signature = Convert.ToBase64String(signature)
        };

        var envelopeJson = System.Text.Json.JsonSerializer.Serialize(envelope, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var fileName = $"fitcore-licence-{customerName.Replace(' ', '-')}.licence.json";
        var destPath = Path.Combine(desktop, fileName);
        File.WriteAllText(destPath, envelopeJson);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Licence created: {destPath}");
        Console.WriteLine($"  Customer: {customerName}");
        Console.WriteLine($"  Installation: {installationId}");
        Console.WriteLine($"  Edition: {edition}");
        Console.WriteLine($"  Modules: {(modules.Length == 0 ? "(none)" : string.Join(", ", modules))}");
        Console.WriteLine($"  Expires: {(expires.HasValue ? expires.Value.ToString("yyyy-MM-dd") : "never")}");
        Console.ResetColor();
        return 0;
    }

    private static string CanonicaliseObject(object obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var sorted = SortJson(doc.RootElement);
        return System.Text.Json.JsonSerializer.Serialize(sorted);
    }

    private static object? SortJson(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Object => element.EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(p => p.Name, p => SortJson(p.Value)),
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(SortJson).ToList(),
            System.Text.Json.JsonValueKind.String => element.GetString(),
            System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out var l) ? l : (object)element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => null
        };
    }
}




