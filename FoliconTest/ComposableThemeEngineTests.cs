#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using FoliCon.Models.Enums;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using FoliCon.ViewModels;
using Xunit;

namespace FoliconTest;

[Collection(XamlLoadingCollection.name)]
public class ComposableThemeEngineTests
{
    [Fact]
    public void PolarArchetypes_CorporateModern_MeetsContract()
    {
        var parser = new HybridThemeParser();
        var storage = new ThemeStorageService();
        var corporate = storage.GetThemeById("corporate");

        Assert.NotNull(corporate);
        Assert.Equal("Corporate Modern", corporate!.Name);
        Assert.Equal("Dark", corporate.BaseTheme);
        Assert.False(corporate.IsBuiltIn);
        Assert.Equal("#0F172A", corporate.Colors.Background);
        Assert.Equal("#2563EB", corporate.Colors.Primary);
        Assert.NotNull(corporate.Effects);
        Assert.Equal(0.0, corporate.Effects!.GlowIntensity);
        Assert.NotNull(corporate.Shapes);
        Assert.Equal(8.0, corporate.Shapes!.PanelRadius);
        Assert.Equal(6.0, corporate.Shapes!.ButtonRadius);

        // Synthesize ResourceDictionary
        var dict = parser.SynthesizeResourceDictionary(corporate);
        Assert.NotNull(dict);

        // Verify Brushes & Zero Glow
        var glowEffect = dict["ThemeGlowEffect"] as DropShadowEffect;
        Assert.NotNull(glowEffect);
        Assert.Equal(0.0, glowEffect!.BlurRadius);
        Assert.Equal(0.0, glowEffect.Opacity);

        // Verify Wallpaper is Transparent
        var wallpaper = dict["ThemeWallpaperBrush"] as Brush;
        Assert.True(wallpaper is SolidColorBrush { Color.A: 0 });

        // Verify Branding Geometry is present
        var brandingGeom = dict["ThemeBrandingGeometry"] as Geometry;
        Assert.NotNull(brandingGeom);

        // Verify Shapes Corner Radii
        Assert.Equal(new CornerRadius(8.0), dict["PanelCornerRadius"]);
        Assert.Equal(new CornerRadius(6.0), dict["ButtonCornerRadius"]);
    }

    [Fact]
    public void PolarArchetypes_LoveLivingTheme_MeetsContract()
    {
        var parser = new HybridThemeParser();
        var storage = new ThemeStorageService();
        var love = storage.GetThemeById("love");

        Assert.NotNull(love);
        Assert.Equal("Love", love!.Name);
        Assert.Equal("Dark", love.BaseTheme);
        Assert.False(love.IsBuiltIn);
        Assert.Equal("#080610", love.Colors.Background);
        Assert.Equal("#FF4D7D", love.Colors.Primary);
        Assert.Equal("#FF8CB3", love.Colors.Accent);
        Assert.NotNull(love.Effects);
        Assert.Equal(0.85, love.Effects!.GlowIntensity);
        Assert.Equal(26.0, love.Effects!.BloomRadius);
        Assert.NotNull(love.Shapes);
        Assert.Equal(12.0, love.Shapes!.PanelRadius);

        // Synthesize ResourceDictionary
        var dict = parser.SynthesizeResourceDictionary(love);
        Assert.NotNull(dict);

        // Verify Radiant Glow Halos
        var glowEffect = dict["ThemeGlowEffect"] as DropShadowEffect;
        Assert.NotNull(glowEffect);
        Assert.True(glowEffect!.BlurRadius > 15.0);
        Assert.True(glowEffect.Opacity > 0.5);

        // Verify Branding Geometry (Heart glyph)
        var brandingGeom = dict["ThemeBrandingGeometry"] as Geometry;
        Assert.NotNull(brandingGeom);

        // Verify Shapes Corner Radii
        Assert.Equal(new CornerRadius(12.0), dict["PanelCornerRadius"]);
        Assert.Equal(new CornerRadius(10.0), dict["ButtonCornerRadius"]);
    }

    [Fact]
    public void ComposableLayering_UserCustomization_OverridesThemeTokens()
    {
        var parser = new HybridThemeParser();
        var baseManifest = new ThemeManifest
        {
            Id = "base-cyber",
            Name = "Base Cyber",
            Colors = new ThemeColorPalette
            {
                Background = "#0B0E17",
                CardSurface = "#111827",
                Primary = "#088395",
                Accent = "#0EA5E9",
                Border = "#1E293B",
                PrimaryText = "#F8FAFC"
            },
            Effects = new ThemeEffectsTokens
            {
                GlowIntensity = 1.0,
                BloomRadius = 20.0,
                PanelOpacity = 0.9
            }
        };

        var customization = new UserThemeCustomization
        {
            ThemeId = "base-cyber",
            CustomAccentColor = "#FF007F",
            GlowMultiplier = 0.5,
            PanelOpacity = 0.65,
            IsWallpaperEnabled = false
        };

        var dict = parser.SynthesizeResourceDictionary(baseManifest, customization);

        // 1. Primary Brush overridden with custom accent
        var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
        Assert.NotNull(primaryBrush);
        Assert.Equal(0xFF, primaryBrush!.Color.R);
        Assert.Equal(0x00, primaryBrush.Color.G);
        Assert.Equal(0x7F, primaryBrush.Color.B);

        // 2. Glow blur radius scaled down by 0.5
        var glow = dict["ThemeGlowEffect"] as DropShadowEffect;
        Assert.NotNull(glow);
        Assert.Equal(10.0, glow!.BlurRadius); // 20 * 0.5
        Assert.Equal(0.5, glow.Opacity);

        // 3. Wallpaper forced to transparent
        Assert.True(dict["ThemeWallpaperBrush"] is SolidColorBrush { Color.A: 0 });

        // 4. Panel opacity overridden
        var backdrop = dict["WindowBackdropBrush"] as SolidColorBrush;
        Assert.NotNull(backdrop);
        Assert.Equal((byte)Math.Round(0.65 * 255.0), backdrop!.Color.A);
    }

    [Fact]
    public void ThemeStorageService_UserCustomization_SavesAndRetrieves()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "FoliCon_Customization_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);

            var cust = new UserThemeCustomization
            {
                ThemeId = "tokyo-neon",
                CustomAccentColor = "#FF4500",
                GlowMultiplier = 0.75,
                IsWallpaperEnabled = true
            };

            storage.SaveUserCustomization(cust);

            // Re-instantiate service to verify disk persistence
            var storageReloaded = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            var loaded = storageReloaded.GetUserCustomization("tokyo-neon");

            Assert.NotNull(loaded);
            Assert.Equal("tokyo-neon", loaded!.ThemeId);
            Assert.Equal("#FF4500", loaded.CustomAccentColor);
            Assert.Equal(0.75, loaded.GlowMultiplier);

            // Test clearing customization
            storageReloaded.ClearUserCustomization("tokyo-neon");
            Assert.Null(storageReloaded.GetUserCustomization("tokyo-neon"));
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task ThemeStorageService_ExportThemeVariant_CreatesInstallablePackage()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "FoliCon_ExportVariant_Test_" + Guid.NewGuid().ToString("N"));
        var exportZip = Path.Combine(tempFolder, "tokyo-blood-moon.folicontheme");

        try
        {
            var parser = new HybridThemeParser();
            var storage = new ThemeStorageService(parser, tempFolder);

            var customization = new UserThemeCustomization
            {
                ThemeId = "tokyo-neon",
                CustomAccentColor = "#DC2626", // Blood red
                GlowMultiplier = 1.2
            };

            var exportedPath = await storage.ExportThemeVariantAsync(
                baseThemeId: "tokyo-neon",
                customization: customization,
                variantId: "tokyo-blood-moon",
                variantName: "Tokyo Blood Moon",
                destinationZipPath: exportZip);

            Assert.True(File.Exists(exportedPath));

            // Verify zip contains theme.json with variant properties
            using (var archive = ZipFile.OpenRead(exportedPath))
            {
                var entry = archive.GetEntry(ThemeConstants.themeJsonFileName);
                Assert.NotNull(entry);

                using var stream = entry!.Open();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var variantManifest = parser.Parse(json);
                Assert.Equal("tokyo-blood-moon", variantManifest.Id);
                Assert.Equal("Tokyo Blood Moon", variantManifest.Name);
                Assert.Equal("#DC2626", variantManifest.Colors.Primary);
                Assert.Equal("#DC2626", variantManifest.Colors.Accent);
                Assert.Contains("CustomVariant", variantManifest.Tags);
            }

            // Install exported variant into storage
            await using (var zipStream = File.OpenRead(exportedPath))
            {
                var installed = await storage.InstallThemeFromPackageAsync(zipStream);
                Assert.Equal("tokyo-blood-moon", installed.Id);
                Assert.True(storage.IsThemeInstalled("tokyo-blood-moon"));
            }
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void MainWindowViewModel_LiveSwitchBetweenCorporateAndLove_TransitionsCleanly()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            // 1. Switch to Corporate Modern
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Corporate);
            Assert.Equal("corporate", MainWindowViewModel.ActiveThemeId);
            Assert.True(MainWindowViewModel.IsCurrentThemeDark());

            if (Application.Current != null)
            {
                // Verify Corporate zero-glow and clean slate tokens
                var corporateGlow = Application.Current.Resources["ThemeGlowEffect"] as DropShadowEffect;
                Assert.NotNull(corporateGlow);
                Assert.Equal(0.0, corporateGlow!.BlurRadius);
                Assert.Equal(0.0, corporateGlow.Opacity);

                var corporateWallpaper = Application.Current.Resources["ThemeWallpaperBrush"] as Brush;
                Assert.True(corporateWallpaper is SolidColorBrush { Color.A: 0 });
            }

            // 2. Switch to Love Living Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Love);
            Assert.Equal("love", MainWindowViewModel.ActiveThemeId);

            if (Application.Current != null)
            {
                // Verify Love radiant halo and heart branding glyph
                var loveGlow = Application.Current.Resources["ThemeGlowEffect"] as DropShadowEffect;
                Assert.NotNull(loveGlow);
                Assert.True(loveGlow!.BlurRadius > 10.0);
                Assert.True(loveGlow.Opacity > 0.5);

                var loveBranding = Application.Current.Resources["ThemeBrandingGeometry"] as Geometry;
                Assert.NotNull(loveBranding);
            }

            // 3. Switch back to Dark - Verify clean reset with zero living theme leaks
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Dark);
            Assert.Equal("dark", MainWindowViewModel.ActiveThemeId);

            if (Application.Current != null)
            {
                var darkWallpaper = Application.Current.Resources["ThemeWallpaperBrush"] as Brush;
                Assert.True(darkWallpaper is SolidColorBrush { Color.A: 0 });

                var darkGlow = Application.Current.Resources["ThemeGlowEffect"] as DropShadowEffect;
                Assert.NotNull(darkGlow);
                Assert.Equal(0.0, darkGlow!.BlurRadius);
            }
        });
    }
}
