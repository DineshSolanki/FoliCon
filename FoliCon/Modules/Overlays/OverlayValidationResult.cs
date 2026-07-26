#nullable enable
namespace FoliCon.Modules.Overlays;

/// <summary>
/// Severity of a single validation issue.
/// </summary>
public enum OverlayValidationSeverity
{
    /// <summary>Blocks export, install, and publication.</summary>
    Error,

    /// <summary>Allows export but should be surfaced to the author.</summary>
    Warning
}

/// <summary>
/// A single validation finding, carrying enough identity for the designer to
/// focus the offending field and select the affected element on the canvas.
/// </summary>
public sealed class OverlayValidationIssue(
    OverlayValidationSeverity severity,
    string field,
    string message)
{
    public OverlayValidationSeverity Severity { get; } = severity;

    /// <summary>
    /// Dotted schema path of the offending field, e.g. <c>poster.margin</c> or
    /// <c>baseLayer.imagePath</c>. Empty for package-level findings.
    /// </summary>
    public string Field { get; } = field;

    public string Message { get; } = message;

    /// <summary>
    /// Compatibility with the legacy <c>List&lt;string&gt;</c> contract: existing callers
    /// join issues by their message text.
    /// </summary>
    public override string ToString() => Message;
}

/// <summary>
/// Structured outcome of validating an overlay definition and its package folder.
/// Errors block export; warnings are advisory.
/// </summary>
public sealed class OverlayValidationResult
{
    private readonly List<OverlayValidationIssue> _issues = [];

    public IReadOnlyList<OverlayValidationIssue> Issues => _issues;

    public IEnumerable<OverlayValidationIssue> Errors =>
        _issues.Where(i => i.Severity == OverlayValidationSeverity.Error);

    public IEnumerable<OverlayValidationIssue> Warnings =>
        _issues.Where(i => i.Severity == OverlayValidationSeverity.Warning);

    public int ErrorCount => _issues.Count(i => i.Severity == OverlayValidationSeverity.Error);

    public int WarningCount => _issues.Count(i => i.Severity == OverlayValidationSeverity.Warning);

    /// <summary>True when nothing blocks export. Warnings may still be present.</summary>
    public bool IsValid => ErrorCount == 0;

    public void AddError(string field, string message) =>
        _issues.Add(new OverlayValidationIssue(OverlayValidationSeverity.Error, field, message));

    public void AddWarning(string field, string message) =>
        _issues.Add(new OverlayValidationIssue(OverlayValidationSeverity.Warning, field, message));

    /// <summary>
    /// Error messages only, in the legacy shape used by <see cref="Internal.OverlayValidator.Validate"/>.
    /// </summary>
    public List<string> ToErrorMessages() => Errors.Select(e => e.Message).ToList();
}
