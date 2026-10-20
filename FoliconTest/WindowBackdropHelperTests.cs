using System;
using System.Windows.Interop;
using FoliCon.Modules.UI;
using HandyControl.Tools.Interop;
using Xunit;

namespace FoliconTest;

public class WindowBackdropHelperTests
{
    private const int GWL_STYLE = -16;
    private const int WS_SYSMENU = 0x00080000;

    [Fact]
    public void SuppressNativeCaptionButtons_NullWindowOrZeroHandle_DoesNotThrow()
    {
        WindowBackdropHelper.SuppressNativeCaptionButtons((System.Windows.Window)null!);
        WindowBackdropHelper.SuppressNativeCaptionButtons(IntPtr.Zero);
    }

    [Fact]
    public void SuppressNativeCaptionButtons_StripsSysMenuFromHandle()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var window = new System.Windows.Window();
            var handle = new WindowInteropHelper(window).EnsureHandle();

            // Ensure window initially has WS_SYSMENU
            var initialStyle = InteropMethods.GetWindowLong(handle, GWL_STYLE);
            InteropMethods.SetWindowLong(handle, GWL_STYLE, initialStyle | WS_SYSMENU);

            var styleWithSysMenu = InteropMethods.GetWindowLong(handle, GWL_STYLE);
            Assert.True((styleWithSysMenu & WS_SYSMENU) != 0, "Window should have WS_SYSMENU before suppression");

            // Suppress native caption buttons
            WindowBackdropHelper.SuppressNativeCaptionButtons(handle);

            var styleAfter = InteropMethods.GetWindowLong(handle, GWL_STYLE);
            Assert.True((styleAfter & WS_SYSMENU) == 0, "Window should NOT have WS_SYSMENU after suppression");
        });
    }

    [Fact]
    public void HandyWindow_InitializesWithMicaAndStripsSysMenu()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var handy = new HandyWindow();
            Assert.Equal(HandyControl.Tools.BackdropType.Mica, handy.SystemBackdropType);

            var handle = new WindowInteropHelper(handy).EnsureHandle();
            WindowBackdropHelper.SuppressNativeCaptionButtons(handle);

            var style = InteropMethods.GetWindowLong(handle, GWL_STYLE);
            Assert.True((style & WS_SYSMENU) == 0, "HandyWindow should NOT have WS_SYSMENU to prevent duplicate caption buttons");
        });
    }

    [Fact]
    public void HandyWindow_PreservesTemplateWhenPrismDialogStyleIsAssigned()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var handy = new HandyWindow();
            var prismStyle = new System.Windows.Style(typeof(System.Windows.Window));
            prismStyle.Setters.Add(new System.Windows.Setter(System.Windows.FrameworkElement.HeightProperty, 720.0));
            prismStyle.Setters.Add(new System.Windows.Setter(System.Windows.FrameworkElement.WidthProperty, 1060.0));
            prismStyle.Setters.Add(new System.Windows.Setter(System.Windows.FrameworkElement.MinHeightProperty, 660.0));
            prismStyle.Setters.Add(new System.Windows.Setter(System.Windows.FrameworkElement.MinWidthProperty, 960.0));

            handy.Style = prismStyle;
            handy.ApplyTemplate();

            // Let's inspect the template and style
            var template = handy.Template;
            Assert.NotNull(template);
            var nonClient = handy.Template.FindName("PART_NonClientArea", handy);
            Assert.NotNull(nonClient);
            Assert.Equal(720.0, handy.Height);
            Assert.Equal(1060.0, handy.Width);
            Assert.Equal(660.0, handy.MinHeight);
            Assert.Equal(960.0, handy.MinWidth);
            Assert.Equal(HandyControl.Tools.BackdropType.Mica, handy.SystemBackdropType);
            Assert.Equal(42.0, handy.NonClientAreaHeight);
            Assert.True(handy.ShowTitle);
        });
    }

    [Fact]
    public void HandyWindow_ClearsChildControlBackground_ToAllowMicaToShineThrough()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var handy = new HandyWindow();
            var userControl = new System.Windows.Controls.UserControl
            {
                Background = System.Windows.Media.Brushes.Red
            };

            handy.Content = userControl;

            Assert.Equal(System.Windows.Media.Brushes.Transparent, userControl.Background);
        });
    }

    [Fact]
    public void HandyWindow_VerifiesBrushesMatchMainWindow()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            // Load theme dictionary
            var themeUri = new Uri("pack://application:,,,/FoliCon;component/XamlResources/ThemeNordicPine.xaml", UriKind.Absolute);
            var themeDict = new System.Windows.ResourceDictionary { Source = themeUri };
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(themeDict);

            var handy = new HandyWindow();
            handy.ApplyTemplate();

            var expectedBackdrop = (System.Windows.Media.SolidColorBrush)themeDict["WindowBackdropBrush"];
            var actualBg = handy.Background as System.Windows.Media.SolidColorBrush;

            Assert.NotNull(actualBg);
            Assert.Equal(expectedBackdrop.Color, actualBg.Color);
            Assert.Equal(HandyControl.Tools.BackdropType.Mica, handy.SystemBackdropType);
        });
    }

    [Fact]
    public void SetImmersiveDarkMode_WithNullOrZeroHandle_DoesNotThrow()
    {
        WindowBackdropHelper.SetImmersiveDarkMode(IntPtr.Zero, true);
        WindowBackdropHelper.SetImmersiveDarkMode(IntPtr.Zero, false);
    }

    [Fact]
    public void UpdateImmersiveDarkMode_WithValidWindow_SetsDarkModeWithoutThrowing()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var window = new System.Windows.Window();
            WindowBackdropHelper.UpdateImmersiveDarkMode(window, isDark: true);
            WindowBackdropHelper.UpdateImmersiveDarkMode(window, isDark: false);
        });
    }
}

