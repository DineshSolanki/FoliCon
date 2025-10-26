namespace FoliCon.ViewModels;

[Localizable(false)]
public class ManualExplorerViewModel : BindableBase, IDialogAware
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	
	public ManualExplorerViewModel()
	{
		Directory = [];
		PickCommand = new DelegateCommand<object>(PickMethod);
		OpenImageCommand = new DelegateCommand<object>(link=> UiUtils.ShowImageBrowser(link as string));
		CancelCommand = new DelegateCommand(CancelMethod);
	}

	private void PickMethod(object localImagePath)
	{
		Logger.Debug("Picking Image {Image}", localImagePath);
		CloseDialog(ButtonResult.OK, (string)localImagePath);
	}

	private string _title = Lang.ManualExplorer;
	private bool _isBusy;
	private ObservableCollection<string> _directory;
	private DArt _dArtObject;
	private DArtDownloadResponse _dArtDownloadResponse;
	private ProgressBarData _progressInfo = new(0,1,LangProvider.Instance.Downloading);
	private readonly CancellationTokenSource _cts = new();
        
	public string Title { get => _title; set => SetProperty(ref _title, value); }
	public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
	private DArt DArtObject { get => _dArtObject; set => SetProperty(ref _dArtObject, value); }
	public ProgressBarData ProgressInfo { get => _progressInfo; set => SetProperty(ref _progressInfo, value); }
	private DArtDownloadResponse DArtDownloadResponse { get => _dArtDownloadResponse; set => SetProperty(ref _dArtDownloadResponse, value); }
	public ObservableCollection<string> Directory { get => _directory; set => SetProperty(ref _directory, value); }
	
	public DelegateCommand<object> PickCommand { get; set; }
	public DelegateCommand<object> OpenImageCommand { get; set; }
	public DelegateCommand CancelCommand { get; init; }

	private void CancelMethod()
	{
		Logger.Trace("Cancelling Manual Extraction");
		_cts.Cancel();
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

	protected virtual void RaiseRequestClose(IDialogResult dialogResult)
	{
		RequestClose?.Invoke(dialogResult);
	}

	public virtual async void OnDialogOpened(IDialogParameters parameters)
	{
		parameters.TryGetValue("DeviationId", out string deviationId);
		DArtObject = parameters.GetValue<DArt>("dartobject");
		
		if (DArtObject == null)
		{
			Logger.Error("DeviantArt client is not available. Cannot extract deviation.");
			MessageBox.Show(CustomMessageBox.Error(Lang.DAUnavailableCannotExtractMessage, Lang.DAUnavailableTitle));
			CloseDialog(ButtonResult.Cancel, null);
			return;
		}
		
		IsBusy = true;
		try
		{
			DArtDownloadResponse = await Task.Run(() => DArtObject.GetDArtDownloadResponseAsync(deviationId));
			Logger.Trace("Deviation ID {DeviationId} Download Response {DArtDownloadResponse}", deviationId, DArtDownloadResponse);
			DArtDownloadResponse = await Task.Run(() => DArtObject.ExtractDeviation(deviationId, DArtDownloadResponse,
				_cts.Token, new Progress<ProgressBarData>(value => ProgressInfo = value)));
			Logger.Debug("Downloaded Image from Deviation ID {DeviationId}", deviationId);
		}
		catch (OperationCanceledException e)
		{
			Logger.Debug(e, "User cancelled manual extraction");
			
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