#nullable enable
using System;

namespace FoliCon.Models.Protocol;

/// <summary>
/// Actions supported by the universal folicon:// deeplink protocol.
/// </summary>
public enum DeepLinkAction
{
    Unknown = 0,
    ThemeInstall,
    ThemePreview,
    OverlayInstall,
    OpenStore
}

/// <summary>
/// Parsed representation of a verified folicon:// URI request.
/// </summary>
public sealed class DeepLinkRequest
{
    public DeepLinkAction Action { get; set; } = DeepLinkAction.Unknown;

    public string? TargetId { get; set; }

    public string? Repository { get; set; }

    public string? StoreTab { get; set; }

    public string? SearchQuery { get; set; }

    public string RawUri { get; set; } = string.Empty;

    public bool IsValid => Action != DeepLinkAction.Unknown;
}
