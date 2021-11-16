using HandyControl.Tools.Extension;

using RestEase;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

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
            get { return (string)GetValue(HtmlTextProperty); }
            set { SetValue(HtmlTextProperty, value); }
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
            if (!string.IsNullOrEmpty(HtmlText))
            {
                var html = "<!DOCTYPE html><html><head><meta name='viewport' content='width=device-width, initial-scale=1'><meta content='IE=Edge' http-equiv='X-UA-Compatible'><style>.container {position: relative;overflow: hidden;padding-top: 56.25%; /* 16:9 Aspect Ratio */}.responsive-iframe {position: absolute;top: 0;left: 0;bottom: 0;right: 0;width: 100%;height: 100%;border: none;}</style></head><body><div class='container'> <iframe class='responsive-iframe' src='" + HtmlText + "' frameborder='0' allow =\"autoplay; fullscreen; clipboard - write; encrypted - media; picture-in-picture\" allowFullScreen></iframe></div></body></html>";
                //string html = "<html><head>";
                //html += "<meta content='IE=Edge' http-equiv='X-UA-Compatible'>" +
                //    "<meta name='viewport' content='width = device - width, initial - scale = 1'>" +
                //    "<style>.container {position: relative;width: 100 %;overflow: hidden;padding - top: 56.25 %;}" +
                //    ".responsive - iframe {position: absolute;top: 0;left: 0;bottom: 0;right: 0;width: 100 %;height: 100 %;border: none;}</style>" +
                //    "</head>" +
                //    "<body>" +
                //    "<div class='container'>";

                //html += $"<iframe class='responsive - iframe' id='video' src='{HtmlText}?rel=0' frameborder='0' allow =\"autoplay; fullscreen; clipboard - write; encrypted - media; picture-in-picture\" allowFullScreen></iframe>";

                //html += "</div></body></html>";
                browser.NavigateToString(html);
                //browser.Navigate(HtmlText);
            }
        }
    }
}
