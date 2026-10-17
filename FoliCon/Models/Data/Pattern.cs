namespace FoliCon.Models.Data;

public class Pattern(string regex, bool isEnabled, bool isReadOnly = false) : BindableBase
{
    public string Regex
    {
        get;
        set => SetProperty(ref field, value);
    } = regex;

    public bool IsEnabled
    {
        get;
        set => SetProperty(ref field, value);
    } = isEnabled;

    public bool IsReadOnly
    {
        get;
        set => SetProperty(ref field, value);
    } = isReadOnly;
}
