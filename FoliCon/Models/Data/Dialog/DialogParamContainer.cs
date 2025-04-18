namespace FoliCon.Models.Data.Dialog;

public class DialogParamContainer
{
    public Tmdb TmdbObject { get; set; }
    public IgdbClass IgdbObject { get; set; }
    public ResultResponse Result { get; set; }
    public bool IsPickedById { get; set; }
    public Action<IDialogResult> CallBack { get; set; } = _ => { };
}