#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Themes;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Default implementation of <see cref="IThemeStorageService"/> providing theme directory scanning,
/// packaging, zip-slip defense, and deletion.
/// </summary>
public class ThemeStorageService : IThemeStorageService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly string _themesDirectory;
    private readonly IThemeParser _themeParser;
    private readonly object _lock = new();

    private List<ThemeManifest> _userThemes = [];
    private IReadOnlyList<ThemeManifest>? _allThemesCache;
    private Dictionary<string, ThemeManifest>? _themeByIdCache;

    public ThemeStorageService(IThemeParser? themeParser = null, string? themesDirectory = null)
    {
        _themeParser = themeParser ?? new HybridThemeParser();
        _themesDirectory = themesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FoliCon",
            ThemeConstants.themesFolder);

        EnsureDirectoryExists(_themesDirectory);
        LoadUserThemes();
    }

    public string ThemesDirectory => _themesDirectory;

    public IReadOnlyList<ThemeManifest> GetInstalledThemes()
    {
        lock (_lock)
        {
            if (_allThemesCache == null)
            {
                var builtIn = BuiltInThemes.GetAll();
                var combined = new List<ThemeManifest>(builtIn.Count + _userThemes.Count);
                combined.AddRange(builtIn);
                combined.AddRange(_userThemes);
                _allThemesCache = combined.AsReadOnly();
            }

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
            _themeByIdCache ??= GetInstalledThemes().ToDictionary(t => t.Id, t => t, StringComparer.OrdinalIgnoreCase);
            return _themeByIdCache.GetValueOrDefault(themeId);
        }
    }

    public bool IsThemeInstalled(string themeId) => GetThemeById(themeId) != null;

    public string GetThemeFolderPath(string themeId) => Path.Combine(_themesDirectory, themeId);

    public async Task<ThemeManifest> InstallThemeFromJsonAsync(
        string json,
        byte[]? previewImageBytes = null,
        CancellationToken ct = default)
    {
        var manifest = _themeParser.Parse(json);

        if (ThemeConstants.BuiltInThemeIds.Contains(manifest.Id))
        {
            throw new InvalidOperationException($"Cannot overwrite built-in theme '{manifest.Id}'.");
        }

        var targetFolder = GetThemeFolderPath(manifest.Id);
        EnsureDirectoryExists(targetFolder);

        var manifestPath = Path.Combine(targetFolder, ThemeConstants.themeJsonFileName);
        var formattedJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(manifestPath, formattedJson, ct);

        if (previewImageBytes is { Length: > 0 })
        {
            var previewPath = Path.Combine(targetFolder, ThemeConstants.previewImageFileName);
            await File.WriteAllBytesAsync(previewPath, previewImageBytes, ct);
            manifest.PreviewImageFile = ThemeConstants.previewImageFileName;
        }

        manifest.PackageDirectory = targetFolder;
        manifest.IsBuiltIn = false;

        Refresh();
        Logger.Info("Installed community theme '{Id}' ({Name}) from JSON manifest", manifest.Id, manifest.Name);
        return manifest;
    }

    public async Task<ThemeManifest> InstallThemeFromPackageAsync(Stream packageStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packageStream);

        var tempDir = Path.Combine(Path.GetTempPath(), "FoliCon_Theme_Install_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var normalizedTempDir = Path.GetFullPath(tempDir) + Path.DirectorySeparatorChar;

            using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                long totalBytes = 0;

                foreach (var entry in archive.Entries)
                {
                    ct.ThrowIfCancellationRequested();

                    totalBytes += entry.Length;
                    if (totalBytes > ThemeConstants.maxThemePackageSizeBytes)
                    {
                        throw new InvalidOperationException(
                            $"Theme package exceeds maximum uncompressed size limit of {ThemeConstants.maxThemePackageSizeBytes / (1024 * 1024)} MB.");
                    }

                    var destinationPath = Path.GetFullPath(Path.Combine(tempDir, entry.FullName));

                    // Zip-Slip Boundary Defense
                    if (!destinationPath.StartsWith(normalizedTempDir, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SecurityException(
                            $"Malicious zip entry '{entry.FullName}' attempts to escape target directory.");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // Directory entry
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    var entryDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(entryDir))
                    {
                        Directory.CreateDirectory(entryDir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            // Verify theme.json presence and validity
            var manifestPath = Path.Combine(tempDir, ThemeConstants.themeJsonFileName);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidDataException("Theme package is missing required 'theme.json' manifest.");
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = _themeParser.Parse(manifestJson);

            if (ThemeConstants.BuiltInThemeIds.Contains(manifest.Id))
            {
                throw new InvalidOperationException($"Cannot overwrite built-in theme '{manifest.Id}'.");
            }

            // Verify custom XAML if present
            var xamlFileName = manifest.XamlPackageFile ?? (File.Exists(Path.Combine(tempDir, ThemeConstants.themeXamlFileName)) ? ThemeConstants.themeXamlFileName : null);
            if (!string.IsNullOrEmpty(xamlFileName))
            {
                var xamlPath = Path.Combine(tempDir, xamlFileName);
                if (File.Exists(xamlPath))
                {
                    _themeParser.LoadPackageResourceDictionary(xamlPath);
                    manifest.XamlPackageFile = xamlFileName;
                }
            }

            // Atomic Move to Final Location
            var finalTargetDir = GetThemeFolderPath(manifest.Id);
            if (Directory.Exists(finalTargetDir))
            {
                Directory.Delete(finalTargetDir, recursive: true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalTargetDir)!);
            Directory.Move(tempDir, finalTargetDir);

            manifest.PackageDirectory = finalTargetDir;
            manifest.IsBuiltIn = false;

            Refresh();
            Logger.Info("Installed community theme '{Id}' ({Name}) from package", manifest.Id, manifest.Name);
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
            // If it's a built-in theme without a directory, generate a standalone directory
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

                var previewPath = Path.Combine(dir, ThemeConstants.previewImageFileName);
                if (File.Exists(previewPath))
                {
                    manifest.PreviewImageFile = ThemeConstants.previewImageFileName;
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

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
