using System.Windows.Controls;
using System.Windows.Input;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for AboutBox
    /// </summary>
    public partial class AboutBox : UserControl
    {
        //DialogWindow w;
        public AboutBox()
        {
            InitializeComponent();
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
          Cursor=Cursors.Hand;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            Cursor=Cursors.Arrow;
        }

        //private void W_Deactivated(object sender, System.EventArgs e)
        //{
        //   var o=(AboutBoxViewModel) DataContext;
        //    o.RaiseRequestClose(null);
        //    //w.Close();
        //}

        //private void TextBox_Loaded(object sender, RoutedEventArgs e)
        //{
        //    w = (DialogWindow)Parent;
        //     w.Deactivated += W_Deactivated;

        //}
    }
}