using System.Text.Json.Serialization;

namespace Juratifact.Service.VietMap;

public class VietMapOptions
{
    public string BaseUrl { get; set; } = "https://maps.vietmap.vn/api/";
    public string ApiKey { get; set; } = string.Empty;
}

public class VietMapAutocompleteItem
{
    [JsonPropertyName("ref_id")]
    public string RefId { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

public class VietMapPlaceDetail
{
    [JsonPropertyName("display")]
    public string? Display { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("ward")]
    public string? Ward { get; set; }

    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double? Longitude { get; set; }
}
