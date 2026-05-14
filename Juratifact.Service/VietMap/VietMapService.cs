using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Juratifact.Service.VietMap;

public class VietMapService : IVietMapService
{
    private readonly HttpClient _httpClient;
    private readonly VietMapOptions _options;

    public VietMapService(HttpClient httpClient, IOptions<VietMapOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<VietMapAutocompleteItem>> AutocompleteAsync(
        string text,
        string? focus = null,
        int displayType = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Search text is required.", nameof(text));

        EnsureConfigured();

        var query = new Dictionary<string, string?>
        {
            ["apikey"] = _options.ApiKey,
            ["text"] = text,
            ["display_type"] = displayType.ToString()
        };

        if (!string.IsNullOrWhiteSpace(focus))
        {
            query["focus"] = focus;
        }

        var result = await _httpClient.GetFromJsonAsync<List<VietMapAutocompleteItem>>(
            BuildUri("autocomplete/v4", query),
            cancellationToken);

        return result ?? [];
    }

    public async Task<VietMapPlaceDetail> GetPlaceDetailAsync(
        string refId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refId))
            throw new ArgumentException("VietMap ref id is required.", nameof(refId));

        EnsureConfigured();

        var result = await _httpClient.GetFromJsonAsync<VietMapPlaceDetail>(
            BuildUri("place/v4", new Dictionary<string, string?>
            {
                ["apikey"] = _options.ApiKey,
                ["refid"] = refId
            }),
            cancellationToken);

        if (result == null || string.IsNullOrWhiteSpace(result.Display))
            throw new InvalidOperationException("VietMap place detail was not found.");

        return result;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("VietMap API key is not configured.");
    }

    private string BuildUri(string path, IDictionary<string, string?> query)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://maps.vietmap.vn/api/"
            : _options.BaseUrl;

        baseUrl = baseUrl.TrimEnd('/') + "/";
        var normalizedPath = path.TrimStart('/');
        var queryString = string.Join("&", query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));

        return $"{baseUrl}{normalizedPath}?{queryString}";
    }
}
