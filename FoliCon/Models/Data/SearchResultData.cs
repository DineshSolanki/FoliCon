namespace FoliCon.Models.Data;

public class SearchResultData
{
    public dynamic Result { get; set; }
    public string ResultType { get; set; }
    public string FullFolderPath { get; set; }
    public string Rating { get; set; }
    public bool IsPickedById { get; set; }
    public string FolderName { get; set; }
    public string LocalPosterPath { get; set; }
}