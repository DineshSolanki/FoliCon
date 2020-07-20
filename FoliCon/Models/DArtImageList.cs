using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace FoliCon.Models
{
    public class DArtImageList :BindableBase
    {
        private string _url;
        private BitmapSource _image;
        public DArtImageList(string uRL,BitmapSource bmp)
        {
            URL = uRL ?? throw new ArgumentNullException(nameof(uRL));
            Image = bmp ?? throw new ArgumentNullException(nameof(bmp));

        }

        public string URL { get=> _url; set=>SetProperty(ref _url,value); }
        public BitmapSource Image { get=> _image; set=>SetProperty(ref _image,value); }
    }
}
