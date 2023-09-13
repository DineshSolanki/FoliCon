namespace FoliCon.Models;

//[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum FoliconThemes
{
    //[LocalizedDescription("ThemeLight", typeof(Lang))]
    Light,

    //[LocalizedDescription("ThemeDark", typeof(Lang))]
    Dark,

    //[LocalizedDescription("ThemeSystem", typeof(Lang))]
    System
}