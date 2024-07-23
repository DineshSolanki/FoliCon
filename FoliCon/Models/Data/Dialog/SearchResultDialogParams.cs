namespace FoliCon.Models.Data.Dialog;

public class SearchResultDialogParams : DialogParamContainer
{
    public string SearchMode { get; set; }
    public string Query { get; set; }
    public string FolderPath { get; set; }
}