using FoliCon.Modules.utils;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FoliCon.Modules.Extension;

public static class StreamExtensions
{
    public static void ExtractPngAndIcoToDirectory(this Stream archiveStream, string targetPath)
    {
        using var reader = ReaderFactory.Open(archiveStream);
        while (reader.MoveToNextEntry())
        {
            var entryKey = reader.Entry.Key;
            if (IsUnwantedDirectoryOrFileType(entryKey, reader))
            {
                continue;
            }

            if (FileUtils.IsPngOrIco(entryKey))
            {
                reader.WriteEntryToDirectory(targetPath, new ExtractionOptions
                {
                    ExtractFullPath = false,
                    Overwrite = true
                });
            }
        }
    }

    private static bool IsUnwantedDirectoryOrFileType(string entryKey, IReader reader)
    {
        return entryKey != null && (reader.Entry.IsDirectory ||
                                    entryKey.Contains("ResourceForks") ||
                                    entryKey.Contains("__MACOSX") ||
                                    entryKey.StartsWith("._") ||
                                    entryKey.Equals(".DS_Store") ||
                                    entryKey.Equals("Thumbs.db"));
    }
}