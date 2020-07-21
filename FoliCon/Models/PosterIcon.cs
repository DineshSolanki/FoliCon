using FoliCon.Modules;
using System;
using System.IO;
using System.Windows.Media;

namespace FoliCon.Models
{
    public class PosterIcon
    {
        public ImageSource FolderJpg { get; set; }
        public string RatingVisibilty { get; set; }
        public string Rating { get; set; }
        public string MockupVisibility { get; set; }

        public PosterIcon()
        {
            string filePath = Path.GetTempPath() + "\\posterDummy.png";
            if (!File.Exists(filePath))
            {
                _ = Util.DownloadImageFromUrlAsync(new Uri("https://image.tmdb.org/t/p/original/r0bgHi3MwGHTKPWyJdORsb4ukY8.jpg"), filePath);
            }
            MemoryStream thisMemoryStream = new MemoryStream(File.ReadAllBytes(filePath));
            FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream);
            Rating = "7.8";
            MockupVisibility = "visible";
            RatingVisibilty = "visible";
        }

        public PosterIcon(string folderJpgPath, string rating, string ratingVisibilty, string mockupVisibility)
        {
            RatingVisibilty = ratingVisibilty;
            Rating = rating;
            MockupVisibility = mockupVisibility;
            MemoryStream thisMemoryStream = new MemoryStream(File.ReadAllBytes(folderJpgPath));
            FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream);
            //byte[] bytes = Util.ImageSourceToBytes(new PngBitmapEncoder(), FolderJpg);
            //string base64string = System.Convert.ToBase64String(bytes);
        }
    }
}