using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;

namespace FoliconTest;

/// <summary>
/// Unit tests for <see cref="OverlayProvider"/>.
/// Tests built-in overlay loading and overlay resolution logic.
/// </summary>
public class OverlayProviderTests
{
    [Fact]
    public void GetAllOverlays_ReturnsBuiltInOverlays()
    {
        var provider = new OverlayProvider();

        var all = provider.GetAllOverlays();

        Assert.NotEmpty(all);
        Assert.True(all.Count >= 6, $"Expected at least 6 built-in overlays, got {all.Count}");
    }

    [Fact]
    public void GetAllOverlays_AllBuiltInOverlaysHaveIsBuiltInTrue()
    {
        var provider = new OverlayProvider();

        // Only verify the 6 known built-in overlays have IsBuiltIn=true
        // (user-installed overlays from AppData may also be present)
        var builtInIds = new[] { "legacy", "alternate", "liaher", "faelpessoal", "faelpessoal-horizontal", "windows11" };
        foreach (var id in builtInIds)
        {
            var overlay = provider.GetOverlayById(id);
            Assert.NotNull(overlay);
            Assert.True(overlay.IsBuiltIn, $"Built-in overlay '{id}' should have IsBuiltIn=true");
        }
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("alternate")]
    [InlineData("liaher")]
    [InlineData("faelpessoal")]
    [InlineData("faelpessoal-horizontal")]
    [InlineData("windows11")]
    public void GetOverlayById_KnownId_ReturnsOverlay(string id)
    {
        var provider = new OverlayProvider();

        var overlay = provider.GetOverlayById(id);

        Assert.NotNull(overlay);
        Assert.Equal(id, overlay.Id);
    }

    [Fact]
    public void GetOverlayById_UnknownId_ReturnsNull()
    {
        var provider = new OverlayProvider();

        var overlay = provider.GetOverlayById("nonexistent-overlay");

        Assert.Null(overlay);
    }

    [Fact]
    public void GetOverlayById_CaseInsensitive()
    {
        var provider = new OverlayProvider();

        var overlay = provider.GetOverlayById("LEGACY");

        Assert.NotNull(overlay);
        Assert.Equal("legacy", overlay.Id);
    }

    [Fact]
    public void ResolveActiveOverlayOrDefault_NullId_ReturnsDefault()
    {
        var provider = new OverlayProvider();

        var overlay = provider.ResolveActiveOverlayOrDefault(null);

        Assert.NotNull(overlay);
        Assert.Equal("liaher", overlay.Id);
    }

    [Fact]
    public void ResolveActiveOverlayOrDefault_EmptyId_ReturnsDefault()
    {
        var provider = new OverlayProvider();

        var overlay = provider.ResolveActiveOverlayOrDefault("");

        Assert.NotNull(overlay);
        Assert.Equal("liaher", overlay.Id);
    }

    [Fact]
    public void ResolveActiveOverlayOrDefault_UnknownId_ReturnsDefault()
    {
        var provider = new OverlayProvider();

        var overlay = provider.ResolveActiveOverlayOrDefault("nonexistent-overlay");

        Assert.NotNull(overlay);
        Assert.Equal("liaher", overlay.Id);
    }

    [Fact]
    public void ResolveActiveOverlayOrDefault_KnownId_ReturnsThatOverlay()
    {
        var provider = new OverlayProvider();

        var overlay = provider.ResolveActiveOverlayOrDefault("windows11");

        Assert.NotNull(overlay);
        Assert.Equal("windows11", overlay.Id);
    }

    [Fact]
    public void GetOverlayFolderPath_BuiltIn_ReturnsAbsolutePath()
    {
        var provider = new OverlayProvider();

        var path = provider.GetOverlayFolderPath("liaher");

        Assert.True(Path.IsPathRooted(path));
        Assert.Equal(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources", "Overlays", "liaher")), path);
    }

    [Fact]
    public void GetOverlayFolderPath_UserOverlay_ReturnsAbsolutePath()
    {
        var provider = new OverlayProvider();

        var path = provider.GetOverlayFolderPath("custom-overlay");

        Assert.True(Path.IsPathRooted(path));
        Assert.EndsWith(Path.Combine("FoliCon", "Overlays", "custom-overlay"), path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsOverlayInstalled_KnownId_ReturnsTrue()
    {
        var provider = new OverlayProvider();

        Assert.True(provider.IsOverlayInstalled("liaher"));
    }

    [Fact]
    public void IsOverlayInstalled_UnknownId_ReturnsFalse()
    {
        var provider = new OverlayProvider();

        Assert.False(provider.IsOverlayInstalled("nonexistent"));
    }

    [Fact]
    public void BuiltInOverlayDefinitions_HaveRequiredFields()
    {
        var provider = new OverlayProvider();

        var all = provider.GetAllOverlays();

        foreach (var overlay in all)
        {
            Assert.False(string.IsNullOrWhiteSpace(overlay.Id), "Overlay ID should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(overlay.DisplayName), $"Overlay '{overlay.Id}' displayName should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(overlay.OverlayVersion), $"Overlay '{overlay.Id}' overlayVersion should not be empty");
            Assert.NotNull(overlay.Poster);
            Assert.NotNull(overlay.Rating);
            Assert.NotNull(overlay.Title);
            Assert.True(overlay.DesignWidth > 0, $"Overlay '{overlay.Id}' designWidth should be positive");
            Assert.True(overlay.DesignHeight > 0, $"Overlay '{overlay.Id}' designHeight should be positive");
        }
    }

    [Fact]
    public void BuiltInOverlayDefinitions_HaveValidLayerOrder()
    {
        var validKeys = new HashSet<string> { "base", "poster", "front", "rating", "title" };
        var provider = new OverlayProvider();

        var all = provider.GetAllOverlays();

        foreach (var overlay in all)
        {
            if (overlay.LayerOrder != null)
            {
                foreach (var key in overlay.LayerOrder)
                {
                    Assert.Contains(key, validKeys);
                }
            }
        }
    }

    [Fact]
    public void Liaher_HasClipRadius()
    {
        var provider = new OverlayProvider();

        var liaher = provider.GetOverlayById("liaher");

        Assert.NotNull(liaher);
        Assert.NotEqual("0", liaher.Poster.ClipRadius);
    }

    [Fact]
    public void Windows11_HasOpacityMask()
    {
        var provider = new OverlayProvider();

        var win11 = provider.GetOverlayById("windows11");

        Assert.NotNull(win11);
        Assert.False(string.IsNullOrEmpty(win11.Poster.OpacityMaskPath));
    }

    [Fact]
    public void Faelpessoal_HasVisibleTitle()
    {
        var provider = new OverlayProvider();

        var faelpessoal = provider.GetOverlayById("faelpessoal");

        Assert.NotNull(faelpessoal);
        Assert.True(faelpessoal.Title.IsVisible);
        Assert.True(faelpessoal.Title.RotationAngle > 0);
    }

    [Fact]
    public void Legacy_HasNoBaseLayer()
    {
        var provider = new OverlayProvider();

        var legacy = provider.GetOverlayById("legacy");

        Assert.NotNull(legacy);
        Assert.Null(legacy.BaseLayer);
    }
}
