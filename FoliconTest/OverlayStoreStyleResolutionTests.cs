#nullable enable
using System.Windows;

namespace FoliconTest;

/// <summary>
/// Guards the HandyControl style keys the store's tag chips depend on.
///
/// A missing <c>StaticResource</c> key compiles fine and only throws when the view is
/// constructed at runtime, so these keys need an explicit test rather than trusting the build.
/// </summary>
public class OverlayStoreStyleResolutionTests : IDisposable
{
    private readonly WpfTestHost _host = new();

    public void Dispose()
    {
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("TagBaseStyle")]
    public void TagStyles_ResolveWithATemplate(string styleKey)
    {
        // The chips rely on a real Tag style. A style without a template renders as bare
        // content — exactly the failure ToggleButtonCustom produced.
        var style = WpfTestHost.Invoke(() =>
        {
            EnsureThemeLoaded();
            return Application.Current!.TryFindResource(styleKey) as Style;
        });

        Assert.NotNull(style);
        Assert.Equal(typeof(HandyControl.Controls.Tag), style.TargetType);

        var hasTemplate = style.Setters.OfType<Setter>()
            .Any(s => s.Property == System.Windows.Controls.Control.TemplateProperty);
        var inheritsTemplate = style.BasedOn?.Setters.OfType<Setter>()
            .Any(s => s.Property == System.Windows.Controls.Control.TemplateProperty) ?? false;

        Assert.True(hasTemplate || inheritsTemplate, $"'{styleKey}' provides no control template.");
    }

    [Fact]
    public void TagChip_PropagatesSelectionToItsBoundSource()
    {
        // hc:Tag.IsSelected is not BindsTwoWayByDefault, so the chip's Mode=TwoWay binding
        // is load-bearing: without it, clicking a chip would change nothing in the ViewModel.
        var result = WpfTestHost.Invoke(() =>
        {
            EnsureThemeLoaded();

            var source = new FoliCon.ViewModels.OverlayTagFilterViewModel("dvd", 2);
            var tag = new HandyControl.Controls.Tag { Selectable = true, DataContext = source };

            tag.SetBinding(HandyControl.Controls.Tag.IsSelectedProperty,
                new System.Windows.Data.Binding(nameof(source.IsSelected))
                {
                    Mode = System.Windows.Data.BindingMode.TwoWay
                });

            // Simulate what a click does to the control.
            tag.IsSelected = true;
            var afterSelect = source.IsSelected;

            tag.IsSelected = false;
            return (afterSelect, afterDeselect: source.IsSelected);
        });

        Assert.True(result.afterSelect, "Selecting the chip did not reach the ViewModel.");
        Assert.False(result.afterDeselect, "Deselecting the chip did not reach the ViewModel.");
    }

    [Theory]
    [InlineData("ButtonDefault", typeof(System.Windows.Controls.Button))]
    [InlineData("ButtonPrimary", typeof(System.Windows.Controls.Button))]
    public void StoreStyleKeys_ResolveFromHandyControlTheme(string styleKey, Type expectedTargetType)
    {
        var style = WpfTestHost.Invoke(() =>
        {
            EnsureThemeLoaded();
            return Application.Current!.TryFindResource(styleKey) as Style;
        });

        Assert.NotNull(style);
        Assert.Equal(expectedTargetType, style.TargetType);
    }

    /// <summary>
    /// Merges HandyControl's theme into the test application once, mirroring what
    /// <c>App.xaml</c> does in the real process.
    /// </summary>
    private static void EnsureThemeLoaded()
    {
        var app = Application.Current
            ?? throw new InvalidOperationException("No WPF Application; WpfTestHost should have created one.");

        const string themeUri = "pack://application:,,,/HandyControl;component/Themes/Theme.xaml";

        if (app.Resources.MergedDictionaries.Any(d => d.Source?.ToString() == themeUri))
        {
            return;
        }

        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(themeUri, UriKind.Absolute)
        });
    }
}
