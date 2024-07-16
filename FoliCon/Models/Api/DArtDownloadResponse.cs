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
    public string LocalDownloadPath { get; set; }
    
    public string GetFileSizeHumanReadable()
    {
        return ConvertHelper.ToFileSize(FileSizeBytes);
    }
}