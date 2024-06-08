namespace FoliCon.Models.Data;

public class Pattern(string regex, bool isEnabled)
{
    public string Regex { get; set; } = regex;
    public bool IsEnabled { get; set; } = isEnabled;
}