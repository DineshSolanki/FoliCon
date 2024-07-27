namespace FoliCon.Models.Data;

public class PickedListItem: ListItem
{
    private string _folderName;
    
    public string FolderName
    {
        get => _folderName;
        set => SetProperty(ref _folderName, value);
    }

    public PickedListItem(string title, string year, string rating, string folder, string folderName, string poster)
    {
        Title = title;
        Year = year;
        Rating = rating;
        Folder = folder;
        Poster = poster;
        
        FolderName = folderName;
    } 
}