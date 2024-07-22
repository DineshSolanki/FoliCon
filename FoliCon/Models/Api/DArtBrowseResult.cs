// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace FoliCon.Models.Api;

public class Author
{
    [JsonProperty("userid")]
    public string Userid { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("usericon")]
    public string Usericon { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

public class PremiumFolderData
{
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("has_access")]
    public bool HasAccess { get; set; }
    
    [JsonProperty("gallery_id")]
    public string GallaryId { get; set; }
}

public class Stats
{
    [JsonProperty("comments")]
    public int Comments { get; set; }

    [JsonProperty("favourites")]
    public int Favourites { get; set; }
}

public class Content
{
    [JsonProperty("src")]
    public string Src { get; set; }

    [JsonProperty("filesize")]
    public int Filesize { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("transparency")]
    public bool Transparency { get; set; }
}

public class Thumb
{
    [JsonProperty("src")]
    public string Src { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("transparency")]
    public bool Transparency { get; set; }
}

public class Result
{
    [JsonProperty("deviationid")]
    public string Deviationid { get; set; }

    [JsonProperty("printid")]
    public string Printid { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("category_path")]
    public string CategoryPath { get; set; }

    [JsonProperty("is_downloadable")]
    public bool IsDownloadable { get; set; }

    [JsonProperty("is_mature")]
    public bool IsMature { get; set; }

    [JsonProperty("is_favourited")]
    public bool IsFavourited { get; set; }

    [JsonProperty("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonProperty("author")]
    public Author Author { get; set; }

    [JsonProperty("stats")]
    public Stats Stats { get; set; }

    [JsonProperty("published_time")]
    public int PublishedTime { get; set; }

    [JsonProperty("allows_comments")]
    public bool AllowsComments { get; set; }

    [JsonProperty("content")]
    public Content Content { get; set; }

    [JsonProperty("thumbs")]
    public Thumb[] Thumbs { get; set; }
    
    [JsonProperty("premium_folder_data")]
    public PremiumFolderData PremiumFolderData { get; set; }
}

public class DArtBrowseResult
{
    [JsonProperty("has_more")]
    public bool HasMore { get; set; }

    [JsonProperty("next_offset")]
    public int NextOffset { get; set; }

#nullable enable
    [JsonProperty("results")]
    public Result[]? Results { get; set; }
}