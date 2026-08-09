#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// Outcome of an export attempt.
/// </summary>
public sealed class OverlayExportResult
{
    private OverlayExportResult(bool succeeded, string? packagePath, OverlayValidationResult validation, string? failureReason)
    {
        Succeeded = succeeded;
        PackagePath = packagePath;
        Validation = validation;
        FailureReason = failureReason;
    }

    [MemberNotNullWhen(true, nameof(PackagePath))]
    public bool Succeeded { get; }

    /// <summary>Folder the finished package was written to.</summary>
    public string? PackagePath { get; }

    /// <summary>Findings from validating the staged package. Warnings may be present on success.</summary>
    public OverlayValidationResult Validation { get; }

    public string? FailureReason { get; }

    public static OverlayExportResult Success(string packagePath, OverlayValidationResult validation) =>
        new(true, packagePath, validation, null);

    public static OverlayExportResult Failure(string reason, OverlayValidationResult? validation = null) =>
        new(false, null, validation ?? new OverlayValidationResult(), reason);
}

/// <summary>
/// Writes a store-ready overlay package.
///
/// Everything is built in a staging folder first and only moved into place once the package is
/// complete and valid, so a failed export can never leave a half-written folder where the
/// author expected a working one. Hashes are computed after every file exists, because hashing
/// as-you-go would miss the preview and the definition.
///
/// Export is deterministic: the same document produces byte-identical <c>overlay.json</c> and a
/// pixel-identical <c>preview.png</c>, so re-exporting an unchanged overlay does not churn the
/// manifest hashes and create noise in a pull request.
/// </summary>
[Localizable(false)] // CanonicalPreviewContext is deliberately English; see its remarks.
public class OverlayExporter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Fixed inputs for the exported preview.
    ///
    /// The author's transient test poster and rating must not leak into the package: two
    /// exports of the same overlay would otherwise differ, breaking hash stability and
    /// making store thumbnails inconsistent between contributors.
    /// </summary>
    public static OverlayPreviewContext CanonicalPreviewContext => new()
    {
        PosterPath = null, // bundled posterDummy.png
        Rating = "8.4",
        MediaTitle = "Made with ♥ by FoliCon",
        ShowRating = true,
        ShowMockup = true
    };

    /// <summary>
    /// Exports <paramref name="document"/> into <c>{destinationRoot}/{id}/</c>.
    /// </summary>
    /// <param name="document">Document to export. Not modified.</param>
    /// <param name="destinationRoot">Parent folder; the package folder is created inside it.</param>
    /// <param name="overwrite">Replace an existing package folder of the same ID.</param>
    public virtual async Task<OverlayExportResult> ExportAsync(
        OverlayDesignerDocument document,
        string destinationRoot,
        bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        if (!Internal.OverlayValidator.IsValidId(document.Id))
        {
            return OverlayExportResult.Failure(Lang.OverlayExportIdRequired);
        }

        var finalPath = Path.Combine(destinationRoot, document.Id);
        if (Directory.Exists(finalPath) && !overwrite)
        {
            return OverlayExportResult.Failure(
                string.Format(Lang.OverlayExportFolderExists, document.Id));
        }

        // Staged as a sibling so the final move is a rename on the same volume.
        var stagingPath = Path.Combine(destinationRoot, $".{document.Id}.export-tmp");

        try
        {
            PrepareStagingFolder(stagingPath);

            CopyReferencedAssets(document, stagingPath);
            await WritePreviewAsync(document, stagingPath);
            WriteDefinition(document, stagingPath);

            // Validate the staged package rather than the in-memory document: this is what
            // actually ships, including the preview and copied assets.
            var validation = Internal.OverlayValidator.ValidateDetailed(stagingPath, BuildExportDefinition(document, stagingPath));
            if (!validation.IsValid)
            {
                SafeDelete(stagingPath);
                return OverlayExportResult.Failure(
                    string.Format(Lang.OverlayExportBlockedByValidation, validation.ErrorCount),
                    validation);
            }

            // Manifest last: hashes and size must cover every file, including the preview.
            WriteManifest(document, stagingPath);

            Commit(stagingPath, finalPath);

            Logger.Info("Exported overlay '{Id}' to {Path}", document.Id, finalPath);
            return OverlayExportResult.Success(finalPath, validation);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Logger.Error(ex, "Failed to export overlay '{Id}'", document.Id);
            SafeDelete(stagingPath);
            return OverlayExportResult.Failure(string.Format(Lang.OverlayExportFailed, ex.Message));
        }
    }

    /// <summary>
    /// Installs a package into the user's overlay folder so the author can use their work
    /// immediately, without waiting on a store submission.
    /// </summary>
    public virtual OverlayExportResult InstallLocally(string packagePath, string? userOverlaysRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!File.Exists(Path.Combine(packagePath, OverlayConstants.overlayJsonFileName)))
        {
            return OverlayExportResult.Failure(string.Format(
                Lang.OverlayExportPackageMissingDefinition, packagePath, OverlayConstants.overlayJsonFileName));
        }

        var id = new DirectoryInfo(packagePath).Name;
        var root = userOverlaysRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FoliCon", OverlayConstants.overlaysFolder);

        if (OverlayConstants.BuiltInOverlayIds.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            return OverlayExportResult.Failure(string.Format(Lang.OverlayExportBuiltInIdReserved, id));
        }

        var target = Path.Combine(root, id);
        var staging = Path.Combine(root, $".{id}.install-tmp");

        try
        {
            Directory.CreateDirectory(root);
            PrepareStagingFolder(staging);

            foreach (var file in Directory.GetFiles(packagePath))
            {
                File.Copy(file, Path.Combine(staging, Path.GetFileName(file)), overwrite: true);
            }

            Commit(staging, target);

            Logger.Info("Installed overlay '{Id}' locally to {Path}", id, target);
            return OverlayExportResult.Success(target, new OverlayValidationResult());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Error(ex, "Failed to install overlay '{Id}' locally", id);
            SafeDelete(staging);
            return OverlayExportResult.Failure(string.Format(Lang.OverlayExportInstallFailed, ex.Message));
        }
    }

    /// <summary>
    /// Deletes a user-installed overlay's folder.
    ///
    /// Built-in IDs are refused: they live inside the assembly, so there is nothing to delete
    /// and a matching folder name would mean something has gone wrong.
    /// </summary>
    public virtual OverlayExportResult UninstallLocal(string overlayId, string? userOverlaysRoot = null)
    {
        if (string.IsNullOrWhiteSpace(overlayId))
        {
            return OverlayExportResult.Failure(Lang.OverlayExportNoOverlaySpecified);
        }

        if (OverlayConstants.BuiltInOverlayIds.Contains(overlayId, StringComparer.OrdinalIgnoreCase))
        {
            return OverlayExportResult.Failure(string.Format(Lang.OverlayExportBuiltInCannotRemove, overlayId));
        }

        var root = userOverlaysRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FoliCon", OverlayConstants.overlaysFolder);
        var target = Path.Combine(root, overlayId);

        if (!Directory.Exists(target))
        {
            return OverlayExportResult.Failure(string.Format(Lang.OverlayExportNotInstalled, overlayId));
        }

        try
        {
            Directory.Delete(target, recursive: true);
            Logger.Info("Removed locally installed overlay '{Id}'", overlayId);
            return OverlayExportResult.Success(target, new OverlayValidationResult());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Error(ex, "Failed to remove overlay '{Id}'", overlayId);
            return OverlayExportResult.Failure(
                string.Format(Lang.OverlayExportRemoveFailed, overlayId, ex.Message));
        }
    }

    private static void PrepareStagingFolder(string stagingPath)
    {
        // A previous crash may have left one behind.
        SafeDelete(stagingPath);
        Directory.CreateDirectory(stagingPath);
    }

    /// <summary>
    /// Copies only the assets the document actually references, so files the author
    /// abandoned mid-design never ship inside the package.
    /// </summary>
    private static void CopyReferencedAssets(OverlayDesignerDocument document, string stagingPath)
    {
        foreach (var asset in document.GetReferencedAssets().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var source = Path.Combine(document.AssetFolderPath, asset);
            if (!File.Exists(source))
            {
                // Left for the validator to report against the offending field.
                Logger.Warn("Referenced asset '{Asset}' not found in {Folder}", asset, document.AssetFolderPath);
                continue;
            }

            var target = Path.Combine(stagingPath, asset);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }
    }

    private static async Task WritePreviewAsync(OverlayDesignerDocument document, string stagingPath)
    {
        // Render against the staging folder so the preview uses the copied assets, not the
        // author's working folder, and matches exactly what a store user will install.
        var definition = BuildExportDefinition(document, stagingPath);

        var image = await OverlayDesignerPreviewRenderer.RenderNowAsync(definition, CanonicalPreviewContext)
            ?? throw new InvalidOperationException(Lang.OverlayExportPreviewRenderFailed);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = File.Create(Path.Combine(stagingPath, OverlayConstants.previewImageFileName));
        encoder.Save(stream);
    }

    private static void WriteDefinition(OverlayDesignerDocument document, string stagingPath)
    {
        var json = OverlayPackageSerializer.SerializeDefinition(BuildExportDefinition(document, stagingPath: null));
        File.WriteAllText(Path.Combine(stagingPath, OverlayConstants.overlayJsonFileName), json);
    }

    private static void WriteManifest(OverlayDesignerDocument document, string stagingPath)
    {
        var files = Directory.GetFiles(stagingPath)
            .Select(Path.GetFileName)
            .OfType<string>()
            // Stable order so the manifest is byte-identical across exports.
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        long totalSize = 0;

        foreach (var file in files)
        {
            var path = Path.Combine(stagingPath, file);
            hashes[file] = ComputeSha256(path);
            totalSize += new FileInfo(path).Length;
        }

        var now = DateTime.UtcNow;
        var manifest = new OverlayManifest
        {
            SchemaVersion = document.SchemaVersion,
            Id = document.Id,
            DisplayName = document.DisplayName,
            Author = document.Author,
            Description = document.Description,
            OverlayVersion = document.OverlayVersion,
            Tags = [.. document.Tags],
            PreviewImage = OverlayConstants.previewImageFileName,
            Assets = files,
            Sha256 = hashes,
            SizeBytes = totalSize,
            // Editing an existing package keeps its original creation date.
            CreatedAt = document.CreatedAt ?? now,
            UpdatedAt = now
        };

        File.WriteAllText(
            Path.Combine(stagingPath, OverlayConstants.manifestFileName),
            OverlayPackageSerializer.SerializeManifest(manifest));
    }

    /// <summary>
    /// Snapshot for export. <paramref name="stagingPath"/> points asset resolution at the staged
    /// copies while rendering and validating; pass null when serializing, so no machine-specific
    /// path is written into the package.
    /// </summary>
    private static PosterOverlayDefinition BuildExportDefinition(OverlayDesignerDocument document, string? stagingPath)
    {
        var definition = document.CreateSnapshot();
        definition.OverlayFolderPath = stagingPath;
        definition.IsBuiltIn = false;
        return definition;
    }

    /// <summary>Moves the staged package into place, replacing any existing folder atomically.</summary>
    private static void Commit(string stagingPath, string finalPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        if (Directory.Exists(finalPath))
        {
            // Keep the old copy until the new one is in place, so a failure mid-swap
            // does not destroy a working package.
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

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void SafeDelete(string path)
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
            Logger.Warn(ex, "Could not clean up {Path}", path);
        }
    }
}
