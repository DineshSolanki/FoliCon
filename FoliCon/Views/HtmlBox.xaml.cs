namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for HtmlBox.xaml
    /// </summary>
    public partial class HtmlBox : System.Windows.Controls.UserControl
    {
        public HtmlBox()
        {
            InitializeComponent();
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
            if (browser is not { IsLoaded: true }) return;
            if (string.IsNullOrEmpty(HtmlText) || HtmlText.Contains("Video not available!"))
            {
                const string unavailableVideoContent = @"
    <html>
        <body style='margin:0; padding:0; height: 100%; width: 100%; overflow: hidden;'>
            <img src='https://moviemaker.minitool.com/images/uploads/articles/2020/08/youtube-video-not-available/youtube-video-not-available-1.png' style='height: 100%; width: 100%; object-fit: fill;' />
        </body>
    </html>";
                browser.NavigateToString(unavailableVideoContent);
                return;
            }
            //var html = "<!DOCTYPE html><html><head><meta name='viewport' content='width=device-width, initial-scale=1'><meta content='IE=Edge' http-equiv='X-UA-Compatible'><style>.container {position: relative;overflow: hidden;padding-top: 56.25%; /* 16:9 Aspect Ratio */}.responsive-iframe {position: absolute;top: 0;left: 0;bottom: 0;right: 0;width: 100%;height: 100%;border: none;}</style></head><body><div class='container'> <iframe class='responsive-iframe' src='" + HtmlText + "' frameborder='0' allow =\"autoplay; fullscreen; clipboard - write; encrypted - media; picture-in-picture\" allowFullScreen></iframe></div></body></html>";
            var html = $@"
            <html>
            <head>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<meta content='IE=Edge' http-equiv='X-UA-Compatible'>
<style>html{{overflow:hidden;}}</style>
<script src=""http://code.jquery.com/jquery-latest.min.js""
        type=""text/javascript""></script>
<script>$(function() {{
  $('#video').css({{
    width: $(window).innerWidth() + 'px',
    height: $(window).innerHeight() + 'px'
  }});

  $(window).resize(function() {{
    $('#video').css({{
      width: $(window).innerWidth() + 'px',
      height: $(window).innerHeight() + 'px'
    }});
  }});
}});</script>
                </ head>
                <body>
                    <iframe id='video' allow =autoplay; fullscreen; clipboard - write; encrypted - media; picture -in-picture' allowfullscreen='true'
                        src ='{HtmlText}\' frameborder='0'>
                    </iframe>
                </body>
            </html>";


            browser.NavigateToString(html);
        }
    }
}
