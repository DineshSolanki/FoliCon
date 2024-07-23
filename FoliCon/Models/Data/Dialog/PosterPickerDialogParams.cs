using FoliCon.Models.Data.Dialog;

namespace FoliCon.Models.Data;

public class PosterPickerDialogParams : DialogParamContainer
{
    public int PickedIndex { get; set; }
    public ObservableCollection<ListItem> ResultData { get; set; }
}