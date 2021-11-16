namespace FoliCon.Modules.LangExtension
{
    public class LangExtension : HandyControl.Tools.Extension.LangExtension
    {
        public LangExtension()
        {
            Source = LangProvider.Instance;
        }
    }
}
