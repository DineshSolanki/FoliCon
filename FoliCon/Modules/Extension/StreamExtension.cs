using System.Text;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FoliCon.Modules.Extension;

public static class StreamExtensions
{
    private static readonly ReaderOptions ReaderOptions = new() { ArchiveEncoding = 
        new ArchiveEncoding(Encoding.GetEncoding("IBM437"), Encoding.GetEncoding("IBM437")) };
    
    private static readonly ExtractionOptions ExtractionSettings = new()
    {
        ExtractFullPath = false,
        Overwrite = true
    };
    
    /// <summary>
    /// Extracts PNG and ICO files from a compressed stream and writes them to a directory.
    /// </summary>
    /// <param name="archiveStream">The compressed stream containing the PNG and ICO files.</param>
    /// <param name="targetPath">The path of the directory where the extracted files should be written.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="progressCallback">A Callback which can be used to report the progress of the extraction.</param>
    public static void ExtractPngAndIcoToDirectory(this Stream archiveStream, string targetPath,
        CancellationToken cancellationToken,
        IProgress<ProgressBarData> progressCallback)
    {
        using var reader = ArchiveFactory.Open(archiveStream, ReaderOptions);
        var pngAndIcoEntries = reader.Entries.Where(IsValidFile).ToList();
        
        
        var totalCount = pngAndIcoEntries.Count;
        var extractionProgress = new ProgressBarData(0, totalCount, LangProvider.Instance.Extracting);
        progressCallback.Report(extractionProgress);
        foreach (var entry in pngAndIcoEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entry.WriteToDirectory(targetPath, ExtractionSettings);
            extractionProgress.Value++;
            progressCallback.Report(extractionProgress);
        }
    }
    
    private static bool IsValidFile(IEntry entry)
    {
        return !entry.IsDirectory && !FileUtils.IsExcludedFileIdentifier(entry.Key) && FileUtils.IsPngOrIco(entry.Key);
    }
}