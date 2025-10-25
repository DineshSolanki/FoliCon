using HandyControl.Themes;
using Microsoft.Web.WebView2.Core;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for HtmlBox.xaml
/// </summary>
[Localizable(false)]
public partial class HtmlBox
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string VideoUnavailable = "Video not available!";
    private readonly string _backgroundColor;
    private const string DarkGray = "#1C1C1C";
    private const string White = "#FFFFFF";

    // Serialize and share WebView2 environment creation across controls
    private static readonly SemaphoreSlim EnvLock = new(1, 1);
    private static Task<CoreWebView2Environment>? _sharedEnvTask;

    // Serialize initialization per control to avoid concurrent EnsureCoreWebView2Async
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public static readonly DependencyProperty HtmlTextProperty
        = DependencyProperty.Register(
            nameof(HtmlText), 
            typeof(string), 
            typeof(HtmlBox), 
            new PropertyMetadata(default(string), OnHtmlTextPropertyChanged));

    public static readonly DependencyProperty VideosProperty
        = DependencyProperty.Register(
            nameof(Videos), 
            typeof(ICollection<MediaVideo>), 
            typeof(HtmlBox), 
            new PropertyMetadata(default(ICollection<MediaVideo>), OnVideosPropertyChanged));

    private static async void OnVideosPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HtmlBox control)
        {
            return;
        }
        await control.Dispatcher.InvokeAsync(() =>
        {
            control.VideoSelector.ItemsSource = e.NewValue as ICollection<MediaVideo>;
            control.VideoSelector.SelectedIndex = 0;
        });
    }

    private bool IsVideoAvailable { get; set; }

    public HtmlBox()
    {
        InitializeComponent();
        _backgroundColor = ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark ? DarkGray : White;
       InitializeVideoSelector();
    }

    public string HtmlText
    {
        get => (string)GetValue(HtmlTextProperty);
        set => SetValue(HtmlTextProperty, value);
    }
    
    public ICollection<MediaVideo> Videos
    {
        get => (ICollection<MediaVideo>)GetValue(VideosProperty);
        set => SetValue(VideosProperty, value);
    }
    private async Task ProcessBrowse()
    {
        if (Browser is not {IsLoaded: true})
        {
            return;
        }

        var content = GenerateContentString();

        await InitializeAsync(content);
    }

    private string GenerateContentString()
    {
        return !IsVideoAvailable
            ? $"""<html><body style="background-color: {_backgroundColor}"></body></html>"""
            : GenerateHtmlContent();
    }

    private string GenerateHtmlContent()
    {
        return string.Format(HtmlTemplate, LangProvider.Culture.TwoLetterISOLanguageName, _backgroundColor, HtmlText);
    }
    
    private static async void OnHtmlTextPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        var htmlText = e.NewValue as string;
        if (source is not HtmlBox control)
        {
            return;
        }
        control.IsVideoAvailable = !string.IsNullOrEmpty(htmlText) && !htmlText.Contains(VideoUnavailable);
        if (control.CheckAccess())
        {
            await control.ProcessBrowse();
        }
        else
        {
            await control.Dispatcher.InvokeAsync(control.ProcessBrowse);
        }
    }

    private async Task InitializeAsync(string content)
    {
        // Prevent re-entrant initialization on rapid property changes
        await _initLock.WaitAsync();
        try
        {
            if (Browser.CoreWebView2 == null)
            {
                Logger.Info("Initializing WebView2 environment and control");
                var env = await EnsureSharedEnvironmentAsyncWithRetry();
                await EnsureWebView2AsyncWithRetry(env);

                // Apply settings once after initialization
                Browser.DefaultBackgroundColor = ColorTranslator.FromHtml(_backgroundColor);
                if (Browser.CoreWebView2 != null)
                {
                    Browser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                    Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                }
            }

            // Navigate after initialization (or if already initialized)
            Browser.CoreWebView2?.NavigateToString(content);
        }
        finally
        {
            _initLock.Release();
        }
    }

    // Create or reuse a shared environment with a dedicated, stable user data folder
    private static async Task<CoreWebView2Environment> EnsureSharedEnvironmentAsyncWithRetry()
    {
        await EnvLock.WaitAsync();
        try
        {
            if (_sharedEnvTask != null)
            {
                return await _sharedEnvTask.ConfigureAwait(false);
            }

            var userDataFolder = GetUserDataFolder();
            EnsureUserDataFolderReady(userDataFolder);

            _sharedEnvTask = CreateEnvironmentWithRetryAsync(userDataFolder);
            return await _sharedEnvTask.ConfigureAwait(false);
        }
        finally
        {
            EnvLock.Release();
        }
    }

    private static async Task<CoreWebView2Environment> CreateEnvironmentWithRetryAsync(string userDataFolder)
    {
        var delays = new[] { 200, 500, 1000 }; // milliseconds
        for (var attempt = 0; attempt <= delays.Length; attempt++)
        {
            try
            {
                var options = new CoreWebView2EnvironmentOptions
                {
                    // Keep extensions off and defaults simple to reduce disk activity
                    AreBrowserExtensionsEnabled = false
                };
                return await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            }
            catch (COMException ex) when (IsAbortOrSharingViolation(ex))
            {
                if (attempt == delays.Length)
                {
                    Logger.ForErrorEvent().Message("WebView2 environment creation failed after retries: {Message}", ex.Message).Exception(ex).Log();
                    throw;
                }
                await Task.Delay(delays[attempt]);
            }
            catch (Exception ex)
            {
                if (attempt == delays.Length)
                {
                    Logger.ForErrorEvent().Message("WebView2 environment creation failed: {Message}", ex.Message).Exception(ex).Log();
                    throw;
                }
                await Task.Delay(delays[attempt]);
            }
        }
        // Should not reach here
        throw new InvalidOperationException("Failed to create WebView2 environment.");
    }

    private async Task EnsureWebView2AsyncWithRetry(CoreWebView2Environment env)
    {
        var delays = new[] { 200, 500, 1000 };
        for (var attempt = 0; attempt <= delays.Length; attempt++)
        {
            try
            {
                await Browser.EnsureCoreWebView2Async(env);
                return;
            }
            catch (COMException ex) when (IsAbortOrSharingViolation(ex))
            {
                if (attempt == delays.Length)
                {
                    Logger.ForErrorEvent().Message("WebView2 initialization failed after retries: {Message}", ex.Message).Exception(ex).Log();
                    throw;
                }
                await Task.Delay(delays[attempt]);
            }
        }
    }

    private static bool IsAbortOrSharingViolation(COMException ex)
    {
        const int eAbort = unchecked((int)0x80004004);
        const int errorSharingViolation = 0x20;
        if (ex.HResult == eAbort)
        {
            return true;
        }
        // Some COMExceptions wrap Win32 errors; check Data if present
        if (!ex.Data.Contains("HRForWin32Error")) return false;
        try
        {
            var hr = Convert.ToInt32(ex.Data["HRForWin32Error"]);
            return hr == errorSharingViolation;
        }
        catch
        {
            // ignore
        }
        return false;
    }

    private static string GetUserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // Use app-specific folder to avoid contention with other apps and avoid our own icon operations
        var folder = Path.Combine(localAppData, "FoliCon", "WebView2UserData");
        return folder;
    }

    private static void EnsureUserDataFolderReady(string userDataFolder)
    {
        if (userDataFolder == null) return;
        try
        {
            if (!Directory.Exists(userDataFolder))
            {
                Directory.CreateDirectory(userDataFolder);
            }

            // Normalize attributes to avoid interference from Hidden/System flags
            var attrs = File.GetAttributes(userDataFolder);
            var normalized = attrs & ~(FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
            if (attrs != normalized)
            {
                File.SetAttributes(userDataFolder, normalized);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal; log and continue. WebView2 will still attempt creation and may succeed.
            Logger.ForWarnEvent().Message("Failed to normalize WebView2 user data folder attributes: {Message}", ex.Message).Exception(ex).Log();
        }
    }
    
    private const string HtmlTemplate = """
                                        
                                                    <html lang="{0}">
                                                        <head>
                                                            <meta name='viewport' content='width=device-width, initial-scale=1'>
                                                            <style>
                                                                html, body {{
                                                                    overflow: hidden;
                                                                    margin: 0;
                                                                    padding: 0;
                                                                    width: 100%;
                                                                    height: 100%;
                                                                    box-sizing: border-box;
                                                                }}
                                                            </style>
                                                        </head>
                                                        <body style="background-color: {1}">
                                                            <iframe id='video' allow='autoplay; fullscreen; clipboard-write; encrypted-media; picture-in-picture' allowfullscreen
                                                                src='{2}?hl={0}'
                                                                style="visibility:hidden;" onload="this.style.visibility='visible';">
                                                            </iframe>
                                                            <script src='https://code.jquery.com/jquery-latest.min.js' type='text/javascript'></script>
                                                            <script>
                                                                $(function() {{
                                                                    $('#video').css({{
                                                                        width: $(window).innerWidth() + 'px',
                                                                        height: $(window).innerHeight() + 'px',
                                                                        border: 'none'
                                                                    }});
                                                                    $(window).resize(function() {{
                                                                        $('#video').css({{
                                                                            width: $(window).innerWidth() + 'px',
                                                                            height: $(window).innerHeight() + 'px'
                                                                        }});
                                                                    }});
                                                                }});
                                                            </script>
                                                        </body>
                                                    </html>
                                        """;

    private async void VideoSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        HtmlText = VideoSelector.SelectedValue?.ToString() ?? string.Empty;
        await ProcessBrowse();
    }
    
    private void InitializeVideoSelector()
    {
        VideoSelector.ItemsSource = Videos;
        VideoSelector.SelectedValuePath = "Url";
        VideoSelector.DisplayMemberPath = "Name";
        VideoSelector.SelectedIndex = 0;
    }
}