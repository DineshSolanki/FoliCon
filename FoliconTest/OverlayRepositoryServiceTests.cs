using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using Newtonsoft.Json;

namespace FoliconTest;

/// <summary>
/// Unit tests for <see cref="OverlayRepositoryService"/>.
/// Tests file-based operations (install, uninstall, cache) using temp directories.
/// Network calls are not tested here — they require integration tests.
/// </summary>
public class OverlayRepositoryServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _userOverlaysDir;
    private readonly string _cacheDir;
    private readonly OverlayProvider _provider;

    public OverlayRepositoryServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"FoliconRepoTest_{Guid.NewGuid():N}");
        _userOverlaysDir = Path.Combine(_tempRoot, "AppData", "FoliCon", "Overlays");
        _cacheDir = Path.Combine(_tempRoot, "LocalAppData", "FoliCon", "OverlayCache");
        Directory.CreateDirectory(_userOverlaysDir);
        Directory.CreateDirectory(_cacheDir);
        _provider = new OverlayProvider();
    }

    [Fact]
    public void IsOverlayInstalled_NonExistentOverlay_ReturnsFalse()
    {
        var service = CreateService();
        Assert.False(service.IsOverlayInstalled("nonexistent-overlay"));
    }

    [Fact]
    public void IsOverlayInstalled_BuiltInOverlay_ReturnsFalse()
    {
        var service = CreateService();
        // Built-in overlays are not "installed" (user-installed)
        Assert.False(service.IsOverlayInstalled("liaher"));
    }

    [Fact]
    public void IsOverlayInstalled_WithValidFolder_ReturnsTrue()
    {
        // Create a mock installed overlay
        var overlayDir = Path.Combine(_userOverlaysDir, "test-overlay");
        Directory.CreateDirectory(overlayDir);
        File.WriteAllText(Path.Combine(overlayDir, "overlay.json"), """
        {
            "schemaVersion": 1,
            "id": "test-overlay",
            "displayName": "Test",
            "overlayVersion": "1.0.0"
        }
        """);

        var service = CreateService();
        Assert.True(service.IsOverlayInstalled("test-overlay"));
    }

    [Fact]
    public void IsOverlayInstalled_FolderWithoutOverlayJson_ReturnsFalse()
    {
        var overlayDir = Path.Combine(_userOverlaysDir, "incomplete-overlay");
        Directory.CreateDirectory(overlayDir);
        // No overlay.json

        var service = CreateService();
        Assert.False(service.IsOverlayInstalled("incomplete-overlay"));
    }

    [Fact]
    public void GetInstalledVersion_InstalledOverlay_ReturnsVersion()
    {
        var overlayDir = Path.Combine(_userOverlaysDir, "my-overlay");
        Directory.CreateDirectory(overlayDir);
        File.WriteAllText(Path.Combine(overlayDir, "overlay.json"), """
        {
            "schemaVersion": 1,
            "id": "my-overlay",
            "displayName": "My Overlay",
            "overlayVersion": "2.1.0"
        }
        """);

        var service = CreateService();
        Assert.Equal("2.1.0", service.GetInstalledVersion("my-overlay"));
    }

    [Fact]
    public void GetInstalledVersion_NotInstalled_ReturnsNull()
    {
        var service = CreateService();
        Assert.Null(service.GetInstalledVersion("nonexistent"));
    }

    [Fact]
    public void InvalidateCache_ClearsDiskCache()
    {
        // Create a cache file
        var catalogPath = Path.Combine(_cacheDir, "catalog.json");
        File.WriteAllText(catalogPath, """{"schemaVersion":1,"overlays":[]}""");

        var service = CreateService();
        service.InvalidateCache();

        Assert.False(File.Exists(catalogPath));
    }

    [Fact]
    public async Task UninstallOverlay_RemovesDirectory()
    {
        var overlayDir = Path.Combine(_userOverlaysDir, "to-remove");
        Directory.CreateDirectory(overlayDir);
        File.WriteAllText(Path.Combine(overlayDir, "overlay.json"), """
        {"schemaVersion":1,"id":"to-remove","overlayVersion":"1.0.0"}
        """);

        var service = CreateService();
        await service.UninstallOverlayAsync("to-remove");

        Assert.False(Directory.Exists(overlayDir));
    }

    [Fact]
    public async Task UninstallOverlay_RemovesBackupToo()
    {
        var overlayDir = Path.Combine(_userOverlaysDir, "to-remove");
        var backupDir = Path.Combine(_userOverlaysDir, "to-remove_previous");
        Directory.CreateDirectory(overlayDir);
        Directory.CreateDirectory(backupDir);

        var service = CreateService();
        await service.UninstallOverlayAsync("to-remove");

        Assert.False(Directory.Exists(overlayDir));
        Assert.False(Directory.Exists(backupDir));
    }

    [Fact]
    public async Task UninstallOverlay_NonExistent_DoesNotThrow()
    {
        var service = CreateService();
        var exception = await Record.ExceptionAsync(() => service.UninstallOverlayAsync("nonexistent"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task UninstallOverlay_InvalidId_DoesNotDeleteOutsideTheOverlayDirectory()
    {
        var traversalId = Path.Combine("..", "..", "..", "..", "outside");
        var outsideDirectory = Path.GetFullPath(
            Path.Combine(_userOverlaysDir, traversalId));
        Directory.CreateDirectory(outsideDirectory);
        var sentinel = Path.Combine(outsideDirectory, "keep.txt");
        File.WriteAllText(sentinel, "must survive");

        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.UninstallOverlayAsync(traversalId));

        Assert.True(File.Exists(sentinel));
    }

    [Fact]
    public void InvalidOverlayIds_AreNeverConsideredInstalledOrVersioned()
    {
        var service = CreateService();

        Assert.False(service.IsOverlayInstalled("../outside"));
        Assert.Null(service.GetInstalledVersion("../outside"));
    }

    [Fact]
    public void IsUpdateAvailable_NoUpdates_ReturnsFalse()
    {
        var service = CreateService();
        Assert.False(service.IsUpdateAvailable("any-overlay"));
    }

    /// <summary>
    /// Creates a service instance with temp directory paths.
    /// Note: This uses the real OverlayProvider (built-in overlays only)
    /// since we can't easily inject the paths. The service reads user overlays
    /// from the real %AppData% path, but our tests create files in _userOverlaysDir.
    /// These tests verify the logic, not the full path integration.
    /// </summary>
    private OverlayRepositoryService CreateService() => new(_provider, _userOverlaysDir, _cacheDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); } catch { /* best effort */ }
        }
        GC.SuppressFinalize(this);
    }
}
