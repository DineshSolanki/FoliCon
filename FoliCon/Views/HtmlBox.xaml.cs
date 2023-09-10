using System.Windows.Navigation;
using HandyControl.Themes;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for HtmlBox.xaml
    /// </summary>
    public partial class HtmlBox : System.Windows.Controls.UserControl
    {
        private readonly string _imagePath = Util.GetResourcePath("video-unavailable.png");

        public HtmlBox()
        {
            InitializeComponent();
            Browser.Visibility = Visibility.Hidden;
            var theme = ThemeManager.Current.ApplicationTheme;
            // var htmlText = """
            //            <style>html, body {
            //                overflow: hidden;
            //                margin: 0;
            //                padding: 0;
            //                width: 100%;
            //                height: 100%;
            //                box-sizing: border-box;
            //                background-color: #FFFFFF;
            //            }</style>
            //            """;
            //
            // if (theme == ApplicationTheme.Dark)
            // {
            //     htmlText = """
            //                <style>html, body {
            //                    overflow: hidden;
            //                    margin: 0;
            //                    padding: 0;
            //                    width: 100%;
            //                    height: 100%;
            //                    box-sizing: border-box;
            //                    background-color: #202020;
            //                }</style>
            //                """;
            // }
            //
            // browser.NavigateToString(htmlText);
        }

        public static readonly DependencyProperty HtmlTextProperty = DependencyProperty.Register("HtmlText", typeof(string), typeof(HtmlBox));

        public string HtmlText
        {
            get => (string)GetValue(HtmlTextProperty);
            set => SetValue(HtmlTextProperty, value);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == HtmlTextProperty)
            {
                DoBrowse();
            }
        }
        private void DoBrowse()
        {
            if (CheckAccess())
            {
                BrowseInternal();
            }
            else
            {
                Dispatcher.Invoke(BrowseInternal);
            }
           
        }

        private void BrowseInternal()
        {
            if (Browser is not { IsLoaded: true }) return;
            if (string.IsNullOrEmpty(HtmlText) || HtmlText.Contains("Video not available!"))
            {
                var unavailableVideoContent = $"""
                                               
                                                   <html>
                                                       <body style='margin:0; padding:0; height: 100%; width: 100%; overflow: hidden;'>
                                                           <img src='{_imagePath}' style='height: 100%; width: 100%; object-fit: fill;' />
                                                       </body>
                                                   </html>
                                               """;
                Browser.NavigateToString(unavailableVideoContent);
                return;
            }
            //var html = "<!DOCTYPE html><html><head><meta name='viewport' content='width=device-width, initial-scale=1'><meta content='IE=Edge' http-equiv='X-UA-Compatible'><style>.container {position: relative;overflow: hidden;padding-top: 56.25%; /* 16:9 Aspect Ratio */}.responsive-iframe {position: absolute;top: 0;left: 0;bottom: 0;right: 0;width: 100%;height: 100%;border: none;}</style></head><body><div class='container'> <iframe class='responsive-iframe' src='" + HtmlText + "' frameborder='0' allow =\"autoplay; fullscreen; clipboard - write; encrypted - media; picture-in-picture\" allowFullScreen></iframe></div></body></html>";
            var html = $$"""
                         
                                     <html>
                                     <head>
                         <meta name='viewport' content='width=device-width, initial-scale=1'>
                         <meta content='IE=Edge' http-equiv='X-UA-Compatible'>
                         <style>html, body {
                             overflow: hidden;
                             margin: 0;
                             padding: 0;
                             width: 100%;
                             height: 100%;
                             box-sizing: border-box;
                         }</style>
                         <script src="https://code.jquery.com/jquery-latest.min.js"
                                 type="text/javascript"></script>
                         <script>$(function() {
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
                         });</script>
                                         </ head>
                                         <body>
                                             <iframe id='video' allow =autoplay; fullscreen; clipboard - write; encrypted - media; picture -in-picture' allowfullscreen='true'
                                                 src ='{{HtmlText}}\' frameborder='0'>
                                             </iframe>
                                         </body>
                                     </html>
                         """;


            Browser.NavigateToString(html);
        }

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            Browser.Visibility = Visibility.Visible;
        }
    }
}
