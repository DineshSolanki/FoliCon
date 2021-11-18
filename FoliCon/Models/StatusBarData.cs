namespace FoliCon.Models;

public class StatusBarData : BindableBase
{
    private string _appStatus;
    private string _appStatusAdditional;
    private int _processedFolder;
    private int _processedIcon;
    private int _totalIcons;
    private int _totalFolders;
    private string _netIcon;
    public string AppStatus { get => _appStatus; set => SetProperty(ref _appStatus, value); }
    public string AppStatusAdditional { get => _appStatusAdditional; set => SetProperty(ref _appStatusAdditional, value); }
    public int ProcessedFolder { get => _processedFolder; set => SetProperty(ref _processedFolder, value); }
    public int ProcessedIcon { get => _processedIcon; set => SetProperty(ref _processedIcon, value); }
    public int TotalIcons { get => _totalIcons; set => SetProperty(ref _totalIcons, value); }
    public int TotalFolders { get => _totalFolders; set => SetProperty(ref _totalFolders, value); }
    public string NetIcon { get => _netIcon; set => SetProperty(ref _netIcon, value); }
    public ProgressBarData ProgressBarData { get; set; }

    public StatusBarData()
    {
        ProgressBarData = new ProgressBarData();
    }

    public void ResetData()
    {
        AppStatus = LangProvider.GetLang("Idle");
        AppStatusAdditional = "";
        ProcessedFolder = 0;
        TotalFolders = 0;
        ProcessedIcon = 0;
        TotalIcons = 0;
        ProgressBarData.Value = 0;
        ProgressBarData.Max = 100;
    }
}