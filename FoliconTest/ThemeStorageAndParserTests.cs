#nullable enable
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Windows;
using System.Windows.Media;
using FoliCon.Models.Enums;
using FoliCon.Modules.Themes;
using FoliCon.ViewModels;

namespace FoliconTest;

[Collection(XamlLoadingCollection.name)]
public class ThemeStorageAndParserTests
{
    private const string sampleCyberpunkJson = """
    {
      "id": "cyberpunk-2077",
      "name": "Cyberpunk 2077",
      "description": "High-voltage neon yellow and cyan.",
      "author": "JohnnySilver",
      "version": "1.0.0",
      "baseTheme": "Dark",
      "tags": ["Cyberpunk", "Neon"],
      "colors": {
        "background": "#0D0D0D",
        "cardSurface": "#141414",
        "elevatedSurface": "#1F1F1F",
        "primary": "#FCEE09",
        "primaryHover": "#FFF233",
        "primaryPressed": "#D4C700",
        "accent": "#00F0FF",
        "border": "#2E2E2E",
        "primaryText": "#00F0FF",
        "secondaryText": "#FCEE09"
      },
      "materials": {
        "backdropOpacity": 0.88,
        "backdropColor": "#0D0D0D",
        "primaryActionGradient": {
          "startPoint": [0.0, 0.0],
          "endPoint": [1.0, 1.0],
          "stops": [
            { "offset": 0.0, "color": "#FCEE09" },
            { "offset": 1.0, "color": "#FF003C" }
          ]
        }
      }
    }
    """;

    #region HybridThemeParser Tests

    [Fact]
    public void Validate_ValidJson_ReturnsSuccess()
    {
        var parser = new HybridThemeParser();
        var result = parser.Validate(sampleCyberpunkJson);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullJson_ReturnsError(string? json)
    {
        var parser = new HybridThemeParser();
        var result = parser.Validate(json!);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InvalidJsonSyntax_ReturnsError()
    {
        var parser = new HybridThemeParser();
        var result = parser.Validate("{ id: 'broken' ");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid JSON", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Cyberpunk_Theme")] // underscore not allowed
    [InlineData("Cyberpunk 2077")] // spaces not allowed
    [InlineData("CYBERPUNK")] // uppercase not allowed
    [InlineData("-start-hyphen")] // leading hyphen
    public void Validate_InvalidThemeId_ReturnsError(string invalidId)
    {
        var parser = new HybridThemeParser();
        var json = sampleCyberpunkJson.Replace("\"cyberpunk-2077\"", $"\"{invalidId}\"");
        var result = parser.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Theme ID", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingRequiredColor_ReturnsError()
    {
        var parser = new HybridThemeParser();
        var json = """
        {
          "id": "test-theme",
          "name": "Test Theme",
          "colors": {
            "background": "#141414"
          }
        }
        """;
        var result = parser.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("colors.cardSurface", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("colors.primary", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("not-a-color")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void Validate_InvalidHexFormat_ReturnsError(string badHex)
    {
        var parser = new HybridThemeParser();
        var json = sampleCyberpunkJson.Replace("#FCEE09", badHex);
        var result = parser.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid hex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_PathTraversalInXamlPackageFile_ReturnsError()
    {
        var parser = new HybridThemeParser();
        var json = sampleCyberpunkJson.Replace("\"tags\": [\"Cyberpunk\", \"Neon\"],", "\"xamlPackageFile\": \"../../Theme.xaml\",");
        var result = parser.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("path traversal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_ValidJson_ReturnsPopulatedManifest()
    {
        var parser = new HybridThemeParser();
        var manifest = parser.Parse(sampleCyberpunkJson);

        Assert.Equal("cyberpunk-2077", manifest.Id);
        Assert.Equal("Cyberpunk 2077", manifest.Name);
        Assert.Equal("JohnnySilver", manifest.Author);
        Assert.Equal("#0D0D0D", manifest.Colors.Background);
        Assert.Equal("#FCEE09", manifest.Colors.Primary);
        Assert.Equal("#00F0FF", manifest.Colors.Accent);
        Assert.True(manifest.IsDark);
        Assert.NotNull(manifest.Materials);
        Assert.Equal(0.88, manifest.Materials!.BackdropOpacity);
    }

    [Fact]
    public void SynthesizeResourceDictionary_ProducesRequiredBrushesAndColorsAndAreFrozen()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var parser = new HybridThemeParser();
            var manifest = parser.Parse(sampleCyberpunkJson);
            var dict = parser.SynthesizeResourceDictionary(manifest);

            Assert.NotNull(dict);

            // Verify Key Colors
            Assert.True(dict.Contains("BackgroundColor"));
            Assert.True(dict.Contains("PrimaryColor"));
            Assert.True(dict.Contains("RegionColor"));
            Assert.True(dict.Contains("BorderColor"));

            // Verify Key Brushes
            var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.True(primaryBrush.IsFrozen);
            Assert.Equal(Color.FromRgb(0xFC, 0xEE, 0x09), primaryBrush.Color);

            var regionBrush = dict["RegionBrush"] as SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.True(regionBrush.IsFrozen);
            Assert.Equal(Color.FromRgb(0x0D, 0x0D, 0x0D), regionBrush.Color);

            // Verify WindowBackdropBrush has ~88% opacity (224 alpha)
            var backdropBrush = dict["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(backdropBrush);
            Assert.True(backdropBrush.IsFrozen);
            Assert.Equal(224, backdropBrush.Color.A);

            // Verify Custom Material Action Gradient
            var actionGradient = dict["PrimaryActionGradientBrush"] as LinearGradientBrush;
            Assert.NotNull(actionGradient);
            Assert.True(actionGradient.IsFrozen);
            Assert.Equal(2, actionGradient.GradientStops.Count);
            Assert.Equal(Color.FromRgb(0xFC, 0xEE, 0x09), actionGradient.GradientStops[0].Color);
            Assert.Equal(Color.FromRgb(0xFF, 0x00, 0x3C), actionGradient.GradientStops[1].Color);
        });
    }

    [Fact]
    public void LoadPackageResourceDictionary_ValidXaml_LoadsAndFreezesBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var tempXaml = Path.Combine(Path.GetTempPath(), $"TestTheme_{Guid.NewGuid():N}.xaml");
            try
            {
                var xaml = """
                <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <SolidColorBrush x:Key="TestBrush" Color="#FF336699" />
                </ResourceDictionary>
                """;
                File.WriteAllText(tempXaml, xaml);

                var parser = new HybridThemeParser();
                var dict = parser.LoadPackageResourceDictionary(tempXaml);

                Assert.NotNull(dict);
                var brush = dict["TestBrush"] as SolidColorBrush;
                Assert.NotNull(brush);
                Assert.True(brush.IsFrozen);
                Assert.Equal(Color.FromArgb(0xFF, 0x33, 0x66, 0x99), brush.Color);
            }
            finally
            {
                if (File.Exists(tempXaml)) File.Delete(tempXaml);
            }
        });
    }

    [Fact]
    public void LoadPackageResourceDictionary_MaliciousExecutableXaml_ThrowsSecurityException()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var tempXaml = Path.Combine(Path.GetTempPath(), $"MaliciousTheme_{Guid.NewGuid():N}.xaml");
            try
            {
                var xaml = """
                <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                    x:Class="MaliciousNamespace.Exploit">
                </ResourceDictionary>
                """;
                File.WriteAllText(tempXaml, xaml);

                var parser = new HybridThemeParser();
                Assert.Throws<SecurityException>(() => parser.LoadPackageResourceDictionary(tempXaml));
            }
            finally
            {
                if (File.Exists(tempXaml)) File.Delete(tempXaml);
            }
        });
    }

    [Fact]
    public void LoadPackageResourceDictionary_DtdEntityOrProhibitedElement_ThrowsException()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var tempXaml = Path.Combine(Path.GetTempPath(), $"DtdTheme_{Guid.NewGuid():N}.xaml");
            try
            {
                var xaml = """
                <!DOCTYPE r [ <!ENTITY evil "exploit"> ]>
                <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                </ResourceDictionary>
                """;
                File.WriteAllText(tempXaml, xaml);

                var parser = new HybridThemeParser();
                Assert.ThrowsAny<Exception>(() => parser.LoadPackageResourceDictionary(tempXaml));
            }
            finally
            {
                if (File.Exists(tempXaml)) File.Delete(tempXaml);
            }
        });
    }

    #endregion

    #region ThemeStorageService Tests

    [Fact]
    public void GetInstalledThemes_ReturnsBuiltInThemesByDefault()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"FoliCon_Themes_Test_{Guid.NewGuid():N}");
        try
        {
            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            var installed = storage.GetInstalledThemes();

            Assert.NotNull(installed);
            Assert.True(installed.Count >= 8);
            Assert.Contains(installed, t => t.Id == "dark" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "light" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "the-butterfly" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "tokyo-neon" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "mica-dark-obsidian" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "nordic-pine" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "cinema-35mm" && t.IsBuiltIn);
            Assert.Contains(installed, t => t.Id == "system" && t.IsBuiltIn);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task InstallThemeFromJsonAsync_InstallsThemeAndCanBeRetrievedAndDeleted()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"FoliCon_Themes_Test_{Guid.NewGuid():N}");
        try
        {
            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            var dummyPreview = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes

            var installed = await storage.InstallThemeFromJsonAsync(sampleCyberpunkJson, dummyPreview);

            Assert.NotNull(installed);
            Assert.Equal("cyberpunk-2077", installed.Id);
            Assert.False(installed.IsBuiltIn);
            Assert.True(storage.IsThemeInstalled("cyberpunk-2077"));

            var retrieved = storage.GetThemeById("cyberpunk-2077");
            Assert.NotNull(retrieved);
            Assert.Equal("Cyberpunk 2077", retrieved.Name);
            Assert.Equal(ThemeConstants.previewImageFileName, retrieved.PreviewImageFile);

            // User themes count
            var userThemes = storage.GetUserThemes();
            Assert.Single(userThemes);

            // Delete Theme
            var deleted = storage.DeleteTheme("cyberpunk-2077");
            Assert.True(deleted);
            Assert.False(storage.IsThemeInstalled("cyberpunk-2077"));
            Assert.Empty(storage.GetUserThemes());
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task InstallThemeFromJsonAsync_AttemptToOverwriteBuiltInTheme_ThrowsException()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"FoliCon_Themes_Test_{Guid.NewGuid():N}");
        try
        {
            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            var fakeDarkJson = sampleCyberpunkJson.Replace("\"cyberpunk-2077\"", "\"dark\"");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.InstallThemeFromJsonAsync(fakeDarkJson));
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void DeleteTheme_BuiltInTheme_ReturnsFalse()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"FoliCon_Themes_Test_{Guid.NewGuid():N}");
        try
        {
            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            var deleted = storage.DeleteTheme("the-butterfly");
            Assert.False(deleted);
            Assert.True(storage.IsThemeInstalled("the-butterfly"));
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task ExportAndInstallThemePackage_RoundTripsSuccessfully()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"FoliCon_Themes_Test_{Guid.NewGuid():N}");
        var zipExportPath = Path.Combine(Path.GetTempPath(), $"cyberpunk_{Guid.NewGuid():N}.folicontheme");

        try
        {
            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            await storage.InstallThemeFromJsonAsync(sampleCyberpunkJson);

            // Export to .folicontheme
            var exportedPath = await storage.ExportThemePackageAsync("cyberpunk-2077", zipExportPath);
            Assert.True(File.Exists(exportedPath));

            // Delete original
            storage.DeleteTheme("cyberpunk-2077");
            Assert.False(storage.IsThemeInstalled("cyberpunk-2077"));

            // Install from exported package
            await using (var zipStream = File.OpenRead(exportedPath))
            {
                var reinstalled = await storage.InstallThemeFromPackageAsync(zipStream);
                Assert.NotNull(reinstalled);
                Assert.Equal("cyberpunk-2077", reinstalled.Id);
                Assert.True(storage.IsThemeInstalled("cyberpunk-2077"));
            }
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
            if (File.Exists(zipExportPath)) File.Delete(zipExportPath);
        }
    }

    [Fact]
    public async Task InstallThemeFromPackageAsync_ZipSlipAttack_ThrowsSecurityException()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"FoliCon_Themes_Test_{Guid.NewGuid():N}");
        var maliciousZip = Path.Combine(Path.GetTempPath(), $"malicious_{Guid.NewGuid():N}.folicontheme");

        try
        {
            // Build a malicious zip entry trying to escape to ../evil.txt
            using (var zipStream = new FileStream(maliciousZip, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("../evil.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.WriteLine("malicious payload");
            }

            var storage = new ThemeStorageService(new HybridThemeParser(), tempFolder);
            await using (var zipStream = File.OpenRead(maliciousZip))
            {
                await Assert.ThrowsAsync<SecurityException>(() =>
                    storage.InstallThemeFromPackageAsync(zipStream));
            }
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
            if (File.Exists(maliciousZip)) File.Delete(maliciousZip);
        }
    }

    #endregion

    #region MainWindowViewModel Community Integration Tests

    [Fact]
    public void MainWindowViewModel_ApplyCommunityTheme_UpdatesThemeAndBackdropBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var parser = new HybridThemeParser();
            var manifest = parser.Parse(sampleCyberpunkJson);
            var dict = parser.SynthesizeResourceDictionary(manifest);

            MainWindowViewModel.ApplyCommunityTheme(manifest, dict);

            Assert.Equal("cyberpunk-2077", MainWindowViewModel.ActiveThemeId);
            Assert.NotNull(MainWindowViewModel.ActiveThemeManifest);
            Assert.Equal("Cyberpunk 2077", MainWindowViewModel.ActiveThemeManifest!.Name);

            // Check that WindowBackdropBrush in Application resources is synced
            if (Application.Current != null)
            {
                var backdrop = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
                Assert.NotNull(backdrop);
                Assert.Equal(224, backdrop.Color.A);
            }

            // Clean up back to Dark
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Dark);
            Assert.Equal("dark", MainWindowViewModel.ActiveThemeId);
        });
    }

    [Fact]
    public void MainWindowViewModel_ApplyThemeManifest_HandlesBothBuiltInAndCommunityThemes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var parser = new HybridThemeParser();

            // 1. Built-in manifest
            var butterflyManifest = BuiltInThemes.GetAll().First(t => t.Id == "the-butterfly");
            MainWindowViewModel.ApplyThemeManifest(butterflyManifest, parser);

            Assert.Equal("the-butterfly", MainWindowViewModel.ActiveThemeId);
            Assert.Equal(FoliconThemes.TheButterfly, MainWindowViewModel.CurrentTheme);

            // 2. Dynamic community manifest
            var communityManifest = parser.Parse(sampleCyberpunkJson);
            MainWindowViewModel.ApplyThemeManifest(communityManifest, parser);

            Assert.Equal("cyberpunk-2077", MainWindowViewModel.ActiveThemeId);

            // 3. Reset back to Dark
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Dark);
            Assert.Equal("dark", MainWindowViewModel.ActiveThemeId);
        });
    }

    #endregion
}
