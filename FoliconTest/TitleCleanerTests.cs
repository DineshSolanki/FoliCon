#nullable enable
using System.IO;
using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Models.Enums;
using FoliCon.Modules.utils;
using Xunit.Abstractions;

namespace FoliconTest;

/// <summary>
/// Unit tests for <see cref="TitleCleaner"/> and related title parsing logic in <see cref="DataUtils"/>.
/// </summary>
public class TitleCleanerTests(ITestOutputHelper? output = null)
{
    #region Clean Tests

    [Theory]
    [InlineData("Spider-Man", "Spider Man")]
    [InlineData("The_Dark_Knight", "The Dark Knight")]
    [InlineData("The.Lord.of.the.Rings", "The Lord of the Rings")]
    [InlineData("Breaking_Bad-The_Final.Pilot", "Breaking Bad The Final Pilot")]
    public void Clean_NormalizesSeparators_ReplacesHyphensUnderscoresAndDotsWithSpaces(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clean_ReplacesModifierLetterColonWithAsciiColon()
    {
        // \uA789 is Modifier Letter Colon '꞉'
        const string input = "Star Wars\uA789 A New Hope";
        var result = TitleCleaner.Clean(input);
        Assert.Equal("Star Wars: A New Hope", result);
    }

    [Fact]
    public void Clean_ReplacesRatioSymbolWithAsciiColon()
    {
        // \u2236 is Ratio Symbol '∶'
        const string input = "Mission\u2236 Impossible";
        var result = TitleCleaner.Clean(input);
        Assert.Equal("Mission: Impossible", result);
    }

    [Fact]
    public void Clean_RemovesNonAsciiCharacters()
    {
        // Emojis and accented characters outside 0x00-0x7F are removed
        var input = "Am\u00E9lie \uD83C\uDFAC";
        var result = TitleCleaner.Clean(input);
        Assert.Equal("Amlie", result);
    }

    [Theory]
    [InlineData("Avatar 1080p", "Avatar")]
    [InlineData("Avatar 1080i", "Avatar")]
    [InlineData("Avatar 720p", "Avatar")]
    [InlineData("Avatar 720i", "Avatar")]
    [InlineData("Avatar 420p", "Avatar")]
    [InlineData("Avatar (1080p)", "Avatar")]
    [InlineData("Avatar (720p)", "Avatar")]
    [InlineData("Avatar (420p)", "Avatar")]
    [InlineData("Avatar (720)", "Avatar")]
    [InlineData("Avatar 1080P", "Avatar")]
    public void Clean_RemovesResolutionTokens(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Inception 2010 1080p BluRay x264", "Inception")]
    [InlineData("The Dark Knight 2008 720p WEB-DL AAC", "The Dark Knight")]
    [InlineData("Interstellar.2014.1080p.BluRay.x264-SPARKS", "Interstellar")]
    [InlineData("Gladiator (2000) Remastered 1080p", "Gladiator")]
    public void Clean_RemovesYearAndAllTrailingInformation(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Movie Name [Extended Cut]", "Movie Name")]
    [InlineData("[ReleaseGroup] Movie Name", "Movie Name")]
    [InlineData("Movie [Director's Cut] Special", "Movie Special")]
    public void Clean_RemovesContentEnclosedInSquareBrackets(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("The    Godfather", "The Godfather")]
    [InlineData("  Pulp   Fiction  ", "Pulp Fiction")]
    [InlineData("   Inception   ", "Inception")]
    public void Clean_CollapsesMultipleSpacesAndTrims(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2020", "2020")]
    [InlineData("1080p", "1080p")]
    [InlineData("[Custom]", "[Custom]")]
    [InlineData("  420p  ", "420p")]
    [InlineData("2001 A Space Odyssey", "2001 A Space Odyssey")]
    public void Clean_WhenCleanedResultIsWhitespaceOrEmpty_FallsBackToTrimmedOriginal(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\t \n ", "")]
    public void Clean_EmptyOrWhitespaceInput_ReturnsEmptyString(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clean_MultipleUnicodeColonsInSameTitle_ReplacesAllWithAsciiColon()
    {
        var input = "Title\uA789 Part 1\u2236 Subtitle";
        var result = TitleCleaner.Clean(input);
        Assert.Equal("Title: Part 1: Subtitle", result);
    }

    [Theory]
    [InlineData("Oppenheimer 2023 IMAX 2160p UHD HDR TrueHD Atmos x265", "Oppenheimer")]
    [InlineData("Dune 2021 Remux 1080p AVC DTS-HD MA 5.1", "Dune")]
    public void Clean_ComplexReleaseTagsAfterYear_StripsEntireReleaseSuffix(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Cobra Kai Season 2", "Cobra Kai")]
    [InlineData("Arcane Season 2", "Arcane")]
    [InlineData("The Boys S03", "The Boys")]
    [InlineData("Dark Staffel 1", "Dark")]
    [InlineData("Lupin Saison 2", "Lupin")]
    [InlineData("Stranger Things Season Four", "Stranger Things")]
    public void Clean_RemovesSeasonTagsFromTitle(string input, string expected)
    {
        var result = TitleCleaner.Clean(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region CleanAndParse Tests

    [Fact]
    public void CleanAndParse_WithTmdbId_ExtractsTmdbIdAndCleansTitle()
    {
        var input = "Inception {tmdb-27205}";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Inception", result.Title);
        Assert.Equal(IdType.Tmdb, result.IdType);
        Assert.Equal("27205", result.Id);
        Assert.Equal(0, result.Year);
    }

    [Fact]
    public void CleanAndParse_WithTvdbId_ExtractsTvdbIdAndCleansTitle()
    {
        var input = "Breaking Bad {tvdb-81189}";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Breaking Bad", result.Title);
        Assert.Equal(IdType.Tvdb, result.IdType);
        Assert.Equal("81189", result.Id);
        Assert.Equal(0, result.Year);
    }

    [Theory]
    [InlineData("Show {TMDB-12345}", IdType.Tmdb, "12345")]
    [InlineData("Show {TVDB-67890}", IdType.Tvdb, "67890")]
    [InlineData("Show {TmDb-999}", IdType.Tmdb, "999")]
    public void CleanAndParse_IsCaseInsensitiveForShowIdTag(string input, IdType expectedType, string expectedId)
    {
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Show", result.Title);
        Assert.Equal(expectedType, result.IdType);
        Assert.Equal(expectedId, result.Id);
    }

    [Fact]
    public void CleanAndParse_WithYearInParentheses_ExtractsYearAndCleansTitle()
    {
        var input = "The Batman (2022)";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("The Batman", result.Title);
        Assert.Equal(2022, result.Year);
        Assert.Equal(IdType.None, result.IdType);
        Assert.Equal("0", result.Id);
    }

    [Fact]
    public void CleanAndParse_WithShowIdAndYear_ExtractsBothCorrectly()
    {
        var input = "Severance (2022) {tvdb-371980}";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Severance", result.Title);
        Assert.Equal(2022, result.Year);
        Assert.Equal(IdType.Tvdb, result.IdType);
        Assert.Equal("371980", result.Id);
    }

    [Fact]
    public void CleanAndParse_WithoutShowIdOrYear_ReturnsDefaultMetadataAndCleansTitle()
    {
        var input = "The.Matrix.1080p.BluRay";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("The Matrix", result.Title);
        Assert.Equal(0, result.Year);
        Assert.Equal(IdType.None, result.IdType);
        Assert.Equal("0", result.Id);
    }

    [Fact]
    public void CleanAndParse_ComplexReleaseFolder_ExtractsMetadataAndCleansName()
    {
        var input = "The.Last.of.Us.(2023).{tvdb-392256}.1080p.AMZN.WEB-DL";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("The Last of Us", result.Title);
        Assert.Equal(2023, result.Year);
        Assert.Equal(IdType.Tvdb, result.IdType);
        Assert.Equal("392256", result.Id);
    }

    [Fact]
    public void CleanAndParse_TitleWithColonUnicodeAndTmdbId_PreservesColonAndExtractsId()
    {
        var input = "Spider-Man\uA789 No Way Home (2021) {tmdb-634649}";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Spider Man: No Way Home", result.Title);
        Assert.Equal(2021, result.Year);
        Assert.Equal(IdType.Tmdb, result.IdType);
        Assert.Equal("634649", result.Id);
    }

    [Fact]
    public void CleanAndParse_ShowIdAtBeginning_ExtractsCorrectly()
    {
        var input = "{tmdb-550} Fight Club (1999)";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Fight Club", result.Title);
        Assert.Equal(1999, result.Year);
        Assert.Equal(IdType.Tmdb, result.IdType);
        Assert.Equal("550", result.Id);
    }

    [Theory]
    [InlineData("Movie {imdb-tt1234567}")]
    [InlineData("Movie {unknown-9999}")]
    public void CleanAndParse_UnrecognizedShowIdTag_IgnoredAsShowId(string input)
    {
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal(IdType.None, result.IdType);
        Assert.Equal("0", result.Id);
    }

    [Theory]
    [InlineData("Film (99)")]
    [InlineData("Film (19999)")]
    public void CleanAndParse_NonFourDigitYearInParentheses_NotParsedAsYear(string input)
    {
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal(0, result.Year);
    }

    [Fact]
    public void CleanAndParse_LargeShowIdNumber_ParsedCorrectly()
    {
        var input = "Show {tvdb-1029384756}";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("1029384756", result.Id);
        Assert.Equal(IdType.Tvdb, result.IdType);
    }

    [Theory]
    [InlineData("Arcane Season 2", "Arcane", 2)]
    [InlineData("Cobra Kai Season 02", "Cobra Kai", 2)]
    [InlineData("Cobra Kai Season 6", "Cobra Kai", 6)]
    [InlineData("The Boys S03", "The Boys", 3)]
    [InlineData("Breaking Bad S1", "Breaking Bad", 1)]
    [InlineData("Breaking Bad S01E01", "Breaking Bad", 1)]
    [InlineData("Dark Staffel 2", "Dark", 2)]
    [InlineData("Lupin Saison 1", "Lupin", 1)]
    [InlineData("Doctor Who Series 5", "Doctor Who", 5)]
    [InlineData("Stranger Things Season Four", "Stranger Things", 4)]
    [InlineData("Severance (Season 1)", "Severance", 1)]
    [InlineData("Game of Thrones [S02]", "Game of Thrones", 2)]
    public void CleanAndParse_WithSeason_ExtractsSeasonAndCleansTitle(string input, string expectedTitle, int expectedSeason)
    {
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedSeason, result.Season);
    }

    [Fact]
    public void CleanAndParse_WithSeasonYearAndShowId_ExtractsAllFields()
    {
        var input = "Arcane Season 2 (2024) {tmdb-3153}";
        var result = TitleCleaner.CleanAndParse(input);

        Assert.Equal("Arcane", result.Title);
        Assert.Equal(2, result.Season);
        Assert.Equal(2024, result.Year);
        Assert.Equal(IdType.Tmdb, result.IdType);
        Assert.Equal("3153", result.Id);
    }

    #endregion

    #region ParsedTitle Record Tests

    [Fact]
    public void ParsedTitle_RecordEquality_WorksByValue()
    {
        var item1 = new ParsedTitle("Inception", IdType.Tmdb, "27205", 2010);
        var item2 = new ParsedTitle("Inception", IdType.Tmdb, "27205", 2010);
        var item3 = new ParsedTitle("Inception", IdType.Tmdb, "27205", 2011);

        Assert.Equal(item1, item2);
        Assert.NotEqual(item1, item3);
        Assert.True(item1 == item2);
        Assert.False(item1 == item3);
    }

    [Fact]
    public void ParsedTitle_WithExpression_CreatesModifiedCopy()
    {
        var original = new ParsedTitle("Matrix", IdType.Tmdb, "603", 1999);
        var modified = original with { Year = 2003, Title = "Matrix Reloaded" };

        Assert.Equal("Matrix Reloaded", modified.Title);
        Assert.Equal(2003, modified.Year);
        Assert.Equal(IdType.Tmdb, modified.IdType);
        Assert.Equal("603", modified.Id);
    }

    #endregion

    #region DataUtils.ShouldUseParsedTitle Tests

    [Fact]
    public void ShouldUseParsedTitle_NullInput_ReturnsFalse()
    {
        Assert.False(DataUtils.ShouldUseParsedTitle(null!));
    }

    [Fact]
    public void ShouldUseParsedTitle_DefaultEmptyParsedTitle_ReturnsFalse()
    {
        var parsedTitle = new ParsedTitle("Title", IdType.None, "0", 0);
        Assert.False(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2024)]
    public void ShouldUseParsedTitle_WithYear_ReturnsTrue(int year)
    {
        var parsedTitle = new ParsedTitle("Title", IdType.None, "0", year);
        Assert.True(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    [Theory]
    [InlineData(IdType.Tmdb, "12345")]
    [InlineData(IdType.Tvdb, "67890")]
    public void ShouldUseParsedTitle_WithValidShowId_ReturnsTrue(IdType idType, string id)
    {
        var parsedTitle = new ParsedTitle("Title", idType, id, 0);
        Assert.True(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    [Fact]
    public void ShouldUseParsedTitle_WithBothYearAndShowId_ReturnsTrue()
    {
        var parsedTitle = new ParsedTitle("Title", IdType.Tvdb, "81189", 2008);
        Assert.True(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    [Fact]
    public void ShouldUseParsedTitle_WithIdTypeButZeroIdAndZeroYear_ReturnsFalse()
    {
        var parsedTitle = new ParsedTitle("Title", IdType.Tmdb, "0", 0);
        Assert.False(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    [Fact]
    public void ShouldUseParsedTitle_WithNoneIdTypeEvenIfIdIsNonZero_ReturnsFalse()
    {
        var parsedTitle = new ParsedTitle("Title", IdType.None, "12345", 0);
        Assert.False(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void ShouldUseParsedTitle_WithSeason_ReturnsTrue(int season)
    {
        var parsedTitle = new ParsedTitle("Title", IdType.None, "0", 0, season);
        Assert.True(DataUtils.ShouldUseParsedTitle(parsedTitle));
    }

    #endregion

    #region Real Folder Integration Tests

    [Fact]
    public void CleanAndParse_AllFoldersInRealTestingDirectories_ProcessWithoutErrors()
    {
        var directories = new[]
        {
            @"J:\FoliCon testing\Games",
            @"J:\FoliCon testing\Movies & TV"
        };

        var issues = new List<string>();
        var totalCount = 0;

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                output?.WriteLine($"Directory does not exist: {dir}");
                continue;
            }

            var folderNames = Directory.EnumerateDirectories(dir).Select(Path.GetFileName).Where(n => !string.IsNullOrEmpty(n)).Cast<string>();

            var isGame = dir.Contains("Games", StringComparison.OrdinalIgnoreCase);
            var mediaType = isGame ? MediaTypes.game : MediaTypes.movie;

            foreach (var folder in folderNames)
            {
                totalCount++;

                // 1. Clean should never throw and never return null or empty
                string cleaned;
                try
                {
                    cleaned = TitleCleaner.Clean(folder, mediaType);
                }
                catch (Exception ex)
                {
                    issues.Add($"[Clean Exception] Folder '{folder}' threw {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    issues.Add($"[Empty Cleaned Title] Folder '{folder}' resulted in empty cleaned title");
                }

                // 2. CleanAndParse should never throw and should return valid ParsedTitle
                ParsedTitle parsed;
                try
                {
                    parsed = TitleCleaner.CleanAndParse(folder, mediaType);
                }
                catch (Exception ex)
                {
                    issues.Add($"[CleanAndParse Exception] Folder '{folder}' threw {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(parsed.Title))
                {
                    issues.Add($"[Empty Parsed Title] Folder '{folder}' resulted in empty parsed title");
                }

                // 3. If folder has {tmdb-XXX} or {tvdb-XXX}, verify ShowId and IdType
                var tmdbMatch = System.Text.RegularExpressions.Regex.Match(folder, @"\{(tmdb|tvdb)-(\d+)\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (tmdbMatch.Success)
                {
                    var expectedType = Enum.Parse<IdType>(tmdbMatch.Groups[1].Value, true);
                    var expectedId = tmdbMatch.Groups[2].Value;

                    if (parsed.IdType != expectedType)
                    {
                        issues.Add($"[ShowId Type Mismatch] Folder '{folder}': expected IdType {expectedType}, got {parsed.IdType}");
                    }
                    if (parsed.Id != expectedId)
                    {
                        issues.Add($"[ShowId Mismatch] Folder '{folder}': expected Id {expectedId}, got {parsed.Id}");
                    }
                    if (parsed.Title.Contains(tmdbMatch.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add($"[Unstripped ShowId Tag] Folder '{folder}': parsed title still contains show tag '{parsed.Title}'");
                    }
                }

                // 4. If folder has (YYYY), verify Year
                var yearMatch = System.Text.RegularExpressions.Regex.Match(folder, @"\((\d{4})\)");
                if (yearMatch.Success)
                {
                    var expectedYear = int.Parse(yearMatch.Groups[1].Value);
                    if (parsed.Year != expectedYear)
                    {
                        issues.Add($"[Year Mismatch] Folder '{folder}': expected Year {expectedYear}, got {parsed.Year}");
                    }
                    if (parsed.Title.Contains($"({expectedYear})"))
                    {
                        issues.Add($"[Unstripped Year Tag] Folder '{folder}': parsed title still contains year '({expectedYear})'");
                    }
                }

                // 5. If folder has Season, verify Season
                var seasonMatch = System.Text.RegularExpressions.Regex.Match(folder, @"(?i)(?:season|staffel|saison|series)[._\s-]+(\d{1,2})");
                if (seasonMatch.Success)
                {
                    var expectedSeason = int.Parse(seasonMatch.Groups[1].Value);
                    if (parsed.Season != expectedSeason)
                    {
                        issues.Add($"[Season Mismatch] Folder '{folder}': expected Season {expectedSeason}, got {parsed.Season}");
                    }
                    if (parsed.Title.Contains(seasonMatch.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add($"[Unstripped Season Tag] Folder '{folder}': parsed title still contains season tag '{parsed.Title}'");
                    }
                }
            }
        }

        output?.WriteLine($"Total real folders checked: {totalCount}");
        if (issues.Count > 0)
        {
            output?.WriteLine($"Found {issues.Count} issues:");
            foreach (var issue in issues.Take(30))
            {
                output?.WriteLine($"  - {issue}");
            }
        }

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("10 Things I Hate About You {tmdb-1346}", "10 Things I Hate About You", IdType.Tmdb, "1346", 0)]
    [InlineData("12 Years a Slave (1985)", "12 Years a Slave", IdType.None, "0", 1985)]
    [InlineData("1917 {tmdb-1752}", "1917", IdType.Tmdb, "1752", 0)]
    [InlineData("2 Broke Girls {tmdb-4349}", "2 Broke Girls", IdType.Tmdb, "4349", 0)]
    [InlineData("2.5 Dimensional Seduction (1982)", "2 5 Dimensional Seduction", IdType.None, "0", 1982)]
    [InlineData("2046 {tmdb-3900}", "2046", IdType.Tmdb, "3900", 0)]
    [InlineData("Paprika 2006 (2022)", "Paprika", IdType.None, "0", 2022)]
    [InlineData("Perfect Blue 1997 {tmdb-5015}", "Perfect Blue", IdType.Tmdb, "5015", 0)]
    [InlineData("Tokyo Godfathers 2003 (2002)", "Tokyo Godfathers", IdType.None, "0", 2002)]
    [InlineData("One Cut of the Dead (2017) (2016)", "One Cut of the Dead", IdType.None, "0", 2017)]
    [InlineData("Star Wars? The Clone Wars", "Star Wars? The Clone Wars", IdType.None, "0", 0)]
    [InlineData("Steins;Gate 0 {tmdb-7142}", "Steins;Gate 0", IdType.Tmdb, "7142", 0)]
    [InlineData("The Night is Short, Walk On Girl", "The Night is Short, Walk On Girl", IdType.None, "0", 0)]
    [InlineData("S1m0ne (2002)", "S1m0ne", IdType.None, "0", 2002)]
    public void CleanAndParse_MoviesAndTVSamples_MatchesExpectedResults(
        string folderName, string expectedTitle, IdType expectedIdType, string expectedId, int expectedYear)
    {
        var result = TitleCleaner.CleanAndParse(folderName);

        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedIdType, result.IdType);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(expectedYear, result.Year);
    }

    [Theory]
    [InlineData("Age of Empires II Definitive Edition [Ubisoft Connect]", "Age of Empires II Definitive Edition", IdType.None, "0", 0)]
    [InlineData("Alan Wake [GOG]", "Alan Wake", IdType.None, "0", 0)]
    [InlineData("A-Train 9 (2021)", "A Train 9", IdType.None, "0", 2021)]
    [InlineData("Age of Empires IV (2004)", "Age of Empires IV", IdType.None, "0", 2004)]
    [InlineData("1-2-Switch v0.7", "1 2 Switch", IdType.None, "0", 0)]
    [InlineData("Age of Wonders 4 _ PROPHET Repack", "Age of Wonders 4", IdType.None, "0", 0)]
    [InlineData("Act of War Direct Action ~ CODEX Repack", "Act of War Direct Action", IdType.None, "0", 0)]
    [InlineData("Advance Wars - DODI Repack", "Advance Wars", IdType.None, "0", 0)]
    [InlineData("7 Days to Die - KaosKrew Repack", "7 Days to Die", IdType.None, "0", 0)]
    [InlineData("A Short Hike _ REVOLT Repack", "A Short Hike", IdType.None, "0", 0)]
    [InlineData("Ori and the Blind Forest v1.0", "Ori and the Blind Forest", IdType.None, "0", 0)]
    [InlineData("F1 2010 . DEFAULTR Repack", "F1 2010", IdType.None, "0", 0)]
    [InlineData("F1 2021 (2026)", "F1 2021", IdType.None, "0", 2026)]
    [InlineData("Shadow of the Tomb Raider-RAZOR1911 [DUNE]", "Shadow of the Tomb Raider", IdType.None, "0", 0)]
    [InlineData("Animal Crossing City Folk - Windows Store Edition", "Animal Crossing City Folk", IdType.None, "0", 0)]
    [InlineData("A-Train-Win64-SiMPLEX", "A Train", IdType.None, "0", 0)]
    [InlineData("Assassin's Creed - black_box", "Assassin's Creed", IdType.None, "0", 0)]
    public void CleanAndParse_GamesSamples_MatchesExpectedResults(
        string folderName, string expectedTitle, IdType expectedIdType, string expectedId, int expectedYear)
    {
        var result = TitleCleaner.CleanAndParse(folderName, MediaTypes.game);

        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedIdType, result.IdType);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(expectedYear, result.Year);
    }

    #endregion
}
