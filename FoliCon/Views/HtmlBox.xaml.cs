﻿using HandyControl.Themes;

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
        Logger.Info("Initializing WebView2");
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