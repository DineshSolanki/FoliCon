using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HandyControl.Tools;

namespace HandyControlDemo.Properties.Langs
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
			OnPropertyChanged(nameof(ShowHideRatingShield));
			OnPropertyChanged(nameof(ShowPosterWindowTooltip));
			OnPropertyChanged(nameof(ShowRatingBadge));
			OnPropertyChanged(nameof(Title));
			OnPropertyChanged(nameof(TV));
			OnPropertyChanged(nameof(UsePosterOverlay));
			OnPropertyChanged(nameof(Year));
        }

        /// <summary>
        ///   s
        /// </summary>
		public string About => Lang.About;

        /// <summary>
        ///   s
        /// </summary>
		public string AlwaysShowPosterWindow => Lang.AlwaysShowPosterWindow;

        /// <summary>
        ///   s
        /// </summary>
		public string AmbiguousTitleTooltip => Lang.AmbiguousTitleTooltip;

        /// <summary>
        ///   s
        /// </summary>
		public string APIKeysConfiguration => Lang.APIKeysConfiguration;

        /// <summary>
        ///   s
        /// </summary>
		public string Auto => Lang.Auto;

        /// <summary>
        ///   s
        /// </summary>
		public string Cancel => Lang.Cancel;

        /// <summary>
        ///   s
        /// </summary>
		public string ChangePosterOverlay => Lang.ChangePosterOverlay;

        /// <summary>
        ///   s
        /// </summary>
		public string CheckForUpdate => Lang.CheckForUpdate;

        /// <summary>
        ///   s
        /// </summary>
		public string DeleteIcons => Lang.DeleteIcons;

        /// <summary>
        ///   s
        /// </summary>
		public string DeleteIconsTooltip => Lang.DeleteIconsTooltip;

        /// <summary>
        ///   s
        /// </summary>
		public string DownloadingIcons => Lang.DownloadingIcons;

        /// <summary>
        ///   s
        /// </summary>
		public string Folder => Lang.Folder;

        /// <summary>
        ///   s
        /// </summary>
		public string FoldersProcessed => Lang.FoldersProcessed;

        /// <summary>
        ///   s
        /// </summary>
		public string Game => Lang.Game;

        /// <summary>
        ///   s
        /// </summary>
		public string HaveIcons => Lang.HaveIcons;

        /// <summary>
        ///   s
        /// </summary>
		public string HaveIconsTooltip => Lang.HaveIconsTooltip;

        /// <summary>
        ///   s
        /// </summary>
		public string HelpDocument => Lang.HelpDocument;

        /// <summary>
        ///   s
        /// </summary>
		public string IconsCreated => Lang.IconsCreated;

        /// <summary>
        ///   s
        /// </summary>
		public string IgnoreAmbiguousTitle => Lang.IgnoreAmbiguousTitle;

        /// <summary>
        ///   s
        /// </summary>
		public string LangComment => Lang.LangComment;

        /// <summary>
        ///   s
        /// </summary>
		public string Load => Lang.Load;

        /// <summary>
        ///   s
        /// </summary>
		public string MakeIcons => Lang.MakeIcons;

        /// <summary>
        ///   s
        /// </summary>
		public string Movie => Lang.Movie;

        /// <summary>
        ///   s
        /// </summary>
		public string OutOf => Lang.OutOf;

        /// <summary>
        ///   s
        /// </summary>
		public string Poster => Lang.Poster;

        /// <summary>
        ///   s
        /// </summary>
		public string PosterOverlayTooltip => Lang.PosterOverlayTooltip;

        /// <summary>
        ///   s
        /// </summary>
		public string Professional => Lang.Professional;

        /// <summary>
        ///   s
        /// </summary>
		public string Rating => Lang.Rating;

        /// <summary>
        ///   s
        /// </summary>
		public string RestartExplorer => Lang.RestartExplorer;

        /// <summary>
        ///   s
        /// </summary>
		public string RestartExplorerTooltip => Lang.RestartExplorerTooltip;

        /// <summary>
        ///   s
        /// </summary>
		public string ShowHideRatingShield => Lang.ShowHideRatingShield;

        /// <summary>
        ///   s
        /// </summary>
		public string ShowPosterWindowTooltip => Lang.ShowPosterWindowTooltip;

        /// <summary>
        ///   s
        /// </summary>
		public string ShowRatingBadge => Lang.ShowRatingBadge;

        /// <summary>
        ///   s
        /// </summary>
		public string Title => Lang.Title;

        /// <summary>
        ///   s
        /// </summary>
		public string TV => Lang.TV;

        /// <summary>
        ///   s
        /// </summary>
		public string UsePosterOverlay => Lang.UsePosterOverlay;

        /// <summary>
        ///   s
        /// </summary>
		public string Year => Lang.Year;


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LangKeys
    {
        /// <summary>
        ///   s
        /// </summary>
		public static string About = nameof(About);

        /// <summary>
        ///   s
        /// </summary>
		public static string AlwaysShowPosterWindow = nameof(AlwaysShowPosterWindow);

        /// <summary>
        ///   s
        /// </summary>
		public static string AmbiguousTitleTooltip = nameof(AmbiguousTitleTooltip);

        /// <summary>
        ///   s
        /// </summary>
		public static string APIKeysConfiguration = nameof(APIKeysConfiguration);

        /// <summary>
        ///   s
        /// </summary>
		public static string Auto = nameof(Auto);

        /// <summary>
        ///   s
        /// </summary>
		public static string Cancel = nameof(Cancel);

        /// <summary>
        ///   s
        /// </summary>
		public static string ChangePosterOverlay = nameof(ChangePosterOverlay);

        /// <summary>
        ///   s
        /// </summary>
		public static string CheckForUpdate = nameof(CheckForUpdate);

        /// <summary>
        ///   s
        /// </summary>
		public static string DeleteIcons = nameof(DeleteIcons);

        /// <summary>
        ///   s
        /// </summary>
		public static string DeleteIconsTooltip = nameof(DeleteIconsTooltip);

        /// <summary>
        ///   s
        /// </summary>
		public static string DownloadingIcons = nameof(DownloadingIcons);

        /// <summary>
        ///   s
        /// </summary>
		public static string Folder = nameof(Folder);

        /// <summary>
        ///   s
        /// </summary>
		public static string FoldersProcessed = nameof(FoldersProcessed);

        /// <summary>
        ///   s
        /// </summary>
		public static string Game = nameof(Game);

        /// <summary>
        ///   s
        /// </summary>
		public static string HaveIcons = nameof(HaveIcons);

        /// <summary>
        ///   s
        /// </summary>
		public static string HaveIconsTooltip = nameof(HaveIconsTooltip);

        /// <summary>
        ///   s
        /// </summary>
		public static string HelpDocument = nameof(HelpDocument);

        /// <summary>
        ///   s
        /// </summary>
		public static string IconsCreated = nameof(IconsCreated);

        /// <summary>
        ///   s
        /// </summary>
		public static string IgnoreAmbiguousTitle = nameof(IgnoreAmbiguousTitle);

        /// <summary>
        ///   s
        /// </summary>
		public static string LangComment = nameof(LangComment);

        /// <summary>
        ///   s
        /// </summary>
		public static string Load = nameof(Load);

        /// <summary>
        ///   s
        /// </summary>
		public static string MakeIcons = nameof(MakeIcons);

        /// <summary>
        ///   s
        /// </summary>
		public static string Movie = nameof(Movie);

        /// <summary>
        ///   s
        /// </summary>
		public static string OutOf = nameof(OutOf);

        /// <summary>
        ///   s
        /// </summary>
		public static string Poster = nameof(Poster);

        /// <summary>
        ///   s
        /// </summary>
		public static string PosterOverlayTooltip = nameof(PosterOverlayTooltip);

        /// <summary>
        ///   s
        /// </summary>
		public static string Professional = nameof(Professional);

        /// <summary>
        ///   s
        /// </summary>
		public static string Rating = nameof(Rating);

        /// <summary>
        ///   s
        /// </summary>
		public static string RestartExplorer = nameof(RestartExplorer);

        /// <summary>
        ///   s
        /// </summary>
		public static string RestartExplorerTooltip = nameof(RestartExplorerTooltip);

        /// <summary>
        ///   s
        /// </summary>
		public static string ShowHideRatingShield = nameof(ShowHideRatingShield);

        /// <summary>
        ///   s
        /// </summary>
		public static string ShowPosterWindowTooltip = nameof(ShowPosterWindowTooltip);

        /// <summary>
        ///   s
        /// </summary>
		public static string ShowRatingBadge = nameof(ShowRatingBadge);

        /// <summary>
        ///   s
        /// </summary>
		public static string Title = nameof(Title);

        /// <summary>
        ///   s
        /// </summary>
		public static string TV = nameof(TV);

        /// <summary>
        ///   s
        /// </summary>
		public static string UsePosterOverlay = nameof(UsePosterOverlay);

        /// <summary>
        ///   s
        /// </summary>
		public static string Year = nameof(Year);

    }
}