namespace FoliCon.ViewModels
{
    public class ApiConfigurationViewModel : BindableBase, IDialogAware
    {
        private string _title = "API Configuration";
        private string _dartClient;
        private string _dartClientId;
        private string _tmdbKey;
        private string _igdbClientId;
        private string _igdbClientSecret;

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string DArtClient { get => _dartClient; set => SetProperty(ref _dartClient, value); }
        public string DArtClientId { get => _dartClientId; set => SetProperty(ref _dartClientId, value); }
        public string TmdbKey { get => _tmdbKey; set => SetProperty(ref _tmdbKey, value); }
        public string IgdbClientId { get => _igdbClientId; set => SetProperty(ref _igdbClientId, value); }
        public string IgdbClientSecret { get => _igdbClientSecret; set => SetProperty(ref _igdbClientSecret, value); }
        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand SaveCommand { get; }

        public DelegateCommand<string> CloseDialogCommand =>
             _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        public ApiConfigurationViewModel()
        {
            SaveCommand = new DelegateCommand(SaveMethod);
            Util.ReadApiConfiguration(out _tmdbKey, out _igdbClientId, out _igdbClientSecret, out _dartClient, out _dartClientId);
        }

        private void SaveMethod()
        {
            if (string.IsNullOrEmpty(TmdbKey) || string.IsNullOrEmpty(IgdbClientSecret) || string.IsNullOrEmpty(IgdbClientId) || string.IsNullOrEmpty(DArtClient) || string.IsNullOrEmpty(DArtClientId))
            {
                MessageBox.Error("All fields are required!", "Invalid Value");
            }
            else
            {
                Util.WriteApiConfiguration(TmdbKey, IgdbClientId, IgdbClientSecret, DArtClient, DArtClientId);
                MessageBox.Success("API configuration Saved.", "Success");
                CloseDialog("true");
            }
        }

        #region DialogMethods

        public event Action<IDialogResult> RequestClose;

        protected virtual void CloseDialog(string parameter)
        {
            var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
            {
                "true" => ButtonResult.OK,
                "false" => ButtonResult.Cancel,
                _ => ButtonResult.None
            };

            RaiseRequestClose(new DialogResult(result));
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public virtual bool CanCloseDialog()
        {
            return true;
        }

        public virtual void OnDialogClosed()
        {
        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #endregion DialogMethods
    }
}