#nullable enable
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using FoliCon.Properties.Langs;

namespace FoliconTest;

/// <summary>
/// Guards the translated overlay strings across every shipped locale.
///
/// <para>
/// A resource string is only exercised when the code path that shows it runs, so a translation
/// that dropped a <c>{0}</c> or mangled a <c>{0:F1}</c> would sit unnoticed until a user in that
/// language hit the message — and a surplus placeholder throws <see cref="FormatException"/>
/// outright. These tests format every overlay string in every locale instead.
/// </para>
/// </summary>
public class OverlayLocalizationTests
{
    private static readonly string[] Cultures = ["ar", "es", "hi", "ja", "pt", "ru", "zh"];

    /// <summary>Matches {0} and {0:F1} alike, capturing the argument index.</summary>
    private static readonly Regex Placeholder = new(@"\{(\d+)(?::[^}]*)?\}", RegexOptions.Compiled);

    /// <summary>
    /// Every LangKeys constant belonging to the overlay feature. Read by reflection so a key
    /// added later is covered without touching this file.
    /// </summary>
    public static TheoryData<string> OverlayKeys
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var key in GetOverlayKeys())
            {
                data.Add(key);
            }
            return data;
        }
    }

    private static IEnumerable<string> GetOverlayKeys() =>
        typeof(LangKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Where(name => name.StartsWith("Overlay", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal);

    [Theory]
    [MemberData(nameof(OverlayKeys))]
    public void EveryOverlayString_IsTranslatedInEveryLocale(string key)
    {
        var english = Lang.ResourceManager.GetString(key, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(english), $"'{key}' has no neutral value.");

        foreach (var culture in Cultures)
        {
            var translated = Lang.ResourceManager.GetString(key, new CultureInfo(culture));
            Assert.False(string.IsNullOrWhiteSpace(translated),
                $"'{key}' is missing or blank in '{culture}'.");
        }
    }

    [Theory]
    [MemberData(nameof(OverlayKeys))]
    public void EveryTranslation_KeepsThePlaceholdersOfItsSource(string key)
    {
        var english = Lang.ResourceManager.GetString(key, CultureInfo.InvariantCulture)!;
        var expected = IndexesIn(english);

        foreach (var culture in Cultures)
        {
            var translated = Lang.ResourceManager.GetString(key, new CultureInfo(culture))!;

            // A missing index prints nothing; a surplus one throws when the string is formatted.
            Assert.True(expected.SetEquals(IndexesIn(translated)),
                $"'{key}' in '{culture}' uses placeholders " +
                $"[{string.Join(",", IndexesIn(translated).Order())}] " +
                $"but the source uses [{string.Join(",", expected.Order())}].");
        }
    }

    [Theory]
    [MemberData(nameof(OverlayKeys))]
    public void EveryTranslation_FormatsWithoutThrowing(string key)
    {
        var english = Lang.ResourceManager.GetString(key, CultureInfo.InvariantCulture)!;
        var arity = IndexesIn(english).Count;
        if (arity == 0)
        {
            return;
        }

        // Doubles satisfy the numeric specifiers ({0:F1}, {1:0.##}) and still render for the
        // plain {0} slots, so one argument array covers every string.
        var args = Enumerable.Range(0, arity).Select(object (i) => (double)(i + 1)).ToArray();

        foreach (var culture in Cultures)
        {
            var info = new CultureInfo(culture);
            var translated = Lang.ResourceManager.GetString(key, info)!;

            var exception = Record.Exception(() => string.Format(info, translated, args));
            Assert.True(exception is null,
                $"'{key}' in '{culture}' failed to format: {exception?.Message}");
        }
    }

    /// <summary>
    /// Catches a locale whose satellite assembly is missing or was never populated: the
    /// ResourceManager would silently fall back to English and every other assertion here
    /// would still pass.
    /// </summary>
    [Theory]
    [InlineData("ar")]
    [InlineData("es")]
    [InlineData("hi")]
    [InlineData("ja")]
    [InlineData("pt")]
    [InlineData("ru")]
    [InlineData("zh")]
    public void EachLocale_ActuallyDiffersFromEnglish(string culture)
    {
        var info = new CultureInfo(culture);
        var keys = GetOverlayKeys().ToList();

        var translated = keys.Count(key =>
            Lang.ResourceManager.GetString(key, CultureInfo.InvariantCulture)
            != Lang.ResourceManager.GetString(key, info));

        // Some strings are legitimately identical across locales — "X", "Y", "ID", "v{0}",
        // "{0} B" — so this is a floor, not a demand that every string differ.
        Assert.True(translated > keys.Count * 0.8,
            $"only {translated} of {keys.Count} overlay strings differ from English in " +
            $"'{culture}'; the satellite assembly is probably not being loaded.");
    }

    [Fact]
    public void NoTranslation_LeavesAnEmptyOrStrayFormatBrace()
    {
        var problems = new List<string>();

        foreach (var key in GetOverlayKeys())
        {
            foreach (var culture in Cultures)
            {
                var value = Lang.ResourceManager.GetString(key, new CultureInfo(culture))!;

                // Strip valid placeholders; anything left with a brace is a typo such as
                // "{0" or "{ 0}" that string.Format would reject or render literally.
                var stripped = Placeholder.Replace(value, string.Empty);
                if (stripped.Contains('{') || stripped.Contains('}'))
                {
                    problems.Add($"{key} [{culture}]: {value}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static HashSet<int> IndexesIn(string value) =>
        [.. Placeholder.Matches(value).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))];
}
