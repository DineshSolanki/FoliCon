namespace FoliCon.Models.Data;

public class ExtractionProgress : BindableBase
{
    private int _current;
    private int _total;
    public int Current { get => _current; set => SetProperty(ref _current, value); }
    public int Total { get => _total; set => SetProperty(ref _total, value); }
    
    public double Progress => 100 * ((double)Current / Total);

    public ExtractionProgress(int current, int total)
    {
        Current = current;
        Total = total;
    }
}