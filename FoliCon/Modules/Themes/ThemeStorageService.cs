#nullable enable
using System.IO.Compression;
using System.Security;
using FoliCon.Models.Themes;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Default implementation of <see cref="IThemeStorageService"/> providing theme directory scanning,
/// packaging, zip-slip defense, deletion, and user customization management.
/// </summary>
public class ThemeStorageService : IThemeStorageService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly string _themesDirectory;
    private readonly string _customizationsFilePath;
    private readonly IThemeParser _themeParser;
    private readonly object _lock = new();

    private List<ThemeManifest> _userThemes = [];
    private IReadOnlyList<ThemeManifest>? _allThemesCache;
    private Dictionary<string, ThemeManifest>? _themeByIdCache;
    private Dictionary<string, UserThemeCustomization> _customizations = new(StringComparer.OrdinalIgnoreCase);

    public ThemeStorageService(IThemeParser? themeParser = null, string? themesDirectory = null)
    {
        _themeParser = themeParser ?? new HybridThemeParser();
        _themesDirectory = themesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FoliCon",
            ThemeConstants.themesFolder);

        _customizationsFilePath = Path.Combine(_themesDirectory, "user_customizations.json");

        EnsureDirectoryExists(_themesDirectory);
        if (themesDirectory == null)
        {
            EnsureDefaultCommunityThemesSeeded();
        }
        LoadUserThemes();
        LoadCustomizations();
    }

    public string ThemesDirectory => _themesDirectory;

    public IReadOnlyList<ThemeManifest> GetInstalledThemes()
    {
        lock (_lock)
        {
            if (_allThemesCache != null)
            {
                return _allThemesCache;
            }
            var dict = new Dictionary<string, ThemeManifest>(StringComparer.OrdinalIgnoreCase);

            // 1. Built-in themes (Dark, Light, System, The Butterfly)
            foreach (var b in BuiltInThemes.GetAll())
            {
                dict[b.Id] = b;
            }

            // 2. Pre-installed themes in app directory (<BaseDirectory>/Themes)
            var appThemesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemeConstants.themesFolder);
            if (Directory.Exists(appThemesDir))
            {
                foreach (var dir in Directory.GetDirectories(appThemesDir))
                {
                    var manifestFile = Path.Combine(dir, ThemeConstants.themeJsonFileName);
                    if (!File.Exists(manifestFile)) continue;
                    try
                    {
                        var json = File.ReadAllText(manifestFile);
                        var manifest = _themeParser.Parse(json);
                        manifest.PackageDirectory = dir;
                        manifest.IsBuiltIn = false;
                        dict[manifest.Id] = manifest;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            // 3. Installed user themes from disk (may override pre-installed if customized)
            foreach (var u in _userThemes)
            {
                dict[u.Id] = u;
            }

            _allThemesCache = dict.Values.ToList().AsReadOnly();

            return _allThemesCache;
        }
    }

    public IReadOnlyList<ThemeManifest> GetUserThemes()
    {
        lock (_lock)
        {
            return _userThemes.AsReadOnly();
        }
    }

    public ThemeManifest? GetThemeById(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId)) return null;

        lock (_lock)
        {
            if (_themeByIdCache == null)
            {
                var all = GetInstalledThemes();
                _themeByIdCache = new Dictionary<string, ThemeManifest>(all.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var manifest in all)
                {
                    _themeByIdCache[manifest.Id] = manifest;
                }
            }

            if (_themeByIdCache.TryGetValue(themeId, out var found))
                return found;

            var appThemesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemeConstants.themesFolder, themeId.ToLowerInvariant());
            var manifestFile = Path.Combine(appThemesDir, ThemeConstants.themeJsonFileName);
            if (!File.Exists(manifestFile))
            {
                return null;
            }
            {
                try
                {
                    var json = File.ReadAllText(manifestFile);
                    var manifest = _themeParser.Parse(json);
                    manifest.PackageDirectory = appThemesDir;
                    manifest.IsBuiltIn = false;
                    var previewCandidate = manifest.PreviewImageFile ?? manifest.Preview;
                    if (string.IsNullOrEmpty(previewCandidate))
                    {
                        previewCandidate = HybridThemeParser.ResolveStandardAssetFile(appThemesDir, "preview", "banner", "backdrop");
                    }
                    if (!string.IsNullOrEmpty(previewCandidate))
                    {
                        manifest.PreviewImageFile = previewCandidate;
                    }
                    _themeByIdCache[manifest.Id] = manifest;
                    return manifest;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }
    }

    public bool IsThemeInstalled(string themeId) => GetThemeById(themeId) != null;

    public string GetThemeFolderPath(string themeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeId);
        return Path.Combine(_themesDirectory, themeId.ToLowerInvariant());
    }

    public async Task<ThemeManifest> InstallThemeFromJsonAsync(string json, byte[]? previewImageBytes = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var manifest = _themeParser.Parse(json);
        if (ThemeConstants.BuiltInThemeIds.Contains(manifest.Id))
        {
            throw new InvalidOperationException($"Cannot overwrite built-in theme ID '{manifest.Id}'.");
        }

        var targetFolder = GetThemeFolderPath(manifest.Id);

        EnsureDirectoryExists(targetFolder);

        var manifestPath = Path.Combine(targetFolder, ThemeConstants.themeJsonFileName);
        await File.WriteAllTextAsync(manifestPath, json, ct);

        if (previewImageBytes is { Length: > 0 })
        {
            var previewPath = Path.Combine(targetFolder, ThemeConstants.previewImageFileName);
            await File.WriteAllBytesAsync(previewPath, previewImageBytes, ct);
            manifest.PreviewImageFile = ThemeConstants.previewImageFileName;
        }

        manifest.PackageDirectory = targetFolder;
        manifest.IsBuiltIn = false;

        Refresh();
        Logger.Info("Successfully installed theme '{Id}' ({Name}) from JSON manifest", manifest.Id, manifest.Name);
        return manifest;
    }

    public async Task<ThemeManifest> InstallThemeFromPackageAsync(Stream packageStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packageStream);

        var tempDir = Path.Combine(Path.GetTempPath(), "FoliCon_Theme_Install_" + Guid.NewGuid().ToString("N"));
        EnsureDirectoryExists(tempDir);

        try
        {
            await using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var fullDestDir = Path.GetFullPath(tempDir);

                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name) && (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')))
                    {
                        continue;
                    }

                    var destinationPath = Path.GetFullPath(Path.Combine(tempDir, entry.FullName));
                    if (!destinationPath.StartsWith(fullDestDir, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SecurityException($"Zip Slip vulnerability detected in theme package entry: {entry.FullName}");
                    }

                    var entryDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(entryDir))
                    {
                        EnsureDirectoryExists(entryDir);
                    }

                    await using var entryStream = entry.Open();
                    await using var outputStream = File.Create(destinationPath);
                    await entryStream.CopyToAsync(outputStream, ct);
                }
            }

            var manifestPath = Path.Combine(tempDir, ThemeConstants.themeJsonFileName);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidDataException("Theme package does not contain a 'theme.json' manifest file.");
            }

            var json = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = _themeParser.Parse(json);

            if (ThemeConstants.BuiltInThemeIds.Contains(manifest.Id))
            {
                throw new InvalidOperationException($"Cannot overwrite built-in theme ID '{manifest.Id}'.");
            }

            if (!string.IsNullOrWhiteSpace(manifest.XamlPackageFile))
            {
                var xamlPath = Path.Combine(tempDir, manifest.XamlPackageFile);
                if (!File.Exists(xamlPath))
                {
                    throw new FileNotFoundException($"XAML package file '{manifest.XamlPackageFile}' declared in manifest does not exist in package.");
                }

                _themeParser.LoadPackageResourceDictionary(xamlPath);
            }

            var targetFolder = GetThemeFolderPath(manifest.Id);
            if (Directory.Exists(targetFolder))
            {
                Directory.Delete(targetFolder, recursive: true);
            }
            EnsureDirectoryExists(targetFolder);

            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(tempDir, file);
                var destFile = Path.Combine(targetFolder, relative);
                EnsureDirectoryExists(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, overwrite: true);
            }

            manifest.PackageDirectory = targetFolder;
            manifest.IsBuiltIn = false;

            Refresh();
            Logger.Info("Successfully installed theme package '{Id}' ({Name}) to {Path}", manifest.Id, manifest.Name, targetFolder);
            return manifest;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to clean up temporary install directory {TempDir}", tempDir);
            }
        }
    }

    public async Task<string> ExportThemePackageAsync(string themeId, string destinationZipPath, CancellationToken ct = default)
    {
        var manifest = GetThemeById(themeId)
            ?? throw new DirectoryNotFoundException($"Theme '{themeId}' is not installed.");

        var sourceDir = manifest.PackageDirectory ?? GetThemeFolderPath(themeId);
        if (!Directory.Exists(sourceDir))
        {
            sourceDir = Path.Combine(Path.GetTempPath(), "FoliCon_Export_" + themeId);
            EnsureDirectoryExists(sourceDir);
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(sourceDir, ThemeConstants.themeJsonFileName), json, ct);
        }

        var destDir = Path.GetDirectoryName(destinationZipPath);
        if (!string.IsNullOrEmpty(destDir))
        {
            EnsureDirectoryExists(destDir);
        }

        if (File.Exists(destinationZipPath))
        {
            File.Delete(destinationZipPath);
        }

        ZipFile.CreateFromDirectory(sourceDir, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        Logger.Info("Exported theme '{Id}' to package at {Path}", themeId, destinationZipPath);
        return destinationZipPath;
    }

    public async Task<string> ExportThemeVariantAsync(string baseThemeId, UserThemeCustomization customization, string variantId, string variantName, string destinationZipPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variantName);
        ArgumentNullException.ThrowIfNull(customization);

        var baseManifest = GetThemeById(baseThemeId)
            ?? throw new DirectoryNotFoundException($"Base theme '{baseThemeId}' is not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), "FoliCon_Variant_" + Guid.NewGuid().ToString("N"));
        EnsureDirectoryExists(tempDir);

        try
        {
            // Clone manifest
            var variantManifest = new ThemeManifest
            {
                Id = variantId.ToLowerInvariant().Trim(),
                Name = variantName.Trim(),
                Description = $"Personalized variant based on {baseManifest.Name}.",
                Author = "Custom Variant",
                Version = "1.0.0",
                BaseTheme = baseManifest.BaseTheme,
                Tags = [..baseManifest.Tags, "CustomVariant"],
                Colors = new ThemeColorPalette
                {
                    Background = baseManifest.Colors.Background,
                    CardSurface = baseManifest.Colors.CardSurface,
                    ElevatedSurface = baseManifest.Colors.ElevatedSurface,
                    Primary = !string.IsNullOrEmpty(customization.CustomAccentColor) ? customization.CustomAccentColor : baseManifest.Colors.Primary,
                    PrimaryHover = baseManifest.Colors.PrimaryHover,
                    PrimaryPressed = baseManifest.Colors.PrimaryPressed,
                    Accent = !string.IsNullOrEmpty(customization.CustomAccentColor) ? customization.CustomAccentColor : baseManifest.Colors.Accent,
                    Border = baseManifest.Colors.Border,
                    PrimaryText = baseManifest.Colors.PrimaryText,
                    SecondaryText = baseManifest.Colors.SecondaryText,
                    ThirdText = baseManifest.Colors.ThirdText,
                    Success = baseManifest.Colors.Success,
                    Warning = baseManifest.Colors.Warning,
                    Error = baseManifest.Colors.Error
                },
                Effects = new ThemeEffectsTokens
                {
                    GlowIntensity = (baseManifest.Effects?.GlowIntensity ?? 0.8) * customization.GlowMultiplier,
                    PanelOpacity = customization.PanelOpacity ?? baseManifest.Effects?.PanelOpacity ?? 0.85,
                    BloomRadius = baseManifest.Effects?.BloomRadius ?? 24.0,
                    ShadowDepth = baseManifest.Effects?.ShadowDepth ?? 0.0,
                    VignetteEnabled = baseManifest.Effects?.VignetteEnabled ?? true
                },
                Shapes = baseManifest.Shapes != null ? new ThemeShapesTokens
                {
                    PanelRadius = baseManifest.Shapes.PanelRadius,
                    ButtonRadius = baseManifest.Shapes.ButtonRadius,
                    CardRadius = baseManifest.Shapes.CardRadius
                } : null,
                Assets = new ThemeAssetsTokens
                {
                    BrandingGeometry = baseManifest.Assets?.BrandingGeometry
                }
            };

            // Copy assets or custom wallpaper
            var assetsDir = Path.Combine(tempDir, "Assets");
            EnsureDirectoryExists(assetsDir);

            if (customization.IsWallpaperEnabled && !string.IsNullOrEmpty(customization.CustomWallpaperPath) && File.Exists(customization.CustomWallpaperPath))
            {
                var ext = Path.GetExtension(customization.CustomWallpaperPath);
                var destWallpaper = Path.Combine(assetsDir, "custom_wallpaper" + ext);
                File.Copy(customization.CustomWallpaperPath, destWallpaper, overwrite: true);
                variantManifest.Assets.Wallpaper = "Assets/custom_wallpaper" + ext;
            }
            else if (customization.IsWallpaperEnabled && !string.IsNullOrEmpty(baseManifest.Assets?.Wallpaper))
            {
                variantManifest.Assets.Wallpaper = baseManifest.Assets.Wallpaper;
            }

            var manifestJson = JsonSerializer.Serialize(variantManifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempDir, ThemeConstants.themeJsonFileName), manifestJson, ct);

            var destDir = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                EnsureDirectoryExists(destDir);
            }

            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }

            ZipFile.CreateFromDirectory(tempDir, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            Logger.Info("Successfully exported custom theme variant '{VariantId}' to {Path}", variantId, destinationZipPath);
            return destinationZipPath;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    public UserThemeCustomization? GetUserCustomization(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId)) return null;

        lock (_lock)
        {
            _customizations.TryGetValue(themeId, out var cust);
            return cust;
        }
    }

    public void SaveUserCustomization(UserThemeCustomization customization)
    {
        ArgumentNullException.ThrowIfNull(customization);
        if (string.IsNullOrWhiteSpace(customization.ThemeId)) return;

        lock (_lock)
        {
            _customizations[customization.ThemeId] = customization;
            SaveCustomizationsToDisk();
        }
    }

    public void ClearUserCustomization(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId)) return;

        lock (_lock)
        {
            if (_customizations.Remove(themeId))
            {
                SaveCustomizationsToDisk();
            }
        }
    }

    private void LoadCustomizations()
    {
        lock (_lock)
        {
            if (!File.Exists(_customizationsFilePath)) return;

            try
            {
                var json = File.ReadAllText(_customizationsFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, UserThemeCustomization>>(json);
                if (dict != null)
                {
                    _customizations = new Dictionary<string, UserThemeCustomization>(dict, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load user theme customizations from {Path}", _customizationsFilePath);
            }
        }
    }

    private void SaveCustomizationsToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_customizations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_customizationsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to persist user theme customizations to {Path}", _customizationsFilePath);
        }
    }

    public bool DeleteTheme(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId) || ThemeConstants.BuiltInThemeIds.Contains(themeId))
        {
            return false;
        }

        var folder = GetThemeFolderPath(themeId);
        if (!Directory.Exists(folder))
        {
            return false;
        }

        try
        {
            Directory.Delete(folder, recursive: true);
            Refresh();
            Logger.Info("Deleted user theme '{Id}' from {Folder}", themeId, folder);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete theme directory {Folder}", folder);
            return false;
        }
    }

    public void Refresh()
    {
        lock (_lock)
        {
            _allThemesCache = null;
            _themeByIdCache = null;
            LoadUserThemes();
        }
    }

    private void LoadUserThemes()
    {
        var list = new List<ThemeManifest>();

        if (!Directory.Exists(_themesDirectory))
        {
            _userThemes = list;
            return;
        }

        var directories = Directory.GetDirectories(_themesDirectory);
        foreach (var dir in directories)
        {
            var manifestFile = Path.Combine(dir, ThemeConstants.themeJsonFileName);
            if (!File.Exists(manifestFile)) continue;

            try
            {
                var json = File.ReadAllText(manifestFile);
                var manifest = _themeParser.Parse(json);
                manifest.PackageDirectory = dir;
                manifest.IsBuiltIn = false;

                var previewCandidate = manifest.PreviewImageFile ?? manifest.Preview;
                if (string.IsNullOrEmpty(previewCandidate))
                {
                    previewCandidate = HybridThemeParser.ResolveStandardAssetFile(dir, "preview", "banner", "backdrop");
                }
                if (!string.IsNullOrEmpty(previewCandidate))
                {
                    manifest.PreviewImageFile = previewCandidate;
                }

                list.Add(manifest);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load user theme from '{Dir}'", dir);
            }
        }

        _userThemes = list;
    }

    private void EnsureDefaultCommunityThemesSeeded()
    {
        try
        {
            EnsureDirectoryExists(_themesDirectory);

            var appThemesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemeConstants.themesFolder);
            if (!Directory.Exists(appThemesDir)) return;

            foreach (var sourceThemeDir in Directory.GetDirectories(appThemesDir))
            {
                var themeId = Path.GetFileName(sourceThemeDir);
                var targetThemeDir = GetThemeFolderPath(themeId);

                var sourceManifest = Path.Combine(sourceThemeDir, ThemeConstants.themeJsonFileName);
                var targetManifest = Path.Combine(targetThemeDir, ThemeConstants.themeJsonFileName);

                if (!File.Exists(sourceManifest))
                {
                    continue;
                }
                EnsureDirectoryExists(targetThemeDir);
                foreach (var file in Directory.GetFiles(sourceThemeDir, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(sourceThemeDir, file);
                    var dest = Path.Combine(targetThemeDir, rel);
                    EnsureDirectoryExists(Path.GetDirectoryName(dest)!);
                    if (!File.Exists(dest) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest))
                    {
                        File.Copy(file, dest, overwrite: true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to seed default custom themes from application directory");
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
