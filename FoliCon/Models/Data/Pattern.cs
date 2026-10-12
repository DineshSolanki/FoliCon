namespace FoliCon.Models.Data;

public class Pattern(string regex, bool isEnabled, bool isReadOnly = false) : BindableBase
{
    private string _regex = regex;
    private bool _isEnabled = isEnabled;
    private bool _isReadOnly = isReadOnly;

    public string Regex { get => _regex; set => SetProperty(ref _regex, value); }
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }
    public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }
}
