namespace FoliCon.Models.Data;

public class Pattern(string regex, bool isEnabled, bool isReadOnly = false)
{
    public string Regex { get; set; } = regex;
    public bool IsEnabled { get; set; } = isEnabled;
    
    public bool IsReadOnly { get; set; } = isReadOnly;
}