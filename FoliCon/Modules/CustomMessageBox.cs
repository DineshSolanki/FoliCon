using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FoliCon.Properties.Langs;
using HandyControl.Data;

namespace FoliCon.Modules
{
    class CustomMessageBox
    {
        public static MessageBoxInfo Ask(string messageBoxText, string caption)
        {
            return new()
            {
                Message = messageBoxText,
                Caption = caption,
                Button = MessageBoxButton.YesNo,
                IconBrushKey = ResourceToken.AccentBrush,
                IconKey = ResourceToken.AskGeometry,
                YesContent = LangProvider.GetLang("Confirm"),
                NoContent = LangProvider.GetLang("Cancel")
            };
        }
        public static MessageBoxInfo Error(string messageBoxText, string caption)
        {
            return new()
            {
                Message = messageBoxText,
                Caption = caption,
                Button = MessageBoxButton.OK,
                IconBrushKey = ResourceToken.AccentBrush,
                IconKey = ResourceToken.ErrorGeometry,
                ConfirmContent = LangProvider.GetLang("OK")
            };
        }
        public static MessageBoxInfo Warning(string messageBoxText, string caption)
        {
            return new()
            {
                Message = messageBoxText,
                Caption = caption,
                Button = MessageBoxButton.OK,
                IconBrushKey = ResourceToken.AccentBrush,
                IconKey = ResourceToken.WarningGeometry,
                ConfirmContent = LangProvider.GetLang("OK")
            };
        }
        public static MessageBoxInfo Info(string messageBoxText, string caption)
        {
            return new()
            {
                Message = messageBoxText,
                Caption = caption,
                Button = MessageBoxButton.OK,
                IconBrushKey = ResourceToken.AccentBrush,
                IconKey = ResourceToken.InfoGeometry,
                ConfirmContent = LangProvider.GetLang("OK")
            };
        }
    }
}
