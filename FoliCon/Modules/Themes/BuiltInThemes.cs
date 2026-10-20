#nullable enable
using System.Collections.Generic;
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Pre-configured theme manifests for FoliCon's built-in themes.
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
                ThirdText = "#64748B"
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
                ThirdText = "#94A3B8"
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
        },
        new()
        {
            Id = "tokyo-neon",
            Name = "Tokyo Neon",
            Description = "High-energy cyberpunk palette with electric cyan on pitch-black.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Cyberpunk", "Neon"],
            Colors = new ThemeColorPalette
            {
                Background = "#0B0E17",
                CardSurface = "#111827",
                ElevatedSurface = "#1F2937",
                Primary = "#088395",
                PrimaryHover = "#0EA5E9",
                PrimaryPressed = "#066675",
                Accent = "#0EA5E9",
                Border = "#1E293B",
                PrimaryText = "#F8FAFC",
                SecondaryText = "#94A3B8",
                ThirdText = "#64748B"
            }
        },
        new()
        {
            Id = "mica-dark-obsidian",
            Name = "Mica Dark Obsidian",
            Description = "Ultra-deep onyx and obsidian with royal amethyst highlights.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Obsidian", "Amethyst"],
            Colors = new ThemeColorPalette
            {
                Background = "#090D16",
                CardSurface = "#131826",
                ElevatedSurface = "#1C2438",
                Primary = "#8B5CF6",
                PrimaryHover = "#C4B5FD",
                PrimaryPressed = "#6D28D9",
                Accent = "#C4B5FD",
                Border = "#252F4A",
                PrimaryText = "#F8FAFC",
                SecondaryText = "#94A3B8",
                ThirdText = "#64748B"
            }
        },
        new()
        {
            Id = "nordic-pine",
            Name = "Nordic Pine",
            Description = "Calm Scandinavian forest tones with deep evergreen and emerald accents.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Emerald", "Green"],
            Colors = new ThemeColorPalette
            {
                Background = "#0C1613",
                CardSurface = "#14241F",
                ElevatedSurface = "#1D332C",
                Primary = "#10B981",
                PrimaryHover = "#6EE7B7",
                PrimaryPressed = "#059669",
                Accent = "#6EE7B7",
                Border = "#214237",
                PrimaryText = "#ECFDF5",
                SecondaryText = "#A7F3D0",
                ThirdText = "#6EE7B7"
            }
        },
        new()
        {
            Id = "cinema-35mm",
            Name = "Cinema 35mm",
            Description = "Warm filmic amber and burnished gold inspired by classic celluloid.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Filmic", "Amber"],
            Colors = new ThemeColorPalette
            {
                Background = "#141210",
                CardSurface = "#1E1A16",
                ElevatedSurface = "#2B251F",
                Primary = "#F59E0B",
                PrimaryHover = "#FCD34D",
                PrimaryPressed = "#D97706",
                Accent = "#FCD34D",
                Border = "#3B332A",
                PrimaryText = "#FFFBEB",
                SecondaryText = "#FDE68A",
                ThirdText = "#D97706"
            }
        }
    ];

    public static IReadOnlyList<ThemeManifest> GetAll() => AllBuiltInThemes.AsReadOnly();
}
