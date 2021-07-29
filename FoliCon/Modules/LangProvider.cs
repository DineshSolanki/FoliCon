using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HandyControl.Tools;

namespace FoliCon.Properties.Langs
{
    public class LangProvider : INotifyPropertyChanged
    {
        internal static LangProvider Instance => ResourceHelper.GetResource<LangProvider>("DemoLangs");

        private static string CultureInfoStr;

        public static CultureInfo Culture
        {
            get => Lang.Culture;
            set
            {
                if (value == null) return;
                if (Equals(CultureInfoStr, value.EnglishName)) return;
                Lang.Culture = value;
                CultureInfoStr = value.EnglishName;

                Instance.UpdateLangs();
            }
        }

        public static string GetLang(string key) => Lang.ResourceManager.GetString(key, Culture);

        public static void SetLang(DependencyObject dependencyObject, DependencyProperty dependencyProperty, string key) =>
            BindingOperations.SetBinding(dependencyObject, dependencyProperty, new Binding(key)
            {
                Source = Instance,
                Mode = BindingMode.OneWay
            });

        private void UpdateLangs()
        {
            OnPropertyChanged(nameof(About));
            OnPropertyChanged(nameof(AlwaysShowPosterWindow));
            OnPropertyChanged(nameof(AmbiguousTitleTooltip));
            OnPropertyChanged(nameof(APIKeysConfiguration));
            OnPropertyChanged(nameof(Auto));
            OnPropertyChanged(nameof(Cancel));
            OnPropertyChanged(nameof(ChangePosterOverlay));
            OnPropertyChanged(nameof(CheckForUpdate));
            OnPropertyChanged(nameof(DeleteIcons));
            OnPropertyChanged(nameof(DeleteIconsTooltip));
            OnPropertyChanged(nameof(DownloadingIcons));
            OnPropertyChanged(nameof(Folder));
            OnPropertyChanged(nameof(FoldersProcessed));
            OnPropertyChanged(nameof(Game));
            OnPropertyChanged(nameof(HaveIcons));
            OnPropertyChanged(nameof(HaveIconsTooltip));
            OnPropertyChanged(nameof(HelpDocument));
            OnPropertyChanged(nameof(IconsCreated));
            OnPropertyChanged(nameof(IgnoreAmbiguousTitle));
            OnPropertyChanged(nameof(LangComment));
            OnPropertyChanged(nameof(Load));
            OnPropertyChanged(nameof(MakeIcons));
            OnPropertyChanged(nameof(Movie));
            OnPropertyChanged(nameof(OutOf));
            OnPropertyChanged(nameof(Poster));
            OnPropertyChanged(nameof(PosterOverlayTooltip));
            OnPropertyChanged(nameof(Professional));
            OnPropertyChanged(nameof(Rating));
            OnPropertyChanged(nameof(RestartExplorer));
            OnPropertyChanged(nameof(RestartExplorerTooltip));
            OnPropertyChanged(nameof(ShowRatingBadgeTooltip));
            OnPropertyChanged(nameof(ShowPosterWindowTooltip));
            OnPropertyChanged(nameof(ShowRatingBadge));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(TV));
            OnPropertyChanged(nameof(UsePosterOverlay));
            OnPropertyChanged(nameof(Year));
            OnPropertyChanged(nameof(IconMode));
            OnPropertyChanged(nameof(SearchMode));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public string About => Lang.About;
        public string AlwaysShowPosterWindow => Lang.AlwaysShowPosterWindow;
        public string AmbiguousTitleTooltip => Lang.AmbiguousTitleTooltip;
        public string APIKeysConfiguration => Lang.APIKeysConfiguration;
        public string Auto => Lang.Auto;
        public string Cancel => Lang.Cancel;
        public string ChangePosterOverlay => Lang.ChangePosterOverlay;
        public string CheckForUpdate => Lang.CheckForUpdate;
        public string DeleteIcons => Lang.DeleteIcons;
        public string DeleteIconsTooltip => Lang.DeleteIconsTooltip;
        public string DownloadingIcons => Lang.DownloadingIcons;
        public string Folder => Lang.Folder;
        public string FoldersProcessed => Lang.FoldersProcessed;
        public string Game => Lang.Game;
        public string HaveIcons => Lang.HaveIcons;
        public string HaveIconsTooltip => Lang.HaveIconsTooltip;
        public string HelpDocument => Lang.HelpDocument;
        public string IconsCreated => Lang.IconsCreated;
        public string IgnoreAmbiguousTitle => Lang.IgnoreAmbiguousTitle;
        public string LangComment => Lang.LangComment;
        public string Load => Lang.Load;
        public string MakeIcons => Lang.MakeIcons;
        public string Movie => Lang.Movie;
        public string OutOf => Lang.OutOf;
        public string Poster => Lang.Poster;
        public string PosterOverlayTooltip => Lang.PosterOverlayTooltip;
        public string Professional => Lang.Professional;
        public string Rating => Lang.Rating;
        public string RestartExplorer => Lang.RestartExplorer;
        public string RestartExplorerTooltip => Lang.RestartExplorerTooltip;
        public string ShowRatingBadgeTooltip => Lang.ShowRatingBadgeTootip;
        public string ShowPosterWindowTooltip => Lang.ShowPosterWindowTooltip;
        public string ShowRatingBadge => Lang.ShowRatingBadge;
        public string Title => Lang.Title;
        public string TV => Lang.TV;
        public string UsePosterOverlay => Lang.UsePosterOverlay;
        public string Year => Lang.Year;
        public string IconMode => Lang.IconMode;
        public string SearchMode => Lang.SearchMode;
    }

    public class LangKeys
    {
        public static string About = nameof(About);
        public static string AlwaysShowPosterWindow = nameof(AlwaysShowPosterWindow);
        public static string AmbiguousTitleTooltip = nameof(AmbiguousTitleTooltip);
        public static string APIKeysConfiguration = nameof(APIKeysConfiguration);
        public static string Auto = nameof(Auto);
        public static string Cancel = nameof(Cancel);
        public static string ChangePosterOverlay = nameof(ChangePosterOverlay);
        public static string CheckForUpdate = nameof(CheckForUpdate);
        public static string DeleteIcons = nameof(DeleteIcons);
        public static string DeleteIconsTooltip = nameof(DeleteIconsTooltip);
        public static string DownloadingIcons = nameof(DownloadingIcons);
        public static string Folder = nameof(Folder);
        public static string FoldersProcessed = nameof(FoldersProcessed);
        public static string Game = nameof(Game);
        public static string HaveIcons = nameof(HaveIcons);
        public static string HaveIconsTooltip = nameof(HaveIconsTooltip);
        public static string HelpDocument = nameof(HelpDocument);
        public static string IconsCreated = nameof(IconsCreated);
        public static string IgnoreAmbiguousTitle = nameof(IgnoreAmbiguousTitle);
        public static string LangComment = nameof(LangComment);
        public static string Load = nameof(Load);
        public static string MakeIcons = nameof(MakeIcons);
        public static string Movie = nameof(Movie);
        public static string OutOf = nameof(OutOf);
        public static string Poster = nameof(Poster);
        public static string PosterOverlayTooltip = nameof(PosterOverlayTooltip);
        public static string Professional = nameof(Professional);
        public static string Rating = nameof(Rating);
        public static string RestartExplorer = nameof(RestartExplorer);
        public static string RestartExplorerTooltip = nameof(RestartExplorerTooltip);
        public static string ShowRatingBadgeTooltip = nameof(ShowRatingBadgeTooltip);
        public static string ShowPosterWindowTooltip = nameof(ShowPosterWindowTooltip);
        public static string ShowRatingBadge = nameof(ShowRatingBadge);
        public static string Title = nameof(Title);
        public static string TV = nameof(TV);
        public static string UsePosterOverlay = nameof(UsePosterOverlay);
        public static string Year = nameof(Year);
        public static string IconMode = nameof(IconMode);
        public static string SearchMode = nameof(SearchMode);
    }
}