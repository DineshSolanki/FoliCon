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
		PickSelectedCommand = new DelegateCommand(() => PickMethod(SelectedFile), () => !string.IsNullOrEmpty(SelectedFile));
		PreviewSelectedCommand = new DelegateCommand(() => UiUtils.ShowImageBrowser(SelectedFile), () => !string.IsNullOrEmpty(SelectedFile));
	}

	private void PickMethod(object localImagePath)
	{
		Logger.Debug("Picking Image {Image}", localImagePath);
		CloseDialog(ButtonResult.OK, (string)localImagePath);
	}

	private string _title = Lang.ManualExplorer;
	private bool _isBusy;
	private ObservableCollection<string> _directory;
	private string _selectedFile;
	private DArt _dArtObject;
	private DArtDownloadResponse _dArtDownloadResponse;
	private ProgressBarData _progressInfo = new(0, 1, LangProvider.Instance.Downloading);
	private string _progressStatusText = "Connecting to DeviantArt...";
	private bool _isIndeterminate = true;
	private CancellationTokenSource? _cts;

	public string Title { get => _title; set => SetProperty(ref _title, value); }
	public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
	public string ProgressStatusText { get => _progressStatusText; set => SetProperty(ref _progressStatusText, value); }
	public bool IsIndeterminate { get => _isIndeterminate; set => SetProperty(ref _isIndeterminate, value); }
	public string SelectedFile
	{
		get => _selectedFile;
		set
		{
			if (SetProperty(ref _selectedFile, value))
			{
				PickSelectedCommand?.RaiseCanExecuteChanged();
				PreviewSelectedCommand?.RaiseCanExecuteChanged();
			}
		}
	}
	private DArt DArtObject { get => _dArtObject; set => SetProperty(ref _dArtObject, value); }
	public ProgressBarData ProgressInfo { get => _progressInfo; set => SetProperty(ref _progressInfo, value); }
	private DArtDownloadResponse DArtDownloadResponse { get => _dArtDownloadResponse; set => SetProperty(ref _dArtDownloadResponse, value); }
	public ObservableCollection<string> Directory { get => _directory; set => SetProperty(ref _directory, value); }

	public DelegateCommand<object> PickCommand { get; set; }
	public DelegateCommand<object> OpenImageCommand { get; set; }
	public DelegateCommand PickSelectedCommand { get; set; }
	public DelegateCommand PreviewSelectedCommand { get; set; }
	public DelegateCommand CancelCommand { get; init; }

	private void CancelMethod()
	{
		Logger.Trace("Cancelling Manual Extraction / Dialog");
		if (IsBusy)
		{
			try
			{
				if (_cts is { IsCancellationRequested: false })
				{
					_cts.Cancel();
				}
			}
			catch (ObjectDisposedException) { }
		}
		CloseDialog(ButtonResult.Cancel, null);
	}

	#region DialogMethods

	public DialogCloseListener RequestClose { get; }

	public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed()
    {
        SelectedFile = null;
        Directory.Clear();
        try
        {
            if (_cts is { IsCancellationRequested: false })
            {
                _cts.Cancel();
            }
            _cts?.Dispose();
            _cts = null;
        }
        catch (ObjectDisposedException) { }
    }

    protected virtual void CloseDialog(ButtonResult result, string localPath)
	{
		Logger.Info("Close Dialog called with result {Result}, localImagePath {LocalImagePath}", result, localPath);
		var dialogParams = new DialogParameters
		{
			{"localPath", localPath}
		};

		RequestClose.Invoke(dialogParams, result);
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


		_cts?.Cancel();
		_cts?.Dispose();
		var cts = _cts = new CancellationTokenSource();
		var token = cts.Token;

		IsBusy = true;
		IsIndeterminate = true;
		ProgressStatusText = "Connecting to DeviantArt...";
		try
		{
			DArtDownloadResponse = await Task.Run(() => DArtObject.GetDArtDownloadResponseAsync(deviationId), token);
			Logger.Trace("Deviation ID {DeviationId} Download Response {DArtDownloadResponse}", deviationId, DArtDownloadResponse);
			if (!string.IsNullOrEmpty(DArtDownloadResponse?.FileSizeHumanReadable))
			{
				ProgressStatusText = $"Downloading archive ({DArtDownloadResponse.FileSizeHumanReadable})...";
			}
			else
			{
				ProgressStatusText = "Downloading archive...";
			}

			DArtDownloadResponse = await Task.Run(() => DArtObject.ExtractDeviation(deviationId, DArtDownloadResponse,
				token, new Progress<ProgressBarData>(value =>
				{
					if (!ReferenceEquals(_cts, cts) || token.IsCancellationRequested)
					{
						return;
					}
					ProgressInfo = value;
					if (value.Max > 1)
					{
						IsIndeterminate = false;
						ProgressStatusText = string.Format(CultureInfo.CurrentCulture, "Extracted {0} of {1} icons", value.Value, value.Max);
					}
				})), token);
			Logger.Debug("Downloaded Image from Deviation ID {DeviationId}", deviationId);
		}
		catch (OperationCanceledException e)
		{
			Logger.Debug(e, "User cancelled manual extraction");
			if (ReferenceEquals(_cts, cts))
			{
				IsBusy = false;
				CloseDialog(ButtonResult.Cancel, null);
			}
			return;
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to extract deviation {DeviationId}", deviationId);
			IsBusy = false;
			var isDownloadLimit = ex is LocalizedException { Message: var m } && m.Contains("download limit");
			var message = ex is LocalizedException le ? le.LocalizedMessage : Lang.DAConnectionFailedMessage;
			MessageBox.Show(CustomMessageBox.Error(message, Lang.DAConnectionFailedTitle));

			if (isDownloadLimit)
			{
				var openPetition = MessageBox.Show(CustomMessageBox.Ask(
					Lang.DADownloadLimitPetitionPrompt,
					Lang.DADownloadLimitPetitionTitle));
				if (openPetition == MessageBoxResult.Yes)
				{
					Process.Start(new ProcessStartInfo(DeviantArtAppConfig.downloadLimitPetitionUrl) { UseShellExecute = true });
				}
			}
			CloseDialog(ButtonResult.Cancel, null);
			return;
		}

		if (!ReferenceEquals(_cts, cts) || token.IsCancellationRequested)
		{
			return;
		}

		var extractedFiles = DArtDownloadResponse?.LocalDownloadPath?.ToDirectoryInfo()
			?.GetFiles();
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
			if (SelectedFile == null && pngOrAvailableFile.Count > 0)
			{
				SelectedFile = pngOrAvailableFile[0].FullName;
			}
		}

		if (ReferenceEquals(_cts, cts))
		{
			IsBusy = false;
		}
	}

	#endregion DialogMethods
}
