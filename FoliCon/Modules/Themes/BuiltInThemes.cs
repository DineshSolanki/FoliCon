#nullable enable
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Pre-configured theme manifests for FoliCon's official built-in themes.
/// Only Dark, Light, System, and The Butterfly are built-in themes.
/// All other themes are community themes managed through the plugin system.
/// </summary>
public static class BuiltInThemes
{
    private static readonly List<ThemeManifest> AllBuiltInThemes =
    [
        new()
        {
            Id = "dark",
            Name = "Dark",
            Description = "Refined dark mode engineered for focus and low eye strain.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Dark", "Default"],
            Colors = new ThemeColorPalette
            {
                Background = "#1C1C1C",
                CardSurface = "#242424",
                ElevatedSurface = "#2E2E2E",
                Primary = "#3B82F6",
                Border = "#3D3D3D",
                PrimaryText = "#F8FAFC",
                SecondaryText = "#94A3B8",
                ThirdText = "#64748B",
                Success = "#10B981",
                Warning = "#F59E0B",
                Error = "#EF4444"
            }
        },
        new()
        {
            Id = "light",
            Name = "Light",
            Description = "Clean daylight UI with crisp contrast and readability.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Light",
            IsBuiltIn = true,
            Tags = ["Official", "Light", "Clean"],
            Colors = new ThemeColorPalette
            {
                Background = "#FFFFFF",
                CardSurface = "#F8FAFC",
                ElevatedSurface = "#FFFFFF",
                Primary = "#3B82F6",
                Border = "#E2E8F0",
                PrimaryText = "#0F172A",
                SecondaryText = "#64748B",
                ThirdText = "#94A3B8",
                Success = "#10B981",
                Warning = "#F59E0B",
                Error = "#EF4444"
            }
        },
        new()
        {
            Id = "system",
            Name = "System",
            Description = "Dynamically matches your Windows OS personalization preference.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Dynamic"]
        },
        new()
        {
            Id = "the-butterfly",
            Name = "The Butterfly",
            Description = "Deep midnight violet with luminous lavender and fuchsia accents.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Violet", "Radiant"],
            Colors = new ThemeColorPalette
            {
                Background = "#17152B",
                CardSurface = "#201D38",
                ElevatedSurface = "#2A254B",
                Primary = "#7651E8",
                PrimaryHover = "#B69AFF",
                PrimaryPressed = "#5C3DBE",
                Accent = "#B69AFF",
                Border = "#383161",
                PrimaryText = "#F5F3FF",
                SecondaryText = "#B69AFF",
                ThirdText = "#887DAE"
            }
        }
    ];

    public static IReadOnlyList<ThemeManifest> GetAll() => AllBuiltInThemes.AsReadOnly();
}
