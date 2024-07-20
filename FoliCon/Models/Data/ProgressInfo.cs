namespace FoliCon.Models.Data;

public class ProgressInfo : BindableBase
{
    private int _current;
    private int _total;
    private string _text;
    public int Current { get => _current; set => SetProperty(ref _current, value); }
    public int Total { get => _total; set => SetProperty(ref _total, value); }
    public string Text { get => _text; set => SetProperty(ref _text, value); }
    
    public double Progress => 100 * ((double)Current / Total);

    public ProgressInfo(int current, int total)
    {
        Current = current;
        Total = total;
    }
    public ProgressInfo(int current, int total, string text) : this(current, total)
    {
        Text = text;
    }
}