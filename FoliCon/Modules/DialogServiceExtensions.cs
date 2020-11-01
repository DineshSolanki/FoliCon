using FoliCon.Models;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Data;

namespace FoliCon.Modules
{
    public static class DialogServiceExtensions
    {
        public static void ShowMessageBox(this IDialogService dialogService, string message,
            Action<IDialogResult> callBack)
        {
            dialogService.ShowDialog("MessageBox", new DialogParameters($"message={message}"), callBack);
        }

        public static void ShowSearchResult(this IDialogService dialogService, string searchMode, string query,
            string folderPath, ResultResponse result, Tmdb tmdbObject, IgdbClass igdbObject,
            Action<IDialogResult> callBack)
        {
            DialogParameters p = new DialogParameters
            {
                {"query", query}, {"result", result}, {"searchmode", searchMode}, {"tmdbObject", tmdbObject},
                {"igdbObject", igdbObject},
                {"folderpath", folderPath}
            };
            dialogService.ShowDialog("SearchResult", p, callBack);
        }

        public static void ShowProSearchResult(this IDialogService dialogService, string folderPath,
            List<string> fnames, DataTable pickedTable, List<ImageToDownload> imgList, DArt dartObject,
            Action<IDialogResult> callBack)
        {
            DialogParameters p = new DialogParameters
            {
                {"folderpath", folderPath}, {"fnames", fnames}, {"pickedtable", pickedTable}, {"imglist", imgList},
                {"dartobject", dartObject}
            };

            dialogService.ShowDialog("ProSearchResult", p, callBack);
        }

        public static void ShowApiConfig(this IDialogService dialogService, Action<IDialogResult> callBack)
        {
            dialogService.ShowDialog("ApiConfig", new DialogParameters(), callBack);
        }

        public static void ShowAboutBox(this IDialogService dialogService, Action<IDialogResult> callBack)
        {
            dialogService.ShowDialog("AboutBox", new DialogParameters(), callBack);
        }
    }
}