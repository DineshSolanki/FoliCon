using Prism.Mvvm;

namespace FoliCon.Models
{
    public class StatusBarData : BindableBase
    {
        private string _appStatus;
        private int _processedFolder;
        private int _processedIcon;
        private int _totalIcons;
        private string _netIcon;
        public string AppStatus { get => _appStatus; set => SetProperty(ref _appStatus, value); }
        public int ProcessedFolder { get => _processedFolder; set => SetProperty(ref _processedFolder, value); }
        public int ProcessedIcon { get => _processedIcon; set => SetProperty(ref _processedIcon, value); }
        public int TotalIcons { get => _totalIcons; set => SetProperty(ref _totalIcons, value); }
        public string NetIcon { get => _netIcon; set => SetProperty(ref _netIcon, value); }
        public ProgressBarData ProgressBarData { get; set; }
        public StatusBarData()
        {
            ProgressBarData = new ProgressBarData();
        }
        public void ResetData()
        {
            AppStatus = "IDLE";
            ProcessedFolder = 0;
            ProcessedIcon = 0;
            TotalIcons = 0;
            ProgressBarData.Value = 0;
            ProgressBarData.Max = 100;
        }

    }
}
