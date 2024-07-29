namespace FoliCon.Modules.Extension;

[Localizable(false)]
public static class DialogServiceExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static void ShowMessageBox(this IDialogService dialogService, string message,
        Action<IDialogResult> callBack)
    {
        Logger.Debug(message: "ShowMessageBox called with message: {Message}", message);
        dialogService.ShowDialog("MessageBox", new DialogParameters($"message={message}"), callBack);
    }

    public static void ShowSearchResult(this IDialogService dialogService, SearchResultDialogParams searchResultDialogParams)
    {
        Logger.Trace("ShowSearchResult called with query: {Query} and result: {@Result}", searchResultDialogParams.Query, searchResultDialogParams.Result);
        var p = new DialogParameters
        {
            {"query", searchResultDialogParams.Query}, {"result", searchResultDialogParams.Result}, {"searchmode", searchResultDialogParams.SearchMode}, {"tmdbObject", searchResultDialogParams.TmdbObject},
            {"igdbObject", searchResultDialogParams.IgdbObject},
            {"folderpath", searchResultDialogParams.FolderPath},
            {"isPickedById" , searchResultDialogParams.IsPickedById}
        };
        dialogService.ShowDialog("SearchResult", p, searchResultDialogParams.CallBack);
    }
    public static void ShowCustomIconWindow(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowCustomIconWindow called");
        dialogService.ShowDialog("CustomIcon", callBack);
    }

    public static void ShowProSearchResult(this IDialogService dialogService, string folderPath,
        List<string> fnames, List<PickedListItem> pickedTable, List<ImageToDownload> imgList, DArt dartObject,
        Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowProSearchResult called with folderPath: {FolderPath} and fnames: {@Fnames}", folderPath,
            fnames);
        var p = new DialogParameters
        {
            {"folderpath", folderPath}, {"fnames", fnames}, {"pickedtable", pickedTable}, {"imglist", imgList},
            {"dartobject", dartObject}
        };

        dialogService.ShowDialog("ProSearchResult", p, callBack);
    }

    public static void ShowApiConfig(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        dialogService.ShowDialog("ApiConfig", callBack);
    }
    public static void ShowPosterIconConfig(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowPosterIconConfig called");
        dialogService.ShowDialog("PosterIconConfig", callBack);
    }
    
    public static void ShowSubfolderProcessingConfig(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowSubfolderProcessingConfig called");
        dialogService.ShowDialog("SubfolderProcessingConfig", callBack);
    }
    
    public static void ShowManualExplorer(this IDialogService dialogService, string deviationId,
        DArt dartObject, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowManualExplorer called with deviationId: {DeviationId}", deviationId);
        var p = new DialogParameters
        {
            {"DeviationId", deviationId}, {"dartobject", dartObject}
        };
        dialogService.ShowDialog("ManualExplorer", p, callBack);
    }

    public static void ShowAboutBox(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowAboutBox called");
        dialogService.ShowDialog("AboutBox", callBack);
    }
    public static void ShowPosterPicker(this IDialogService dialogService, PosterPickerDialogParams dialogParams)
    {
        Logger.Trace("ShowPosterPicker called with dialogParams: {@DialogParams}", dialogParams);
        var p = new DialogParameters
        {
            {"pickedIndex", dialogParams.PickedIndex}, {"result", dialogParams.Result}, {"tmdbObject", dialogParams.TmdbObject},
            {"igdbObject", dialogParams.IgdbObject}, {"resultList", dialogParams.ResultData},
            {"isPickedById" , dialogParams.IsPickedById}
        };
        dialogService.ShowDialog("PosterPicker", p, dialogParams.CallBack);
    }

    public static void ShowPreviewer(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowPreviewer called");
        var p = new DialogParameters();
        dialogService.ShowDialog("Previewer", p, callBack);
    }
}