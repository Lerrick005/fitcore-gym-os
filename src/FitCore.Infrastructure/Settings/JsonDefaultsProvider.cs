using System.Text.Json;
using FitCore.Application.Settings;

namespace FitCore.Infrastructure.Settings;

public sealed class JsonDefaultsProvider : IDefaultsProvider
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;
    private JsonElement? _root;

    public JsonDefaultsProvider(string defaultsPath)
    {
        _path = defaultsPath;
    }

    private async Task<JsonElement?> RootAsync(CancellationToken ct)
    {
        if (_root.HasValue) return _root;
        if (!File.Exists(_path)) return null;

        var json = await File.ReadAllTextAsync(_path, ct);
        using var doc = JsonDocument.Parse(json);
        _root = doc.RootElement.Clone();
        return _root;
    }

    private async Task<T?> LoadSectionAsync<T>(string sectionName, CancellationToken ct) where T : class
    {
        var root = await RootAsync(ct);
        if (root is null) return null;

        if (!root.Value.TryGetProperty(sectionName, out var section)) return null;
        return JsonSerializer.Deserialize<T>(section.GetRawText(), Options);
    }

    public async Task<BrandingSettings> GetBrandingDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<BrandingSettings>("branding", ct) ?? new();

    public async Task<BusinessSettings> GetBusinessDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<BusinessSettings>("business", ct) ?? new();

    public async Task<LocaleSettings> GetLocaleDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<LocaleSettings>("locale", ct) ?? new();

    public async Task<HoursSettings> GetHoursDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<HoursSettings>("hours", ct) ?? new();

    public async Task<PlansSettings> GetPlansDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<PlansSettings>("plans", ct) ?? new();

    public async Task<ReceiptSettings> GetReceiptDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<ReceiptSettings>("receipt", ct) ?? new();

    public async Task<GymPreferencesSettings> GetGymPreferencesDefaultsAsync(CancellationToken ct = default)
        => await LoadSectionAsync<GymPreferencesSettings>("gymPreferences", ct) ?? new();
}
