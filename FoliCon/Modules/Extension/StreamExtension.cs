using FoliCon.Models.Data;
using FoliCon.Modules.utils;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace FoliCon.Modules.Extension;

public static class StreamExtensions
{
    /// <summary>
    /// Extracts PNG and ICO files from a compressed stream and writes them to a directory.
    /// </summary>
    /// <param name="archiveStream">The compressed stream containing the PNG and ICO files.</param>
    /// <param name="targetPath">The path of the directory where the extracted files should be written.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="progressCallback">A Callback which can be used to report the progress of the extraction.</param>
    public static void ExtractPngAndIcoToDirectory(this Stream archiveStream, string targetPath,
        CancellationToken cancellationToken,
        IProgress<ProgressInfo> progressCallback)
    {
        using var reader = ArchiveFactory.Open(archiveStream);
        var pngAndIcoEntries = reader.Entries.Where(entry =>
            (!entry.IsDirectory || !IsUnwantedDirectoryOrFileType(entry)) && FileUtils.IsPngOrIco(entry.Key));
        var pngAndIcoFiles = pngAndIcoEntries as IArchiveEntry[] ?? pngAndIcoEntries.ToArray();
        var totalCount = pngAndIcoFiles.Length;
        var extractionProgress = new ProgressInfo(0, totalCount, "Extracting...");
        progressCallback.Report(extractionProgress);
        var extractionSettings = new ExtractionOptions
        {
            ExtractFullPath = false,
            Overwrite = true
        };
        foreach (var entry in pngAndIcoFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entry.WriteToDirectory(targetPath, extractionSettings);
            extractionProgress.Current++;
            progressCallback.Report(extractionProgress);
        }
    }

    private static bool IsUnwantedDirectoryOrFileType(IEntry entry)
    {
        return entry.Key != null && (entry.IsDirectory ||
                                    entry.Key.Contains("ResourceForks") ||
                                    entry.Key.Contains("__MACOSX") ||
                                    entry.Key.StartsWith("._") ||
                                    entry.Key.Equals(".DS_Store") ||
                                    entry.Key.Equals("Thumbs.db"));
    }
}