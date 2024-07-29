namespace FoliCon.Modules.UI;

internal static class CustomMessageBox
{
    public static MessageBoxInfo Ask(string messageBoxText, string caption)
    {
        return new MessageBoxInfo
        {
            Message = messageBoxText,
            Caption = caption,
            Button = MessageBoxButton.YesNo,
            IconBrushKey = ResourceToken.AccentBrush,
            IconKey = ResourceToken.AskGeometry,
            YesContent = Lang.Confirm,
            NoContent = Lang.Cancel
        };
    }
    public static MessageBoxInfo Error(string messageBoxText, string caption)
    {
        return new MessageBoxInfo
        {
            Message = messageBoxText,
            Caption = caption,
            Button = MessageBoxButton.OK,
            IconBrushKey = ResourceToken.AccentBrush,
            IconKey = ResourceToken.ErrorGeometry,
            ConfirmContent = Lang.OK
        };
    }
    public static MessageBoxInfo Warning(string messageBoxText, string caption)
    {
        return new MessageBoxInfo
        {
            Message = messageBoxText,
            Caption = caption,
            Button = MessageBoxButton.OK,
            IconBrushKey = ResourceToken.AccentBrush,
            IconKey = ResourceToken.WarningGeometry,
            ConfirmContent = Lang.OK
        };
    }
    public static MessageBoxInfo Info(string messageBoxText, string caption)
    {
        return new MessageBoxInfo
        {
            Message = messageBoxText,
            Caption = caption,
            Button = MessageBoxButton.OK,
            IconBrushKey = ResourceToken.AccentBrush,
            IconKey = ResourceToken.InfoGeometry,
            ConfirmContent = Lang.OK
        };
    }
}