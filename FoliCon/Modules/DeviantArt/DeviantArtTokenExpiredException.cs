namespace FoliCon.Modules.DeviantArt;

/// <summary>
/// Internal exception used to signal that a DeviantArt access token expired during a request
/// and has been refreshed. Used for control flow within the retry pipeline — not a user-facing error.
/// </summary>
internal sealed class DeviantArtTokenExpiredException() : Exception("DeviantArt token expired and was refreshed");