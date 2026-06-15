namespace FoliCon.Modules;

/// <summary>
/// An exception that carries both an English message (for logging via NLog)
/// and a localized message (for display in the UI).
/// NLog logs <see cref="Exception.Message"/> which is always English.
/// UI code should use <see cref="LocalizedMessage"/> for user-facing display.
/// </summary>
public sealed class LocalizedException : Exception
{
    /// <summary>
    /// The localized message intended for user-facing display.
    /// </summary>
    public string LocalizedMessage { get; }

    public LocalizedException(string englishMessage, string localizedMessage)
        : base(englishMessage)
    {
        LocalizedMessage = localizedMessage;
    }

    public LocalizedException(string englishMessage, string localizedMessage, Exception innerException)
        : base(englishMessage, innerException)
    {
        LocalizedMessage = localizedMessage;
    }
}
