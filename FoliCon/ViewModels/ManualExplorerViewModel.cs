
using FoliCon.Modules.DeviantArt;
using NLog;

namespace FoliCon.ViewModels;

public class ManualExplorerViewModel : BindableBase, IDialogAware
{
	private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
	
	public ManualExplorerViewModel()
	{
		Directory = [];
		PickCommand = new DelegateCommand<object>(PickMethod);
		OpenImageCommand = new DelegateCommand<object>(OpenImageMethod);
	}

	private void PickMethod(object localImagePath)
	{
		Logger.Debug("Picking Image {Image}", localImagePath);
		CloseDialog(ButtonResult.OK, (string)localImagePath);
	}

	private string _title = LangProvider.GetLang("ManualExplorer");
	private bool _isBusy;
	private ObservableCollection<string> _directory;
	private DArt _dArtObject;
        
	public string Title { get => _title; set => SetProperty(ref _title, value); }
	public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
	public DArt DArtObject { get => _dArtObject; set => SetProperty(ref _dArtObject, value); }
	public ObservableCollection<string> Directory { get => _directory; set => SetProperty(ref _directory, value); }
	
	public DelegateCommand<object> PickCommand { get; set; }
	public DelegateCommand<object> OpenImageCommand { get; set; }
	
	
	private void OpenImageMethod(object parameter)
	{
		Logger.Debug("Opening Image {Image}", parameter);
		var link = (string)parameter;
		var browser = new ImageBrowser(link)
		{
			ShowTitle = false,
			IsFullScreen = true
		};
		browser.Show();
	}
	
	#region DialogMethods

	public event Action<IDialogResult> RequestClose;

	public virtual bool CanCloseDialog()
	{
		return true;
	}

	public virtual void OnDialogClosed()
	{
		Directory.Clear();
	}
	
	protected virtual void CloseDialog(ButtonResult result, string localPath)
	{
		Logger.Info("Close Dialog called with result {Result}, localImagePath {LocalImagePath}", result, localPath);
		var dialogParams = new DialogParameters
		{
			{"localPath", localPath}
		};

		RaiseRequestClose(new DialogResult(result, dialogParams));
	}

	public virtual void RaiseRequestClose(IDialogResult dialogResult)
	{
		RequestClose?.Invoke(dialogResult);
	}

	public virtual void OnDialogOpened(IDialogParameters parameters)
	{
		parameters.TryGetValue("DeviationId", out string deviationId);
		DArtObject = parameters.GetValue<DArt>("dartobject");
		IsBusy = true;
		DArtObject.Download(deviationId).ContinueWith(task =>
		{
			Logger.Debug("Downloaded Image from Deviation ID {DeviationId}", deviationId);
			var dArtDownloadResponse = task.Result;
			dArtDownloadResponse.LocalDownloadPath.ToDirectoryInfo().GetFiles()
				.ForEach(info => { Directory.AddOnUI(info.FullName); });
			IsBusy = false;
		});
	}

	#endregion DialogMethods
}