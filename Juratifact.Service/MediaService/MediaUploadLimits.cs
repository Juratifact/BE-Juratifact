namespace Juratifact.Service.MediaService;

public static class MediaUploadLimits
{
    public const long MaxImageBytes = 10L * 1000 * 1000;
    public const long MaxVideoBytes = 100L * 1000 * 1000;

    public const int MaxImageMb = 10;
    public const int MaxVideoMb = 100;
}
