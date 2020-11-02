using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using Prism.Commands;

namespace FoliCon.ViewModels
{
    public class PosterIconConfigViewModel : BindableBase, IDialogAware
    {
        private string _iconOverlay = "PosterLegacy";
        public DelegateCommand<object> IconOverlayChangedCommand { get; private set; }
        public PosterIconConfigViewModel()
        {
            Services.Tracker.Configure<PosterIconConfigViewModel>()
                .Property(p => p.IconOverlay, defaultValue: "PosterLegacy")
                .PersistOn(nameof(PropertyChanged));
            Services.Tracker.Track(this);
            IconOverlayChangedCommand = new DelegateCommand<object>(delegate(object parameter)
            {
                IconOverlay = (string) parameter;
            });
        }

        public string IconOverlay { get => _iconOverlay;
            set => SetProperty(ref _iconOverlay, value);
        }

        public string Title => "Select Poster Icon overlay";

        #region DialogMethods

        public event Action<IDialogResult> RequestClose;

        protected virtual void CloseDialog(string parameter)
        {
            var result = ButtonResult.None;

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
