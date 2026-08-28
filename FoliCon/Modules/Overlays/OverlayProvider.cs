#nullable enable
namespace FoliCon.Modules.Overlays;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel;

/// <summary>
/// Loads and manages overlay definitions from built-in resources and user-installed folders.
/// </summary>
public class OverlayProvider : IOverlayProvider
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly string _userOverlaysPath;
    private readonly List<PosterOverlayDefinition> _builtInOverlays = [];
    private readonly List<PosterOverlayDefinition> _userOverlays = [];

    public OverlayProvider()
    {
        _userOverlaysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FoliCon", OverlayConstants.overlaysFolder);

        LoadBuiltInOverlays();
        LoadUserOverlays();
    }

    public IReadOnlyList<PosterOverlayDefinition> GetAllOverlays() => _builtInOverlays.Concat(_userOverlays).ToList().AsReadOnly();

    public IReadOnlyList<PosterOverlayDefinition> GetUserOverlays() => _userOverlays.AsReadOnly();

    public PosterOverlayDefinition? GetOverlayById(string id)
    {
        return GetAllOverlays().FirstOrDefault(o =>
            string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public PosterOverlayDefinition ResolveActiveOverlayOrDefault(string? activeOverlayId)
    {
        if (!string.IsNullOrWhiteSpace(activeOverlayId))
        {
            var overlay = GetOverlayById(activeOverlayId);
            if (overlay != null)
            {
                return overlay;
            }

            Logger.Warn("Active overlay '{ActiveId}' not found. Falling back to default.", activeOverlayId);
        }

        var defaultOverlay = GetOverlayById(OverlayConstants.defaultOverlayId);
        if (defaultOverlay != null)
        {
            return defaultOverlay;
        }

        Logger.Error("Default overlay '{DefaultId}' not found. Using first available overlay.", OverlayConstants.defaultOverlayId);
        var all = GetAllOverlays();
        return (all.Count > 0 ? all[0] : null) ?? CreateFallbackDefinition();
    }

    public bool IsOverlayInstalled(string id) => GetOverlayById(id) != null;

    public string GetOverlayFolderPath(string id)
    {
        // Check built-in first. Built-in overlay assets are resolved from the app output directory,
        // not the process working directory, so the result must be fully rooted.
        if (OverlayConstants.BuiltInOverlayIds.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources", "Overlays", id));
        }

        return Path.GetFullPath(Path.Combine(_userOverlaysPath, id));
    }

    public void Refresh() => LoadUserOverlays();

    private void LoadBuiltInOverlays()
    {
        _builtInOverlays.Clear();

        foreach (var id in OverlayConstants.BuiltInOverlayIds)
        {
            try
            {
                // .NET SDK converts hyphens to underscores in embedded resource names
                var resourceName = $"FoliCon.Resources.Overlays.{id.Replace('-', '_')}.{OverlayConstants.overlayJsonFileName}";
                var json = LoadEmbeddedResource(resourceName);
                if (json == null)
                {
                    continue;
                }
                var definition = JsonConvert.DeserializeObject<PosterOverlayDefinition>(json);
                if (definition == null)
                {
                    continue;
                }
                definition.IsBuiltIn = true;
                // Built-in overlays use embedded resource paths for images
                // The DynamicPosterIcon will resolve these via GetResourcePath
                _builtInOverlays.Add(definition);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load built-in overlay '{Id}'", id);
            }
        }

        Logger.Info("Loaded {Count} built-in overlays", _builtInOverlays.Count);
    }

    private void LoadUserOverlays()
    {
        _userOverlays.Clear();

        if (!Directory.Exists(_userOverlaysPath))
        {
            Logger.Debug("User overlays directory does not exist: {Path}", _userOverlaysPath);
            return;
        }

        var overlayFolders = Directory.GetDirectories(_userOverlaysPath);
        foreach (var folder in overlayFolders)
        {
            var jsonPath = Path.Combine(folder, OverlayConstants.overlayJsonFileName);
            if (!File.Exists(jsonPath))
            {
                Logger.Warn("Overlay folder '{Folder}' missing {JsonFile}", folder, OverlayConstants.overlayJsonFileName);
                continue;
            }

            try
            {
                var json = File.ReadAllText(jsonPath);
                var definition = JsonConvert.DeserializeObject<PosterOverlayDefinition>(json);
                if (definition == null)
                {
                    Logger.Warn("Failed to deserialize overlay from '{Path}'", jsonPath);
                    continue;
                }

                // Reject community overlays that try to use built-in IDs
                if (OverlayConstants.BuiltInOverlayIds.Contains(definition.Id))
                {
                    Logger.Warn("Community overlay at '{Path}' uses reserved built-in ID '{Id}'. Skipping.", folder, definition.Id);
                    continue;
                }

                // Schema version check
                if (definition.SchemaVersion > OverlayConstants.appSupportedSchemaVersion)
                {
                    Logger.Warn("Overlay '{Id}' requires schema v{Version}, app supports v{AppVersion}. Skipping.",
                        definition.Id, definition.SchemaVersion, OverlayConstants.appSupportedSchemaVersion);
                    continue;
                }

                // Validate
                var errors = Internal.OverlayValidator.Validate(folder, definition);
                if (errors.Count > 0)
                {
                    Logger.Warn("Overlay '{Id}' failed validation: {Errors}", definition.Id, string.Join("; ", errors.Select(e => e.ToString())));
                    continue;
                }

                _userOverlays.Add(definition);
                definition.OverlayFolderPath = folder;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load overlay from '{Path}'", jsonPath);
            }
        }

        Logger.Info("Loaded {Count} user overlays", _userOverlays.Count);
    }

    private static string? LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static PosterOverlayDefinition CreateFallbackDefinition()
    {
        Logger.Error("No overlays available. Creating minimal fallback definition.");
        return new PosterOverlayDefinition
        {
            Id = "fallback",
            DisplayName = "Fallback",
            IsBuiltIn = true
        };
    }
}
