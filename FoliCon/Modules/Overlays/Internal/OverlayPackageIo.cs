#nullable enable
using System.Security.Cryptography;
using FoliCon.Modules.Overlays.Designer;

namespace FoliCon.Modules.Overlays.Internal;

/// <summary>
/// Shared primitives for writing overlay package folders atomically.
///
/// Every writer of overlay folders — the exporter, the draft store, and the repository
/// installer — stages into a sibling temp folder and swaps it into place only when complete,
/// so an interrupted write can never corrupt or half-replace a working package. The helpers
/// here are the single implementation of that pattern; do not copy them into another class.
/// </summary>
public static class OverlayPackageIo
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Deletes a directory tree, treating failure as a warning rather than an error.</summary>
    public static void SafeDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Could not delete {Path}", path);
        }
    }

    /// <summary>
    /// Recreates an empty staging folder, clearing anything a previous crash left behind.
    /// </summary>
    public static void PrepareStagingFolder(string stagingPath)
    {
        SafeDelete(stagingPath);
        Directory.CreateDirectory(stagingPath);
    }

    /// <summary>
    /// Copies only the assets the document actually references, so files the author abandoned
    /// mid-design never ship inside the package.
    /// </summary>
    /// <param name="requireAll">When false (drafts), missing assets are skipped silently; the
    /// validator reports them on export.</param>
    public static void CopyReferencedAssets(OverlayDesignerDocument document, string stagingPath, bool requireAll)
    {
        if (string.IsNullOrWhiteSpace(document.AssetFolderPath))
        {
            return;
        }

        foreach (var asset in document.GetReferencedAssets().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var source = Path.Combine(document.AssetFolderPath, asset);
            if (!File.Exists(source))
            {
                if (requireAll)
                {
                    Logger.Warn("Referenced asset '{Asset}' not found in {Folder}", asset, document.AssetFolderPath);
                }
                continue;
            }

            var target = Path.Combine(stagingPath, asset);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }
    }

    /// <summary>
    /// Moves the staged folder into place, replacing any existing folder atomically: the old
    /// copy is kept aside until the new one is fully moved, so a failure mid-swap does not
    /// destroy a working package.
    /// </summary>
    public static void Commit(string stagingPath, string finalPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        if (Directory.Exists(finalPath))
        {
            var backup = $"{finalPath}.replaced-{Guid.NewGuid():N}";
            Directory.Move(finalPath, backup);

            try
            {
                Directory.Move(stagingPath, finalPath);
            }
            catch
            {
                Directory.Move(backup, finalPath);
                throw;
            }

            SafeDelete(backup);
            return;
        }

        Directory.Move(stagingPath, finalPath);
    }

    /// <summary>
    /// Resolves <paramref name="relativePath"/> against <paramref name="targetDir"/>,
    /// rejecting rooted paths and any path that escapes the target after canonicalization.
    /// The trailing-separator prefix comparison prevents "C:\Overlays\ab" from passing as
    /// inside "C:\Overlays\a".
    /// </summary>
    public static bool TryGetContainedPath(string targetDir, string relativePath, [NotNullWhen(true)] out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        try
        {
            var targetRoot = Path.GetFullPath(targetDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(targetRoot, relativePath));
            if (!candidate.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>SHA256 of a file's contents as lowercase hex.</summary>
    public static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}