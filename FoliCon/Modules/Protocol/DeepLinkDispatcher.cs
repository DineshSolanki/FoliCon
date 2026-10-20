#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using FoliCon.Models.Protocol;
using FoliCon.Modules.Themes;
using Prism.Dialogs;

namespace FoliCon.Modules.Protocol;

/// <summary>
/// Default implementation of <see cref="IDeepLinkDispatcher"/> with strict route validation and SSRF defenses.
/// </summary>
public class DeepLinkDispatcher : IDeepLinkDispatcher
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly Regex RepoRegex = new(
        @"^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SafeIdRegex = new(
        @"^[a-zA-Z0-9_-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public DeepLinkRequest Parse(string uriString)
    {
        var request = new DeepLinkRequest { RawUri = uriString };

        if (string.IsNullOrWhiteSpace(uriString))
        {
            return request;
        }

        Uri uri;
        try
        {
            uri = new Uri(uriString.Trim());
        }
        catch (UriFormatException)
        {
            Logger.Warn("Malformed deeplink URI format: {Uri}", uriString);
            return request;
        }

        if (!string.Equals(uri.Scheme, "folicon", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warn("Rejected non-folicon protocol URI: {Scheme}", uri.Scheme);
            return request;
        }

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.Trim('/').ToLowerInvariant();
        var queryParams = ParseQueryParameters(uri.Query);

        // Security check query params against script/path injection
        foreach (var (k, v) in queryParams)
        {
            if (v.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                v.Contains("file:", StringComparison.OrdinalIgnoreCase) ||
                v.Contains(".."))
            {
                Logger.Warn("Deeplink query parameter '{Key}' contains dangerous payload: {Value}", k, v);
                return request;
            }
        }

        // 1. Theme routes: folicon://theme/install or folicon://theme/preview
        if (host == "theme")
        {
            if (path == "install")
            {
                request.Action = DeepLinkAction.ThemeInstall;
            }
            else if (path == "preview")
            {
                request.Action = DeepLinkAction.ThemePreview;
            }

            if (queryParams.TryGetValue("id", out var themeId) && SafeIdRegex.IsMatch(themeId))
            {
                request.TargetId = themeId;
            }

            if (queryParams.TryGetValue("repo", out var repo) && RepoRegex.IsMatch(repo))
            {
                request.Repository = repo;
            }
        }
        // 2. Overlay routes: folicon://overlay/install
        else if (host == "overlay")
        {
            if (path == "install")
            {
                request.Action = DeepLinkAction.OverlayInstall;
            }

            if (queryParams.TryGetValue("id", out var overlayId) && SafeIdRegex.IsMatch(overlayId))
            {
                request.TargetId = overlayId;
            }

            if (queryParams.TryGetValue("repo", out var repo) && RepoRegex.IsMatch(repo))
            {
                request.Repository = repo;
            }
        }
        // 3. Store routes: folicon://store?tab=themes / ?tab=overlays
        else if (host == "store")
        {
            request.Action = DeepLinkAction.OpenStore;
            if (queryParams.TryGetValue("tab", out var tab))
            {
                request.StoreTab = tab.ToLowerInvariant();
            }
            if (queryParams.TryGetValue("search", out var search))
            {
                request.SearchQuery = search;
            }
        }

        return request;
    }

    public async Task<bool> DispatchAsync(DeepLinkRequest request, IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(dialogService);

        if (!request.IsValid)
        {
            Logger.Warn("Cannot dispatch invalid deeplink request: {Uri}", request.RawUri);
            return false;
        }

        // Ensure execution on UI thread
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            return await Application.Current.Dispatcher.InvokeAsync(() => DispatchAsync(request, dialogService)).Task.Unwrap();
        }

        BringAppToForeground();

        switch (request.Action)
        {
            case DeepLinkAction.ThemeInstall:
            case DeepLinkAction.ThemePreview:
                if (string.IsNullOrWhiteSpace(request.TargetId))
                {
                    Logger.Warn("Theme deeplink missing 'id' parameter.");
                    return false;
                }

                var themeParams = new DialogParameters
                {
                    { "TargetType", "Theme" },
                    { "TargetId", request.TargetId },
                    { "Repository", request.Repository ?? "DineshSolanki/FoliCon-Overlays" },
                    { "IsPreviewOnly", request.Action == DeepLinkAction.ThemePreview }
                };

                dialogService.ShowDialog("DeepLinkInstallDialog", themeParams, _ => { });
                return true;

            case DeepLinkAction.OverlayInstall:
                if (string.IsNullOrWhiteSpace(request.TargetId))
                {
                    Logger.Warn("Overlay deeplink missing 'id' parameter.");
                    return false;
                }

                var overlayParams = new DialogParameters
                {
                    { "TargetType", "Overlay" },
                    { "TargetId", request.TargetId },
                    { "Repository", request.Repository ?? "DineshSolanki/FoliCon-Overlays" },
                    { "IsPreviewOnly", false }
                };

                dialogService.ShowDialog("DeepLinkInstallDialog", overlayParams, _ => { });
                return true;

            case DeepLinkAction.OpenStore:
                var storeParams = new DialogParameters();
                if (!string.IsNullOrEmpty(request.StoreTab))
                {
                    storeParams.Add("tab", request.StoreTab);
                }
                if (!string.IsNullOrEmpty(request.SearchQuery))
                {
                    storeParams.Add("search", request.SearchQuery);
                }

                // Opens Unified Store (or OverlayStore alias during Phase 2-3)
                dialogService.ShowDialog("OverlayStore", storeParams, _ => { });
                return true;

            default:
                return false;
        }
    }

    private static void BringAppToForeground()
    {
        try
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null)
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }

                mainWindow.Activate();
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
                mainWindow.Focus();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to bring MainWindow to foreground during deeplink dispatch.");
        }
    }

    private static Dictionary<string, string> ParseQueryParameters(string query)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return parameters;

        var trimmed = query.TrimStart('?');
        var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(keyValue[0]);
            var value = keyValue.Length > 1 ? Uri.UnescapeDataString(keyValue[1]) : string.Empty;
            parameters[key] = value;
        }

        return parameters;
    }
}
