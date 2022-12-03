using HandyControl.Controls;

using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules;

public static class DialogServiceExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static void ShowMessageBox(this IDialogService dialogService, string message,
        Action<IDialogResult> callBack)
    {
        Logger.Debug(message: "ShowMessageBox called with message: {Message}", message);
        dialogService.ShowDialog("MessageBox", new DialogParameters($"message={message}"), callBack);
    }

    public static void ShowSearchResult(this IDialogService dialogService, string searchMode, string query,
        string folderPath, ResultResponse result, Tmdb tmdbObject, IgdbClass igdbObject, bool isPickedById,
        Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowSearchResult called with query: {Query} and result: {@Result}", query, result);
        var p = new DialogParameters
        {
            {"query", query}, {"result", result}, {"searchmode", searchMode}, {"tmdbObject", tmdbObject},
            {"igdbObject", igdbObject},
            {"folderpath", folderPath},
            {"isPickedById" , isPickedById}
        };
        dialogService.ShowDialog("SearchResult", p, callBack);
    }
    public static void ShowCustomIconWindow(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowCustomIconWindow called");
        dialogService.ShowDialog("CustomIcon", callBack);
    }

    public static void ShowProSearchResult(this IDialogService dialogService, string folderPath,
        List<string> fnames, DataTable pickedTable, List<ImageToDownload> imgList, DArt dartObject,
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

    public static void ShowAboutBox(this IDialogService dialogService, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowAboutBox called");
        dialogService.ShowDialog("AboutBox", callBack);
    }
    public static void ShowPosterPicker(this IDialogService dialogService, Tmdb tmdbObject, IgdbClass igdbObject, ResultResponse result,int pickedIndex, ObservableCollection<ListItem> resultData, bool isPickedById, Action<IDialogResult> callBack)
    {
        Logger.Trace("ShowPosterPicker called with result: {@Result}", result);
        var p = new DialogParameters
        {
            {"pickedIndex", pickedIndex}, {"result", result}, {"tmdbObject",tmdbObject}, {"igdbObject", igdbObject} , {"resultList", resultData},
            {"isPickedById" , isPickedById}
        };
        dialogService.ShowDialog("PosterPicker", p, callBack);
    }
}