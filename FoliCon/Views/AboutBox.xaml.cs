namespace FoliCon.Views;

/// <summary>
/// Interaction logic for AboutBox
/// </summary>
public partial class AboutBox
{
    public AboutBox()
    {
        InitializeComponent();
    }

    private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
    {
        Cursor = Cursors.Hand;
    }

    private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
    {
        Cursor = Cursors.Arrow;
    }
}