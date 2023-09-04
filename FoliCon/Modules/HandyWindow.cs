namespace FoliCon.Modules
{
    public class HandyWindow : HandyControl.Controls.Window, IDialogWindow
    {
        // static HandyWindow()
        // {
        //     DefaultStyleKeyProperty.OverrideMetadata(typeof(HandyWindow), new FrameworkPropertyMetadata(typeof(HandyControl.Controls.Window)));
        //
        // }
        public HandyWindow()
        {
            Background = (System.Windows.Media.Brush)FindResource("RegionBrush");
        }
        public IDialogResult Result { get; set ; }
    }
}
