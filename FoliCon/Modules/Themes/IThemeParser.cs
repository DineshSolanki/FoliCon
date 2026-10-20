#nullable enable
using System.IO;
using System.Windows;
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Parser and synthesizer for FoliCon hybrid custom themes (JSON manifests and XAML packages).
/// </summary>
public interface IThemeParser
{
    /// <summary>
    /// Validates raw JSON string against the FoliCon theme manifest schema and constraints.
    /// </summary>
    ThemeValidationResult Validate(string json);

    /// <summary>
    /// Parses JSON string into a validated <see cref="ThemeManifest"/>.
    /// Throws <see cref="InvalidDataException"/> if validation fails.
    /// </summary>
    ThemeManifest Parse(string json);

    /// <summary>
    /// Synthesizes a fully frozen WPF <see cref="ResourceDictionary"/> from declarative manifest color and material tokens.
    /// </summary>
    ResourceDictionary SynthesizeResourceDictionary(ThemeManifest manifest);

    /// <summary>
    /// Safely loads an advanced custom XAML resource dictionary from disk, enforcing security sandboxing.
    /// </summary>
    ResourceDictionary LoadPackageResourceDictionary(string xamlFilePath);
}
