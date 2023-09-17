using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class FileUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Determines whether a given string value ends with any string within a collection of file extensions.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="fileExtensions">An iterable collection of file extensions.</param>
    /// <returns>A boolean indicating whether the string value ends with any of the provided file extensions.
    /// Returns true if the value ends with any of the file extensions; false otherwise.</returns>
    private static bool EndsIn(string value, IEnumerable<string> fileExtensions)
    {
        return fileExtensions.Any(fileExtension => value.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsPngOrIco(string fileName) =>
        fileName != null && EndsIn(fileName, new[] { ".png", ".ico" });

    /// <summary>
    /// Deletes Icons (.ico and Desktop.ini files) from all subfolders of given path.
    /// </summary>
    /// <param name="folderPath">Path to delete Icons from</param>
    public static void DeleteIconsFromSubfolders(string folderPath)
    {
        Logger.Debug("Deleting Icons from Subfolders of: {FolderPath}", folderPath);
        DeleteIconsFromFolder(folderPath);
        foreach (var folder in Directory.EnumerateDirectories(folderPath))
        {
            DeleteIconsFromFolder(folder);
        }

        ProcessUtils.RefreshIconCache();
        Logger.Debug("Icons Deleted from Subfolders of: {FolderPath}", folderPath);
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST | SHCNF.SHCNF_FLUSHNOWAIT, folderPath);
    }

    public static void DeleteIconsFromFolder(string folderPath)
    {
        Logger.Debug("Deleting Icons from: {FolderPath}", folderPath);
        var folderName = Path.GetFileName(folderPath);
        var icoFile = Path.Combine(folderPath, $"{folderName}.ico");
        var iniFile = Path.Combine(folderPath, "desktop.ini");
        try
        {
            if (File.Exists(icoFile))
                File.Delete(icoFile);
            else
                Logger.Debug("ICO File Not Found: {IcoFile}", icoFile);

            if (File.Exists(iniFile))
                File.Delete(iniFile);
            else
                Logger.Debug("INI File Not Found: {IniFile}", iniFile);

        }
        catch (UnauthorizedAccessException e)
        {
            Logger.ForErrorEvent().Message("DeleteIconsFromFolder: UnauthorizedAccessException Occurred. message: {Message}",
                    e.Message)
                .Exception(e).Log();
            Util.HandleUnauthorizedAccessException(e,folderPath);
        }
        Logger.Debug("Icons Deleted from: {FolderPath}", folderPath);
    }

    public static void DeleteMediaInfoFromSubfolders(string folderPath)
    {
        Logger.Debug("Deleting MediaInfo from Subfolders of: {FolderPath}", folderPath);
        var icoFile = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);
        File.Delete(icoFile);
        foreach (var folder in Directory.EnumerateDirectories(folderPath))
        {
            icoFile = Path.Combine(folder, GlobalVariables.MediaInfoFile);
            File.Delete(icoFile);
        }
        Logger.Debug("MediaInfo Deleted from Subfolders of: {FolderPath}", folderPath);
    }

    public static List<string> GetFolderNames(string folderPath)
    {
        var folderNames = new List<string>();
        if (!string.IsNullOrEmpty(folderPath))
        {
            folderNames.AddRange(from folder in Directory.GetDirectories(folderPath)
                where !File.Exists(folder + @"\" + Path.GetFileName(folder) + ".ico")
                select Path.GetFileName(folder));
        }

        return folderNames;
    }

    /// <summary>
    /// Get List of file in given folder.
    /// </summary>
    /// <param name="folder">Folder to Get File from.</param>
    /// <returns>ArrayList with file Names.</returns>
    public static List<string> GetFileNamesFromFolder(string folder)
    {
        Logger.Debug("Getting File Names from Folder: {Folder}", folder);
        var itemList = new List<string>();
        try
        {
            if (string.IsNullOrEmpty(folder)) return itemList;
            itemList.AddRange(Directory.GetFiles(folder).Select(Path.GetFileName));
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("Error Occurred while Getting File Names from Folder: {Folder}", folder)
                .Exception(e).Log();
            itemList.Add($"Error accessing files: {e.Message}");
        }

        return itemList;
    }

    public static void HideFile(string icoFile)
    {
        Logger.Debug("Hiding File: {IcoFile}", icoFile);
        if (!File.Exists(icoFile))
        {
            Logger.ForErrorEvent().Message("File Not Found: {IcoFile}", icoFile).Log();
            return;
        }
        // Set file attribute to "Hidden"
        if ((File.GetAttributes(icoFile) & FileAttributes.Hidden) != FileAttributes.Hidden)
        {
            Logger.Debug("Setting File Attribute to Hidden");
            File.SetAttributes(icoFile, File.GetAttributes(icoFile) | FileAttributes.Hidden);
        }

        // Set file attribute to "System"
        if ((File.GetAttributes(icoFile) & FileAttributes.System) != FileAttributes.System)
        {
            Logger.Debug("Setting File Attribute to System");
            File.SetAttributes(icoFile, File.GetAttributes(icoFile) | FileAttributes.System);
        }
    }

    public static void SaveMediaInfo(int id, string mediaType, string folderPath)
    {
        Logger.Debug("Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
            mediaType, folderPath);
        var filePath = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);
        try
        {
            File.Create(filePath);
            InIHelper.AddValue("ID", id.ToString(CultureInfo.InvariantCulture), null, filePath);
            InIHelper.AddValue("MediaType", mediaType, null, filePath);
            HideFile(filePath);
        }
        catch (UnauthorizedAccessException e)
        {
            Logger.ForExceptionEvent(e, LogLevel.Error).Message("Error Occurred while Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
                mediaType, filePath).Log();
            MessageBox.Show(CustomMessageBox.Error(
                e.Message.Contains("The process cannot access the file")
                    ? LangProvider.GetLang("FileIsInUse")
                    : LangProvider.GetLang("FailedToSaveMediaInfoAt").Format(filePath), LangProvider.GetLang("UnauthorizedAccess")));
        }
        catch (Exception e)
        {
            Logger.ForExceptionEvent(e, LogLevel.Error).Message("Error Occurred while Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
                mediaType, folderPath).Log();
            CustomMessageBox.Error(e.Message, LangProvider.GetLang("ExceptionOccurred"));
        }
    }

    public static (string ID, string MediaType) ReadMediaInfo(string folderPath)
    {
        string id = null;
        string mediaType = null;
        var filePath = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);

        if (File.Exists(filePath))
        {
            id = InIHelper.ReadValue("ID", null, filePath);
            mediaType = InIHelper.ReadValue("MediaType", null, filePath);

            if (string.IsNullOrWhiteSpace(id))
            {
                id = null;
                mediaType = null;
            }
        }

        var mediaInfo = (ID: id, MediaType: mediaType);
        Logger.Debug("Media Info Read: {@MediaInfo}", mediaInfo);

        return mediaInfo;
    }

    public static DirectoryPermissionsResult CheckDirectoryPermissions(string dirPath)
    {
        var result = new DirectoryPermissionsResult();

        if (!Directory.Exists(dirPath)) return result;

        result.CanRead = CheckReadPermissions(dirPath, "test.txt");
        result.CanWrite = CheckWritePermissions(dirPath, "test.tmp");

        Logger.Debug("Path: {Path}; Directory Permissions Checked: {@Result}", dirPath, result);

        return result;
    }

    private static bool CheckReadPermissions(string dirPath, string fileName)
    {
        try
        {
            // Attempt to open the file with read permissions
            using var stream = File.OpenRead(Path.Combine(dirPath, fileName));
            return true;
        }
        catch
        {
            // Ignore any exception, it means we don't have read permissions.
            return false;
        }
    }

    private static bool CheckWritePermissions(string dirPath, string fileName)
    {
        try
        {
            // Attempt to open a new file with write permissions
            using (File.Create(Path.Combine(dirPath, fileName)));
            // Successfully created file, try to delete it
            File.Delete(Path.Combine(dirPath, fileName));
            return true;
        }
        catch
        {
            // Ignore any exception, it means we don't have write permissions.
            return false;
        }
    }

    public static string GetResourcePath(string resource)
    {
        var path = Path.Combine(Path.GetTempPath(), resource);
        Logger.Debug("Getting Resource Path, Resource: {Resource}, Path: {Path}", resource, path);
        if (File.Exists(path)) return path;

        var resourceUri = new Uri($"pack://application:,,,/FoliCon;component/Resources/{resource}");
        var resourceStream = Application.GetResourceStream(resourceUri);

        if (resourceStream == null)
        {
            Logger.Warn("Resource {Resource} cannot be found");
            throw new InvalidOperationException($"Resource {resource} cannot be found.");
        }
        using var input = resourceStream.Stream;
        using Stream output = File.Create(path);
        input.CopyTo(output);
        return path;

    }
}