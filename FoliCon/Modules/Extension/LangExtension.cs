namespace FoliCon.Modules.Extension;

public class LangExtension : HandyControl.Tools.Extension.LangExtension
{
    public LangExtension()
    {
        Source = LangProvider.Instance;
    }
}