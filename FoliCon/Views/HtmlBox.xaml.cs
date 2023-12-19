using HandyControl.Themes;
using Microsoft.Web.WebView2.Core;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for HtmlBox.xaml
/// </summary>
public partial class HtmlBox
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string VideoUnavailable = "Video not available!";
    private readonly string _backgroundColor;
    
    public static readonly DependencyProperty HtmlTextProperty
        = DependencyProperty.Register(
            nameof(HtmlText), 
            typeof(string), 
            typeof(HtmlBox), 
            new PropertyMetadata(default(string), OnHtmlTextPropertyChanged));

    public bool IsVideoAvailable { get; private set; }

    public HtmlBox()
    {
        InitializeComponent();
        _backgroundColor = ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark ? "#1C1C1C" : "#FFFFFF";
    }

    public string HtmlText
    {
        get => (string)GetValue(HtmlTextProperty);
        set => SetValue(HtmlTextProperty, value);
    }
    
    private async Task ProcessBrowse()
    {
        if (Browser is not {IsLoaded: true}) return;
        
        var content = !IsVideoAvailable
            ? $"""<html><body style="background-color: {_backgroundColor}"></body></html>"""
            : GenerateHtmlContent();

        await InitializeAsync(content);
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
        Logger.Info("Initializing WebView2");
        CheckWebView2();
        await Browser.EnsureCoreWebView2Async(null);
        Browser.DefaultBackgroundColor = ColorTranslator.FromHtml(_backgroundColor);
        Browser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        Browser.CoreWebView2.NavigateToString(content);
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
    
    public static void CheckWebView2()
    {
        try
        {
            Logger.Info("Checking WebView2 Runtime availability");
            var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Logger.Info($"WebView2 Runtime version {availableBrowserVersionString} is available");
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            Logger.ForErrorEvent().Message("WebView2 Runtime is not installed.").Exception(exception).Log();
            // WebView2 Runtime is not installed. Show a meaningful error message to the user.
            MessageBox.Show("Microsoft WebView2 Runtime is not found. Please install.", "Error");
        }
    }
}