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
            Id = "love",
            Name = "Love",
            Description = "Romantic twilight atmosphere with luminous neon heart accents, cherry blossom glow, and scenic living artwork.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Romantic", "Neon", "Love", "Living"],
            Colors = new ThemeColorPalette
            {
                Background = "#080610",
                CardSurface = "#171018",
                ElevatedSurface = "#22151E",
                Primary = "#FF4D7D",
                PrimaryHover = "#FF8CB3",
                PrimaryPressed = "#D6285A",
                Accent = "#FF8CB3",
                Border = "#6A3264",
                PrimaryText = "#F8E9EF",
                SecondaryText = "#C6A7B3",
                ThirdText = "#8F6580",
                Success = "#00D6A0",
                Warning = "#F59E0B",
                Error = "#E83E5B"
            },
            Effects = new ThemeEffectsTokens
            {
                GlowIntensity = 0.85,
                PanelOpacity = 0.70,
                BloomRadius = 26.0,
                ShadowDepth = 0.0,
                VignetteEnabled = true
            },
            Shapes = new ThemeShapesTokens
            {
                PanelRadius = 12.0,
                ButtonRadius = 10.0,
                CardRadius = 10.0
            },
            Assets = new ThemeAssetsTokens
            {
                Wallpaper = "pack://application:,,,/FoliCon;component/Resources/Themes/love_backdrop.jpg",
                ActionBanner = "pack://application:,,,/FoliCon;component/Resources/Themes/love_banner.jpg",
                MovieCard = "pack://application:,,,/FoliCon;component/Resources/Themes/love_card_movie.jpg",
                TvCard = "pack://application:,,,/FoliCon;component/Resources/Themes/love_card_tv.jpg",
                GameCard = "pack://application:,,,/FoliCon;component/Resources/Themes/love_card_game.jpg",
                AutoCard = "pack://application:,,,/FoliCon;component/Resources/Themes/love_card_auto.jpg",
                BrandingGeometry = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"
            }
        },
        new()
        {
            Id = "corporate",
            Name = "Corporate Modern",
            Description = "Clean, distraction-free slate and precision enterprise blue engineered for peak productivity.",
            Author = "FoliCon Team",
            Version = "1.0.0",
            BaseTheme = "Dark",
            IsBuiltIn = true,
            Tags = ["Official", "Corporate", "Slate", "Clean", "Minimal"],
            Colors = new ThemeColorPalette
            {
                Background = "#0F172A",
                CardSurface = "#1E293B",
                ElevatedSurface = "#334155",
                Primary = "#2563EB",
                PrimaryHover = "#3B82F6",
                PrimaryPressed = "#1D4ED8",
                Accent = "#60A5FA",
                Border = "#334155",
                PrimaryText = "#F8FAFC",
                SecondaryText = "#94A3B8",
                ThirdText = "#64748B",
                Success = "#10B981",
                Warning = "#F59E0B",
                Error = "#EF4444"
            },
            Effects = new ThemeEffectsTokens
            {
                GlowIntensity = 0.0,
                PanelOpacity = 0.98,
                BloomRadius = 0.0,
                ShadowDepth = 0.0,
                VignetteEnabled = false
            },
            Shapes = new ThemeShapesTokens
            {
                PanelRadius = 8.0,
                ButtonRadius = 6.0,
                CardRadius = 8.0
            },
            Assets = new ThemeAssetsTokens
            {
                BrandingGeometry = "M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"
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
