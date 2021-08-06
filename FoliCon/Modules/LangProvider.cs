using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HandyControl.Tools;

namespace FoliCon.Properties.Langs
{
    public class LangProvider : INotifyPropertyChanged
    {
        private static string _cultureInfoStr;
        internal static LangProvider Instance => ResourceHelper.GetResource<LangProvider>("FoliConLangs");
        public static string GetLang(string key) => Lang.ResourceManager.GetString(key, Culture);

        public static void SetLang(DependencyObject dependencyObject, DependencyProperty dependencyProperty, string key)
        {
            BindingOperations.SetBinding(dependencyObject, dependencyProperty, new Binding(key)
            {
                Source = Instance,
                Mode = BindingMode.OneWay
            });
        }

        public static CultureInfo Culture
        {
            get => Lang.Culture;
            set
            {
                if (value == null) return;
                if (Equals(_cultureInfoStr, value.EnglishName)) return;
                Lang.Culture = value;
                _cultureInfoStr = value.EnglishName;

                Instance.UpdateLangs();
            }
        }

        private void UpdateLangs()
        {
            OnPropertyChanged(nameof(About));
            OnPropertyChanged(nameof(All));
            OnPropertyChanged(nameof(AlwaysShowPosterWindow));
            OnPropertyChanged(nameof(AmbiguousTitleTooltip));
            OnPropertyChanged(nameof(APIKeysConfiguration));
            OnPropertyChanged(nameof(APIKeysNotProvided));
            OnPropertyChanged(nameof(Apply));
            OnPropertyChanged(nameof(AppWillClose));
            OnPropertyChanged(nameof(Arabic));
            OnPropertyChanged(nameof(Auto));
            OnPropertyChanged(nameof(BrowseFolderDropHere));
            OnPropertyChanged(nameof(BrowseIconsDropHere));
            OnPropertyChanged(nameof(Cancel));
            OnPropertyChanged(nameof(ChangeLanguage));
            OnPropertyChanged(nameof(ChangePosterOverlay));
            OnPropertyChanged(nameof(CheckForUpdate));
            OnPropertyChanged(nameof(ClientID));
            OnPropertyChanged(nameof(ClientSecret));
            OnPropertyChanged(nameof(Close));
            OnPropertyChanged(nameof(ClosingApplication));
            OnPropertyChanged(nameof(Confirm));
            OnPropertyChanged(nameof(ConfirmExplorerRestart));
            OnPropertyChanged(nameof(ConfirmIconDeletion));
            OnPropertyChanged(nameof(ConfirmToOpenFolder));
            OnPropertyChanged(nameof(CreatingIcons));
            OnPropertyChanged(nameof(CustomIconSetter));
            OnPropertyChanged(nameof(CustomRating));
            OnPropertyChanged(nameof(CustomRatingTooltip));
            OnPropertyChanged(nameof(DeleteIcons));
            OnPropertyChanged(nameof(DeleteIconsConfirmation));
            OnPropertyChanged(nameof(DeleteIconsTooltip));
            OnPropertyChanged(nameof(DevelopedByDinesh));
            OnPropertyChanged(nameof(DirectoryIsEmpty));
            OnPropertyChanged(nameof(DownloadingIcons));
            OnPropertyChanged(nameof(DownloadingIconWithCount));
            OnPropertyChanged(nameof(DownloadIt));
            OnPropertyChanged(nameof(EmptyDirectory));
            OnPropertyChanged(nameof(English));
            OnPropertyChanged(nameof(EnterTitlePlaceholder));
            OnPropertyChanged(nameof(Folder));
            OnPropertyChanged(nameof(FolderDoesNotExist));
            OnPropertyChanged(nameof(FolderError));
            OnPropertyChanged(nameof(Folders));
            OnPropertyChanged(nameof(FoldersProcessed));
            OnPropertyChanged(nameof(FoliConDescription));
            OnPropertyChanged(nameof(Game));
            OnPropertyChanged(nameof(HaveIcons));
            OnPropertyChanged(nameof(HaveIconsTooltip));
            OnPropertyChanged(nameof(Help));
            OnPropertyChanged(nameof(HelpDocument));
            OnPropertyChanged(nameof(IconCreated));
            OnPropertyChanged(nameof(IconCreatedWithCount));
            OnPropertyChanged(nameof(IconMode));
            OnPropertyChanged(nameof(IconReloadMayTakeTime));
            OnPropertyChanged(nameof(Icons));
            OnPropertyChanged(nameof(IconsAlready));
            OnPropertyChanged(nameof(IconsCreated));
            OnPropertyChanged(nameof(Idle));
            OnPropertyChanged(nameof(Ignore));
            OnPropertyChanged(nameof(IgnoreAmbiguousTitle));
            OnPropertyChanged(nameof(InvalidPath));
            OnPropertyChanged(nameof(License));
            OnPropertyChanged(nameof(LicenseInfo));
            OnPropertyChanged(nameof(Load));
            OnPropertyChanged(nameof(LoadingPosters));
            OnPropertyChanged(nameof(MakeIcons));
            OnPropertyChanged(nameof(Movie));
            OnPropertyChanged(nameof(NetworkError));
            OnPropertyChanged(nameof(NetworkNotAvailable));
            OnPropertyChanged(nameof(NewVersionFound));
            OnPropertyChanged(nameof(NoFolderOrIconAlready));
            OnPropertyChanged(nameof(NoFoldersToProcess));
            OnPropertyChanged(nameof(NoIconsSelected));
            OnPropertyChanged(nameof(NoIconsToApply));
            OnPropertyChanged(nameof(NoInternet));
            OnPropertyChanged(nameof(NoPosterFound));
            OnPropertyChanged(nameof(NoResult));
            OnPropertyChanged(nameof(NoResultFound));
            OnPropertyChanged(nameof(NoResultFoundTryCorrectTitle));
            OnPropertyChanged(nameof(NothingFoundFor));
            OnPropertyChanged(nameof(OK));
            OnPropertyChanged(nameof(OnlyKeepExactMatches));
            OnPropertyChanged(nameof(Or));
            OnPropertyChanged(nameof(OutOf));
            OnPropertyChanged(nameof(PickIconWithName));
            OnPropertyChanged(nameof(PickSelected));
            OnPropertyChanged(nameof(Poster));
            OnPropertyChanged(nameof(PosterOverlayTooltip));
            OnPropertyChanged(nameof(Professional));
            OnPropertyChanged(nameof(Rating));
            OnPropertyChanged(nameof(RestartExplorer));
            OnPropertyChanged(nameof(RestartExplorerConfirmation));
            OnPropertyChanged(nameof(RestartExplorerTooltip));
            OnPropertyChanged(nameof(Russian));
            OnPropertyChanged(nameof(Save));
            OnPropertyChanged(nameof(Searching));
            OnPropertyChanged(nameof(SearchingWithCount));
            OnPropertyChanged(nameof(SearchingWithName));
            OnPropertyChanged(nameof(SearchMode));
            OnPropertyChanged(nameof(SearchResult));
            OnPropertyChanged(nameof(SeeMorePosters));
            OnPropertyChanged(nameof(SelectFolder));
            OnPropertyChanged(nameof(SelectIconsDirectory));
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(ShowMore));
            OnPropertyChanged(nameof(ShowPosterWindowTooltip));
            OnPropertyChanged(nameof(ShowRatingBadge));
            OnPropertyChanged(nameof(ShowRatingBadgeTooltip));
            OnPropertyChanged(nameof(Skip));
            OnPropertyChanged(nameof(SkipThisPlaceholder));
            OnPropertyChanged(nameof(SkipThisTitle));
            OnPropertyChanged(nameof(Spanish));
            OnPropertyChanged(nameof(StopSearching));
            OnPropertyChanged(nameof(ThisIsLatestVersion));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(TMDBAPIKey));
            OnPropertyChanged(nameof(ToForceReload));
            OnPropertyChanged(nameof(TV));
            OnPropertyChanged(nameof(Undo));
            OnPropertyChanged(nameof(UndoSuccessful));
            OnPropertyChanged(nameof(UpdateNow));
            OnPropertyChanged(nameof(UsePosterOverlay));
            OnPropertyChanged(nameof(Version));
            OnPropertyChanged(nameof(Year));
            OnPropertyChanged(nameof(DeleteMediaInfo));
            OnPropertyChanged(nameof(DeleteMediaInfoTooltip));
            OnPropertyChanged(nameof(DeleteMediaInfoConfirmation));
            OnPropertyChanged(nameof(ConfirmMediaInfoDeletion));
        }

        public string About => Lang.About;

        public string All => Lang.All;

        public string AlwaysShowPosterWindow => Lang.AlwaysShowPosterWindow;

        public string AmbiguousTitleTooltip => Lang.AmbiguousTitleTooltip;

        public string APIKeysConfiguration => Lang.APIKeysConfiguration;

        public string APIKeysNotProvided => Lang.APIKeysNotProvided;

        public string Apply => Lang.Apply;

        public string AppWillClose => Lang.AppWillClose;

        public string Arabic => Lang.Arabic;

        public string Auto => Lang.Auto;

        public string BrowseFolderDropHere => Lang.BrowseFolderDropHere;

        public string BrowseIconsDropHere => Lang.BrowseIconsDropHere;

        public string Cancel => Lang.Cancel;

        public string ChangeLanguage => Lang.ChangeLanguage;

        public string ChangePosterOverlay => Lang.ChangePosterOverlay;

        public string CheckForUpdate => Lang.CheckForUpdate;

        public string ClientID => Lang.ClientID;

        public string ClientSecret => Lang.ClientSecret;

        public string Close => Lang.Close;

        public string ClosingApplication => Lang.ClosingApplication;

        public string Confirm => Lang.Confirm;

        public string ConfirmExplorerRestart => Lang.ConfirmExplorerRestart;

        public string ConfirmIconDeletion => Lang.ConfirmIconDeletion;

        public string ConfirmToOpenFolder => Lang.ConfirmToOpenFolder;

        public string CreatingIcons => Lang.CreatingIcons;

        public string CustomIconSetter => Lang.CustomIconSetter;

        public string CustomRating => Lang.CustomRating;

        public string CustomRatingTooltip => Lang.CustomRatingTooltip;

        public string DeleteIcons => Lang.DeleteIcons;

        public string DeleteIconsConfirmation => Lang.DeleteIconsConfirmation;

        public string DeleteIconsTooltip => Lang.DeleteIconsTooltip;

        public string DevelopedByDinesh => Lang.DevelopedByDinesh;

        public string DirectoryIsEmpty => Lang.DirectoryIsEmpty;

        public string DownloadingIcons => Lang.DownloadingIcons;

        public string DownloadingIconWithCount => Lang.DownloadingIconWithCount;

        public string DownloadIt => Lang.DownloadIt;

        public string EmptyDirectory => Lang.EmptyDirectory;

        public string English => Lang.English;

        public string EnterTitlePlaceholder => Lang.EnterTitlePlaceholder;

        public string Folder => Lang.Folder;

        public string FolderDoesNotExist => Lang.FolderDoesNotExist;

        public string FolderError => Lang.FolderError;

        public string Folders => Lang.Folders;

        public string FoldersProcessed => Lang.FoldersProcessed;

        public string FoliConDescription => Lang.FoliConDescription;

        public string Game => Lang.Game;

        public string HaveIcons => Lang.HaveIcons;

        public string HaveIconsTooltip => Lang.HaveIconsTooltip;

        public string Help => Lang.Help;

        public string HelpDocument => Lang.HelpDocument;

        public string IconCreated => Lang.IconCreated;

        public string IconCreatedWithCount => Lang.IconCreatedWithCount;

        public string IconMode => Lang.IconMode;

        public string IconReloadMayTakeTime => Lang.IconReloadMayTakeTime;

        public string Icons => Lang.Icons;

        public string IconsAlready => Lang.IconsAlready;

        public string IconsCreated => Lang.IconsCreated;

        public string Idle => Lang.Idle;

        public string Ignore => Lang.Ignore;

        public string IgnoreAmbiguousTitle => Lang.IgnoreAmbiguousTitle;

        public string InvalidPath => Lang.InvalidPath;

        public string License => Lang.License;

        public string LicenseInfo => Lang.LicenseInfo;

        public string Load => Lang.Load;

        public string LoadingPosters => Lang.LoadingPosters;

        public string MakeIcons => Lang.MakeIcons;

        public string Movie => Lang.Movie;

        public string NetworkError => Lang.NetworkError;

        public string NetworkNotAvailable => Lang.NetworkNotAvailable;

        public string NewVersionFound => Lang.NewVersionFound;

        public string NoFolderOrIconAlready => Lang.NoFolderOrIconAlready;

        public string NoFoldersToProcess => Lang.NoFoldersToProcess;

        public string NoIconsSelected => Lang.NoIconsSelected;

        public string NoIconsToApply => Lang.NoIconsToApply;

        public string NoInternet => Lang.NoInternet;

        public string NoPosterFound => Lang.NoPosterFound;

        public string NoResult => Lang.NoResult;

        public string NoResultFound => Lang.NoResultFound;

        public string NoResultFoundTryCorrectTitle => Lang.NoResultFoundTryCorrectTitle;

        public string NothingFoundFor => Lang.NothingFoundFor;

        public string OK => Lang.OK;

        public string OnlyKeepExactMatches => Lang.OnlyKeepExactMatches;

        public string Or => Lang.Or;

        public string OutOf => Lang.OutOf;

        public string PickIconWithName => Lang.PickIconWithName;

        public string PickSelected => Lang.PickSelected;

        public string Poster => Lang.Poster;

        public string PosterOverlayTooltip => Lang.PosterOverlayTooltip;

        public string Professional => Lang.Professional;

        public string Rating => Lang.Rating;

        public string RestartExplorer => Lang.RestartExplorer;

        public string RestartExplorerConfirmation => Lang.RestartExplorerConfirmation;

        public string RestartExplorerTooltip => Lang.RestartExplorerTooltip;

        public string Russian => Lang.Russian;

        public string Save => Lang.Save;

        public string Searching => Lang.Searching;

        public string SearchingWithCount => Lang.SearchingWithCount;

        public string SearchingWithName => Lang.SearchingWithName;

        public string SearchMode => Lang.SearchMode;

        public string SearchResult => Lang.SearchResult;

        public string SeeMorePosters => Lang.SeeMorePosters;

        public string SelectFolder => Lang.SelectFolder;

        public string SelectIconsDirectory => Lang.SelectIconsDirectory;

        public string Settings => Lang.Settings;

        public string ShowMore => Lang.ShowMore;

        public string ShowPosterWindowTooltip => Lang.ShowPosterWindowTooltip;

        public string ShowRatingBadge => Lang.ShowRatingBadge;

        public string ShowRatingBadgeTooltip => Lang.ShowRatingBadgeTooltip;

        public string Skip => Lang.Skip;

        public string SkipThisPlaceholder => Lang.SkipThisPlaceholder;

        public string SkipThisTitle => Lang.SkipThisTitle;

        public string Spanish => Lang.Spanish;

        public string StopSearching => Lang.StopSearching;

        public string ThisIsLatestVersion => Lang.ThisIsLatestVersion;

        public string Title => Lang.Title;

        public string TMDBAPIKey => Lang.TMDBAPIKey;

        public string ToForceReload => Lang.ToForceReload;

        public string TV => Lang.TV;

        public string Undo => Lang.Undo;

        public string UndoSuccessful => Lang.UndoSuccessful;

        public string UpdateNow => Lang.UpdateNow;

        public string UsePosterOverlay => Lang.UsePosterOverlay;

        public string Version => Lang.Version;

        public string Year => Lang.Year;
        public string ExceptionOccurred => Lang.ExceptionOccurred;
        public string ConfirmMediaInfoDeletion => Lang.ConfirmMediaInfoDeletion;
        public string DeleteMediaInfoConfirmation => Lang.DeleteMediaInfoConfirmation;
        public string DeleteMediaInfoTooltip => Lang.DeleteMediaInfoTooltip;
        public string DeleteMediaInfo => Lang.DeleteMediaInfo;


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LangKeys
    {
        public static string About = nameof(About);

        public static string All = nameof(All);

        public static string AlwaysShowPosterWindow = nameof(AlwaysShowPosterWindow);

        public static string AmbiguousTitleTooltip = nameof(AmbiguousTitleTooltip);

        public static string APIKeysConfiguration = nameof(APIKeysConfiguration);

        public static string APIKeysNotProvided = nameof(APIKeysNotProvided);

        public static string Apply = nameof(Apply);

        public static string AppWillClose = nameof(AppWillClose);

        public static string Arabic = nameof(Arabic);

        public static string Auto = nameof(Auto);

        public static string BrowseFolderDropHere = nameof(BrowseFolderDropHere);

        public static string BrowseIconsDropHere = nameof(BrowseIconsDropHere);

        public static string Cancel = nameof(Cancel);

        public static string ChangeLanguage = nameof(ChangeLanguage);

        public static string ChangePosterOverlay = nameof(ChangePosterOverlay);

        public static string CheckForUpdate = nameof(CheckForUpdate);

        public static string ClientID = nameof(ClientID);

        public static string ClientSecret = nameof(ClientSecret);

        public static string Close = nameof(Close);

        public static string ClosingApplication = nameof(ClosingApplication);

        public static string Confirm = nameof(Confirm);

        public static string ConfirmExplorerRestart = nameof(ConfirmExplorerRestart);

        public static string ConfirmIconDeletion = nameof(ConfirmIconDeletion);

        public static string ConfirmToOpenFolder = nameof(ConfirmToOpenFolder);

        public static string CreatingIcons = nameof(CreatingIcons);

        public static string CustomIconSetter = nameof(CustomIconSetter);

        public static string CustomRating = nameof(CustomRating);

        public static string CustomRatingTooltip = nameof(CustomRatingTooltip);

        public static string DeleteIcons = nameof(DeleteIcons);

        public static string DeleteIconsConfirmation = nameof(DeleteIconsConfirmation);

        public static string DeleteIconsTooltip = nameof(DeleteIconsTooltip);

        public static string DevelopedByDinesh = nameof(DevelopedByDinesh);

        public static string DirectoryIsEmpty = nameof(DirectoryIsEmpty);

        public static string DownloadingIcons = nameof(DownloadingIcons);

        public static string DownloadingIconWithCount = nameof(DownloadingIconWithCount);

        public static string DownloadIt = nameof(DownloadIt);

        public static string EmptyDirectory = nameof(EmptyDirectory);

        public static string English = nameof(English);

        public static string EnterTitlePlaceholder = nameof(EnterTitlePlaceholder);

        public static string Folder = nameof(Folder);

        public static string FolderDoesNotExist = nameof(FolderDoesNotExist);

        public static string FolderError = nameof(FolderError);

        public static string Folders = nameof(Folders);

        public static string FoldersProcessed = nameof(FoldersProcessed);

        public static string FoliConDescription = nameof(FoliConDescription);

        public static string Game = nameof(Game);

        public static string HaveIcons = nameof(HaveIcons);

        public static string HaveIconsTooltip = nameof(HaveIconsTooltip);

        public static string Help = nameof(Help);

        public static string HelpDocument = nameof(HelpDocument);

        public static string IconCreated = nameof(IconCreated);

        public static string IconCreatedWithCount = nameof(IconCreatedWithCount);

        public static string IconMode = nameof(IconMode);

        public static string IconReloadMayTakeTime = nameof(IconReloadMayTakeTime);

        public static string Icons = nameof(Icons);

        public static string IconsAlready = nameof(IconsAlready);

        public static string IconsCreated = nameof(IconsCreated);

        public static string Idle = nameof(Idle);

        public static string Ignore = nameof(Ignore);

        public static string IgnoreAmbiguousTitle = nameof(IgnoreAmbiguousTitle);

        public static string InvalidPath = nameof(InvalidPath);

        public static string License = nameof(License);

        public static string LicenseInfo = nameof(LicenseInfo);

        public static string Load = nameof(Load);

        public static string LoadingPosters = nameof(LoadingPosters);

        public static string MakeIcons = nameof(MakeIcons);

        public static string Movie = nameof(Movie);

        public static string NetworkError = nameof(NetworkError);

        public static string NetworkNotAvailable = nameof(NetworkNotAvailable);

        public static string NewVersionFound = nameof(NewVersionFound);

        public static string NoFolderOrIconAlready = nameof(NoFolderOrIconAlready);

        public static string NoFoldersToProcess = nameof(NoFoldersToProcess);

        public static string NoIconsSelected = nameof(NoIconsSelected);

        public static string NoIconsToApply = nameof(NoIconsToApply);

        public static string NoInternet = nameof(NoInternet);

        public static string NoPosterFound = nameof(NoPosterFound);

        public static string NoResult = nameof(NoResult);

        public static string NoResultFound = nameof(NoResultFound);

        public static string NoResultFoundTryCorrectTitle = nameof(NoResultFoundTryCorrectTitle);

        public static string NothingFoundFor = nameof(NothingFoundFor);

        public static string OK = nameof(OK);

        public static string OnlyKeepExactMatches = nameof(OnlyKeepExactMatches);

        public static string Or = nameof(Or);

        public static string OutOf = nameof(OutOf);

        public static string PickIconWithName = nameof(PickIconWithName);

        public static string PickSelected = nameof(PickSelected);

        public static string Poster = nameof(Poster);

        public static string PosterOverlayTooltip = nameof(PosterOverlayTooltip);

        public static string Professional = nameof(Professional);

        public static string Rating = nameof(Rating);

        public static string RestartExplorer = nameof(RestartExplorer);

        public static string RestartExplorerConfirmation = nameof(RestartExplorerConfirmation);

        public static string RestartExplorerTooltip = nameof(RestartExplorerTooltip);

        public static string Russian = nameof(Russian);

        public static string Save = nameof(Save);

        public static string Searching = nameof(Searching);

        public static string SearchingWithCount = nameof(SearchingWithCount);

        public static string SearchingWithName = nameof(SearchingWithName);

        public static string SearchMode = nameof(SearchMode);

        public static string SearchResult = nameof(SearchResult);

        public static string SeeMorePosters = nameof(SeeMorePosters);

        public static string SelectFolder = nameof(SelectFolder);

        public static string SelectIconsDirectory = nameof(SelectIconsDirectory);

        public static string Settings = nameof(Settings);

        public static string ShowMore = nameof(ShowMore);

        public static string ShowPosterWindowTooltip = nameof(ShowPosterWindowTooltip);

        public static string ShowRatingBadge = nameof(ShowRatingBadge);

        public static string ShowRatingBadgeTooltip = nameof(ShowRatingBadgeTooltip);

        public static string Skip = nameof(Skip);

        public static string SkipThisPlaceholder = nameof(SkipThisPlaceholder);

        public static string SkipThisTitle = nameof(SkipThisTitle);

        public static string Spanish = nameof(Spanish);

        public static string StopSearching = nameof(StopSearching);

        public static string ThisIsLatestVersion = nameof(ThisIsLatestVersion);

        public static string Title = nameof(Title);

        public static string TMDBAPIKey = nameof(TMDBAPIKey);

        public static string ToForceReload = nameof(ToForceReload);

        public static string TV = nameof(TV);

        public static string Undo = nameof(Undo);

        public static string UndoSuccessful = nameof(UndoSuccessful);

        public static string UpdateNow = nameof(UpdateNow);

        public static string UsePosterOverlay = nameof(UsePosterOverlay);

        public static string Version = nameof(Version);

        public static string Year = nameof(Year);

        public static string ExceptionOccurred = nameof(ExceptionOccurred);
        public static string ConfirmMediaInfoDeletion = nameof(ConfirmMediaInfoDeletion);
        public static string DeleteMediaInfoConfirmation = nameof(DeleteMediaInfoConfirmation);
        public static string DeleteMediaInfoTooltip = nameof(DeleteMediaInfoTooltip);
        public static string DeleteMediaInfo = nameof(DeleteMediaInfo);
    }
}