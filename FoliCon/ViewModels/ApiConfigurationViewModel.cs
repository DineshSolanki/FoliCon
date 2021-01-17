using FoliCon.Modules;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Globalization;

namespace FoliCon.ViewModels
{
    public class ApiConfigurationViewModel : BindableBase, IDialogAware
    {
        private string _title = "API Configuration";
        private string _dartClient = GlobalDataHelper<AppConfig>.Config.DevClientSecret;
        private string _dartClientId = GlobalDataHelper<AppConfig>.Config.DevClientId;
        private string _tmdbKey = GlobalDataHelper<AppConfig>.Config.TmdbKey;
        private string _igdbClientId = GlobalDataHelper<AppConfig>.Config.IgdbClientId;
        private string _igdbClientSecret = GlobalDataHelper<AppConfig>.Config.IgdbClientSecret;

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