
using FoliCon.Models.Api;
using FoliCon.Models.Data;
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
		CancelCommand = new DelegateCommand(CancelMethod);
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
	private DArtDownloadResponse _dArtDownloadResponse;
	private ProgressInfo _progressInfo = new(0,1,LangProvider.Instance.Downloading);
	private readonly CancellationTokenSource _cts = new();
        
	public string Title { get => _title; set => SetProperty(ref _title, value); }
	public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
	public DArt DArtObject { get => _dArtObject; set => SetProperty(ref _dArtObject, value); }
	public ProgressInfo ProgressInfo { get => _progressInfo; set => SetProperty(ref _progressInfo, value); }
	public DArtDownloadResponse DArtDownloadResponse { get => _dArtDownloadResponse; set => SetProperty(ref _dArtDownloadResponse, value); }
	public ObservableCollection<string> Directory { get => _directory; set => SetProperty(ref _directory, value); }
	
	public DelegateCommand<object> PickCommand { get; set; }
	public DelegateCommand<object> OpenImageCommand { get; set; }
	public DelegateCommand CancelCommand { get; init; }

	private void CancelMethod()
	{
		Logger.Trace("Cancelling Manual Extraction");
		_cts.Cancel();
	}


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

	public virtual async void OnDialogOpened(IDialogParameters parameters)
	{
		parameters.TryGetValue("DeviationId", out string deviationId);
		DArtObject = parameters.GetValue<DArt>("dartobject");
		IsBusy = true;
		try
		{
			DArtDownloadResponse = await Task.Run(() => DArtObject.GetDArtDownloadResponseAsync(deviationId));
			DArtDownloadResponse = await Task.Run(() => DArtObject.ExtractDeviation(deviationId, DArtDownloadResponse,
				_cts.Token, new Progress<ProgressInfo>(value => ProgressInfo = value)));
			Logger.Debug("Downloaded Image from Deviation ID {DeviationId}", deviationId);
		}
		catch (OperationCanceledException e)
		{
			Logger.Debug("User cancelled manual extraction");
			
		}

		var extractedFiles = DArtDownloadResponse.LocalDownloadPath?.ToDirectoryInfo()
			.GetFiles();
		Logger.Trace("Total Files Extracted {TotalFiles}", extractedFiles?.Length);
		if (extractedFiles?.Length > 0)
		{
			var pngOrAvailableFile = extractedFiles
				.GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name))
				.Select(group =>
				{
					var pngFile = group.FirstOrDefault(entry => Path.GetExtension(entry.Name) == ".png");
					return pngFile ?? group.First();
				}).ToList();
			
			Logger.Trace("Total extracted files after filtering {TotalFiles}", pngOrAvailableFile.Count);
			pngOrAvailableFile.ForEach(entry => Directory.AddOnUI(entry.FullName));
		}

		IsBusy = false;
		_cts.Dispose();
	}

	#endregion DialogMethods
}