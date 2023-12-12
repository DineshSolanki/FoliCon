using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using FoliCon.Modules.utils;

namespace FoliCon.ViewModels
{
    public class PreviewerViewModel : BindableBase, IDialogAware
    {
        public PreviewerViewModel()
        {
            PosterIconInstance = new FoliCon.Models.Data.PosterIcon
            {
                Rating = Rating
            };

            SelectImageCommand = new DelegateCommand(SelectImage);
        }
        
        private FoliCon.Models.Data.PosterIcon _posterIconInstance;
        private string _rating = "3.5";
        public string Title => "Previewer";
        private string _mediaTitle = "Made with ♥ by FoliCon";
        private bool _ratingVisibility = true;
        private bool _overlayVisibility = true;
        
        public FoliCon.Models.Data.PosterIcon PosterIconInstance
        {
            get => _posterIconInstance;
            set => SetProperty(ref _posterIconInstance, value);
        }
        public string Rating
        {
            get => _rating;
            set
            {
                SetProperty(ref _rating, value);
                PosterIconInstance.Rating = value;
            }
        }

        public string MediaTitle
        {
            get => _mediaTitle;
            set
            {
                SetProperty(ref _mediaTitle, value);
                PosterIconInstance.MediaTitle = value;
            }
        }
        
        public bool RatingVisibility
        {
            get => _ratingVisibility;
            set
            {
                SetProperty(ref _ratingVisibility, value);
                PosterIconInstance.RatingVisibility = BooleanToVisibility(value).ToString();
            }
        }
        
        public bool OverlayVisibility
        {
            get => _overlayVisibility;
            set
            {
                SetProperty(ref _overlayVisibility, value);
                PosterIconInstance.MockupVisibility = BooleanToVisibility(value).ToString();
            }
        }
        
        public event Action<IDialogResult> RequestClose;
        public DelegateCommand SelectImageCommand { get; set; }
        

        private void SelectImage()
        {
            var fileDialog = DialogUtils.NewOpenFileDialog("Select Image", "Image files (*.png, *.jpg, *.gif, *.bmp, *.ico)|*.png;*.jpg;*.gif;*.bmp;*.ico");
            var selected = fileDialog.ShowDialog();

            if (selected != null && (bool)!selected) return;
            var thisMemoryStream = new MemoryStream(File.ReadAllBytes(fileDialog.FileName));
            var rt= new FoliCon.Models.Data.PosterIcon
            {
                FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream)
            };
            PosterIconInstance = rt;
        }

        public bool CanCloseDialog()
        {
           return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            
        }
        
        public Visibility BooleanToVisibility(bool value)
        {
            return value ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
