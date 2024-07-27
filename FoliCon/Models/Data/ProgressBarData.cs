namespace FoliCon.Models.Data;

public class ProgressBarData : BindableBase
{
    private int _value;
    private int _max;
    private string _text;
    public int Value { get => _value; set => SetProperty(ref _value, value); }
    public int Max { get => _max; set => SetProperty(ref _max, value); }
    public string Text { get => _text; set => SetProperty(ref _text, value); }
    
    public double Progress => 100 * ((double)Value / Max);

    public ProgressBarData()
    {
    }

    public ProgressBarData(int value, int max)
    {
        Value = value;
        Max = max;
    }
    
    public ProgressBarData(int value, int max, string text) : this(value, max)
    {
        Text = text;
    }
}