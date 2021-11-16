using System.Windows;
using System.Windows.Controls;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for HtmlBox.xaml
    /// </summary>
    public partial class HtmlBox : UserControl
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
                string html = "<html><head>";
                html += "<meta content='IE=Edge' http-equiv='X-UA-Compatible'/>";

                html += $"<iframe id='video' src= '{HtmlText}?rel=0&autoplay=1' frameborder='0' allow =\"autoplay; fullscreen; clipboard - write; encrypted - media; picture -in-picture\" allowFullScreen></iframe>";

                html += "</body></html>";
                browser.NavigateToString(html);
                //browser.Navigate(HtmlText);
            }
        }
    }
}
