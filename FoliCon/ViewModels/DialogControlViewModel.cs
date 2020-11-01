﻿using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;

namespace FoliCon.ViewModels
{
    public class DialogControlViewModel : BindableBase, IDialogAware
    {
        private DelegateCommand<string> _closeDialogCommand;

        public DelegateCommand<string> CloseDialogCommand =>
            _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        private string _message;

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private string _title = "Notification";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

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
            Message = parameters.GetValue<string>("message");
            Title = parameters.GetValue<String>("title");
        }
    }
}