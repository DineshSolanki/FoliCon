namespace FoliCon.Models.Data.Dialog;

public class PosterPickerDialogParams : DialogParamContainer
{
    public int PickedIndex { get; set; }
    public ObservableCollection<ListItem> ResultData { get; set; }
}