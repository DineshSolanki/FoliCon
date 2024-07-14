namespace FoliCon.Models.Api;

public record DArtDownloadResponse
{
    [JsonProperty("src")]
    public string Src { get; init; }

    [JsonProperty("filename")]
    public string Filename { get; init; }

    [JsonProperty("width")]
    public int Width { get; init; }

    [JsonProperty("height")]
    public int Height { get; init; }

    [JsonProperty("filesize")]
    public int FileSizeBytes { get; init; }
    
    [JsonIgnore]
    public string localDownloadPath { get; set; }
    
    public string GetFileSizeHumanReadable()
    {
        if (FileSizeBytes < 1024)
            return $"{FileSizeBytes} bytes";
        
        int i;
        double fileSizeToDouble = FileSizeBytes;

        for (i = 0; i < 8 && fileSizeToDouble >= 1024; ++i)
        {
            fileSizeToDouble /= 1024;
        }

        return $"{fileSizeToDouble:0.##} {new[] {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"}[i]}";
    }
}