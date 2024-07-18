namespace FoliCon.Models.Data;

public class ExtractionProgress(int current, int total)
{
    public int Current { get; set; } = current;
    public int Total { get; } = total;
    
    public double Progress => 100 * ((double)Current / Total);
}