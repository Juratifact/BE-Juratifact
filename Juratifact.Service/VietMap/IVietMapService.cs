namespace Juratifact.Service.VietMap;

public interface IVietMapService
{
    Task<List<VietMapAutocompleteItem>> AutocompleteAsync(
        string text,
        string? focus = null,
        int displayType = 5,
        CancellationToken cancellationToken = default);

    Task<VietMapPlaceDetail> GetPlaceDetailAsync(
        string refId,
        CancellationToken cancellationToken = default);
}
