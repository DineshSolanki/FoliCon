using System.Windows.Navigation;
using HandyControl.Themes;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for HtmlBox.xaml
/// </summary>
public partial class HtmlBox : UserControl
{
    private const string VideoUnavailable = "Video not available!";
    
    private readonly string _imagePath;

    public static readonly DependencyProperty HtmlTextProperty
        = DependencyProperty.Register(
            nameof(HtmlText), 
            typeof(string), 
            typeof(HtmlBox), 
            new PropertyMetadata(default(string), OnHtmlTextPropertyChanged));

    public bool IsVideoAvailable => !string.IsNullOrEmpty(HtmlText) && !HtmlText.Contains(VideoUnavailable);

    public HtmlBox()
    {
        _imagePath = Util.GetResourcePath("video-unavailable.png");

        InitializeComponent();

        Browser.Visibility = Visibility.Collapsed;
    }

    public string HtmlText
    {
        get => (string)GetValue(HtmlTextProperty);
        set => SetValue(HtmlTextProperty, value);
    }
    private void UpdateBrowserVisibility()
    {
        Browser.Visibility = IsVideoAvailable ? Visibility.Visible : Visibility.Collapsed;
    }
    
    private void ProcessBrowse()
    {
        if (Browser is not {IsLoaded: true}) return;

        if (!IsVideoAvailable)
        {
            return;
        }
        var content = GenerateHtmlContent();
        
        Browser.NavigateToString(content);
    }

    private string GenerateHtmlContent()
    {
        return $$"""
                 
                             <html>
                                 <head>
                                     <meta name='viewport' content='width=device-width, initial-scale=1'>
                                     <meta content='IE=Edge' http-equiv='X-UA-Compatible'>
                                     <style>
                                         html, body {
                                             overflow: hidden;
                                             margin: 0;
                                             padding: 0;
                                             width: 100%;
                                             height: 100%;
                                             box-sizing: border-box;
                                         }
                                     </style>
                                     <script src='https://code.jquery.com/jquery-latest.min.js' type='text/javascript'></script>
                                     <script>
                                         $(function() {
                                             $('#video').css({
                                                 width: $(window).innerWidth() + 'px',
                                                 height: $(window).innerHeight() + 'px'
                                             });
                                             $(window).resize(function() {
                                                 $('#video').css({
                                                     width: $(window).innerWidth() + 'px',
                                                     height: $(window).innerHeight() + 'px'
                                                 });
                                             });
                                         });
                                     </script>
                                 </head>
                                 <body>
                                     <iframe id='video' allow='autoplay; fullscreen; clipboard-write; encrypted-media; picture-in-picture' allowfullscreen='true'
                                         src='{{HtmlText}}' frameborder='0'>
                                     </iframe>
                                 </body>
                             </html>
                 """;
    }

    private string GenerateUnavailableContent()
    {
        return $"""
                
                            <html>
                                <body style='margin:0; padding:0; height: 100%; width: 100%; overflow: hidden;'>
                                    <img src='{_imagePath}' style='height: 100%; width: 100%; object-fit: fill;' />
                                </body>
                            </html>
                """;
    }

    private static void OnHtmlTextPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        var control = source as HtmlBox;
        control?.ProcessBrowse();
        control?.UpdateBrowserVisibility();
    }

    private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
    {
        Browser.Visibility = Visibility.Visible;
    }
}