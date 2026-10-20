#nullable enable
using System.Collections.Generic;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using Xunit;

namespace FoliconTest;

public class ThemeQualityAuditorTests
{
    [Fact]
    public void Audit_GoodCommunityTheme_PassesAllChecks()
    {
        var manifest = new ThemeManifest
        {
            Id = "synthwave-sunset",
            Name = "Synthwave Sunset",
            Description = "Retro-futuristic purple and orange palette",
            Author = "RetroCoder",
            Version = "1.0.0",
            BaseTheme = "Dark",
            Colors = new ThemeColorPalette
            {
                Background = "#1A102F",
                CardSurface = "#261A42",
                Primary = "#FF7A00",
                PrimaryText = "#FFFFFF",
                Border = "#3D2E60",
                SecondaryText = "#FF7A00"
            }
        };

        var report = ThemeQualityAuditor.Audit(manifest);

        Assert.True(report.IsQualified);
        Assert.Empty(report.Errors);
        Assert.True(report.ContrastPassed);
        Assert.True(report.PrimaryTextContrastRatio >= 4.5);
    }

    [Fact]
    public void Audit_LowContrastTheme_FailsAccessibilityCheck()
    {
        var unreadableTheme = new ThemeManifest
        {
            Id = "dark-grey-on-black",
            Name = "Low Contrast Dark",
            Description = "Impossible to read",
            Author = "Tester",
            Version = "1.0.0",
            BaseTheme = "Dark",
            Colors = new ThemeColorPalette
            {
                Background = "#111111",
                CardSurface = "#222222",
                Primary = "#333333",
                PrimaryText = "#333333", // Dark gray text on black background!
                Border = "#222222"
            }
        };

        var report = ThemeQualityAuditor.Audit(unreadableTheme);

        Assert.False(report.IsQualified);
        Assert.False(report.ContrastPassed);
        Assert.Contains(report.Errors, e => e.Contains("contrast", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Audit_ReservedBuiltInThemeId_IsRejected()
    {
        var manifest = new ThemeManifest
        {
            Id = "tokyo-neon", // Built-in reserved theme ID
            Name = "My Tokyo Neon Copy",
            Version = "1.0.0",
            Colors = new ThemeColorPalette
            {
                Background = "#0B0E17",
                CardSurface = "#121826",
                Primary = "#00F0FF",
                PrimaryText = "#FFFFFF",
                Border = "#27324D"
            }
        };

        var report = ThemeQualityAuditor.Audit(manifest);

        Assert.False(report.IsQualified);
        Assert.Contains(report.Errors, e => e.Contains("collides with an official built-in theme"));
    }

    [Fact]
    public void Audit_InvalidThemeId_IsRejected()
    {
        var manifest = new ThemeManifest
        {
            Id = "Invalid_Theme_ID!@#",
            Name = "Invalid ID Theme",
            Version = "1.0.0",
            Colors = new ThemeColorPalette
            {
                Background = "#000000",
                CardSurface = "#111111",
                Primary = "#00FF00",
                PrimaryText = "#FFFFFF",
                Border = "#222222"
            }
        };

        var report = ThemeQualityAuditor.Audit(manifest);

        Assert.False(report.IsQualified);
        Assert.Contains(report.Errors, e => e.Contains("kebab-case", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Audit_AllBuiltInThemes_PassQualityAudit()
    {
        var builtInThemes = BuiltInThemes.GetAll();
        Assert.NotEmpty(builtInThemes);

        foreach (var theme in builtInThemes)
        {
            // "system" is a dynamic pass-through mode without a static palette
            if (string.Equals(theme.Id, "system", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Built-in IDs are exempt from the duplicate built-in error check in their own audit
            var manifestCopy = new ThemeManifest
            {
                Id = "custom-" + theme.Id,
                Name = theme.Name,
                Author = theme.Author,
                Version = theme.Version,
                BaseTheme = theme.BaseTheme,
                Colors = theme.Colors
            };

            var report = ThemeQualityAuditor.Audit(manifestCopy);
            Assert.True(report.IsQualified, $"Built-in theme '{theme.Name}' failed quality audit with errors: {string.Join(", ", report.Errors)}");
        }
    }

    [Fact]
    public void Audit_CrimsonNoirTheme_PassesQualityAudit()
    {
        var manifest = new ThemeManifest
        {
            Id = "crimson-noir",
            Name = "Crimson Noir",
            Author = "FoliCon Community",
            Version = "1.0.0",
            BaseTheme = "Dark",
            Colors = new ThemeColorPalette
            {
                Background = "#0F0E11",
                CardSurface = "#17151A",
                ElevatedSurface = "#221E26",
                Primary = "#E11D48",
                PrimaryHover = "#FB7185",
                PrimaryPressed = "#BE123C",
                Accent = "#F43F5E",
                Border = "#332B38",
                PrimaryText = "#FFF1F2",
                SecondaryText = "#FB7185",
                ThirdText = "#9F8E9C"
            }
        };

        var report = ThemeQualityAuditor.Audit(manifest);

        Assert.True(report.IsQualified);
        Assert.Empty(report.Errors);
        Assert.True(report.PrimaryTextContrastRatio >= 10.0, "Crimson Noir text contrast should be very high.");
    }
}
