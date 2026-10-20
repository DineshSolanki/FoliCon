#nullable enable
using System.Collections.Generic;

namespace FoliCon.Models.Themes;

/// <summary>
/// Result of a theme validation check containing validation state, errors, and warnings.
/// </summary>
public sealed class ThemeValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = [];

    public List<string> Warnings { get; } = [];

    public static ThemeValidationResult Success() => new();

    public static ThemeValidationResult Failure(string error)
    {
        var result = new ThemeValidationResult();
        result.Errors.Add(error);
        return result;
    }
}
