using FoliCon.Modules;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;

namespace FoliCon.ViewModels
{
    public class ApiConfigurationViewModel : BindableBase, IDialogAware
    {
        private string title = "API Configuration";
        private string dartClient = GlobalDataHelper<AppConfig>.Config.DevClientSecret;
        private string dartClientID = GlobalDataHelper<AppConfig>.Config.DevClientID;
        private string tmdbKey = GlobalDataHelper<AppConfig>.Config.TMDBKey;
        private string igdbKey = GlobalDataHelper<AppConfig>.Config.IGDBKey;

        public string Title { get => title; set => SetProperty(ref title, value); }
        public string DArtClient { get => dartClient; set => SetProperty(ref dartClient, value); }
        public string DArtClientId { get => dartClientID; set => SetProperty(ref dartClientID, value); }
        public string TMDBKey { get => tmdbKey; set => SetProperty(ref tmdbKey, value); }
        public string IGDBKey { get => igdbKey; set => SetProperty(ref igdbKey, value); }
        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand SaveCommand { get; private set; }

        public DelegateCommand<string> CloseDialogCommand =>
             _closeDialogCommand ?? (_closeDialogCommand = new DelegateCommand<string>(CloseDialog));

        public ApiConfigurationViewModel()
        {
            SaveCommand = new DelegateCommand(SaveMethod);
        }

        private void SaveMethod()
        {
            if (string.IsNullOrEmpty(TMDBKey) || string.IsNullOrEmpty(IGDBKey) || string.IsNullOrEmpty(DArtClient) || string.IsNullOrEmpty(DArtClientId))
            {
                MessageBox.Error("All fields are required!", "Invalid Value");
            }
            else
            {
                Util.WriteApiConfiguration(TMDBKey, IGDBKey, DArtClient, DArtClientId);
                MessageBox.Success("API configuration Saved.", "Sucess");
                CloseDialog("true");
            }
        }

        #region DialogMethods

        public event Action<IDialogResult> RequestClose;

        protected virtual void CloseDialog(string parameter)
        {
            ButtonResult result = ButtonResult.None;

            if (parameter?.ToLower() == "true")
                result = ButtonResult.OK;
            else if (parameter?.ToLower() == "false")
                result = ButtonResult.Cancel;

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