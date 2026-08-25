using System.Windows;
using System.Windows.Controls;
using HandyTextBox = HandyControl.Controls.TextBox;

namespace FoliconTest;

/// <summary>
/// Regression tests for the WPF clipboard path used by editable text boxes.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class TextBoxClipboardTests
{
    [Fact]
    public void TextBox_CopyCutAndPaste_RoundTripsText()
    {
        using var host = new WpfTestHost();

        WpfTestHost.Invoke(() => AssertClipboardRoundTrip(() => new TextBox()));
    }

    [Fact]
    public void HandyControlTextBox_CopyCutAndPaste_RoundTripsText()
    {
        using var host = new WpfTestHost();

        WpfTestHost.Invoke(() => AssertClipboardRoundTrip(() => new HandyTextBox()));
    }

    private static void AssertClipboardRoundTrip(Func<TextBox> createTextBox)
    {
        const string originalText = "clipboard regression";
        const string replacementText = "pasted text";

        var source = createTextBox();
        var destination = createTextBox();

        try
        {
            source.Text = originalText;
            source.SelectAll();
            source.Copy();

            destination.Text = replacementText;
            destination.SelectAll();
            destination.Paste();

            Assert.Equal(originalText, destination.Text);

            destination.SelectAll();
            destination.Cut();

            Assert.Equal(string.Empty, destination.Text);
            Assert.Equal(originalText, Clipboard.GetText());
        }
        finally
        {
            Clipboard.Clear();
        }
    }
}
