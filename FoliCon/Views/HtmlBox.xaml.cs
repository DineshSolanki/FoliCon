using HandyControl.Themes;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for HtmlBox.xaml
/// </summary>
public partial class HtmlBox : UserControl
{
    private const string VideoUnavailable = "Video not available!";
    private readonly string _backgroundColor;
    
    public static readonly DependencyProperty HtmlTextProperty
        = DependencyProperty.Register(
            nameof(HtmlText), 
            typeof(string), 
            typeof(HtmlBox), 
            new PropertyMetadata(default(string), OnHtmlTextPropertyChanged));

    public bool IsVideoAvailable => !string.IsNullOrEmpty(HtmlText) && !HtmlText.Contains(VideoUnavailable);

    public HtmlBox()
    {
        InitializeComponent();
        _backgroundColor = ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark ? "#000000" : "#FFFFFF";
    }

    public string HtmlText
    {
        get => (string)GetValue(HtmlTextProperty);
        set => SetValue(HtmlTextProperty, value);
    }
    
    private void ProcessBrowse()
    {
        if (Browser is not {IsLoaded: true}) return;

        if (!IsVideoAvailable)
        {
            Browser.NavigateToString($"""<html><body style="background-color: {_backgroundColor}"></body></html>""");
            return;
        }
        var content = GenerateHtmlContent();
        Browser.NavigateToString(content);
    }

    private string GenerateHtmlContent()
    {
        return $$"""
                  
                              <html lang="{{LangProvider.Culture.TwoLetterISOLanguageName}}">
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
                                                  height: $(window).innerHeight() + 'px',
                                                  border: 'none'
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
                                  <body style="background-color: {{_backgroundColor}}">
                                      <iframe id='video' allow='autoplay; fullscreen; clipboard-write; encrypted-media; picture-in-picture' allowfullscreen
                                          src='{{HtmlText}}?hl={{LangProvider.Culture.TwoLetterISOLanguageName}}'
                                          style="visibility:hidden;" onload="this.style.visibility='visible';">
                                      </iframe>
                                  </body>
                              </html>
                  """;
    }
    
    private static void OnHtmlTextPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        var control = source as HtmlBox;
        if (control!.CheckAccess())
        {
            control.ProcessBrowse();
        }
        else
        {
            control.Dispatcher.InvokeAsync(control.ProcessBrowse);
        }
    }
}