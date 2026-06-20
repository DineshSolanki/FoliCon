using System.Xml.Linq;

namespace LanguageClassGenerator;

static class Program
{
    static void Main(string[] args)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        var resxPath = args.Length > 0 ? args[0] : Path.Combine(projectRoot, "Properties", "Langs", "Lang.resx");

        if (!File.Exists(resxPath))
        {
            Console.Error.WriteLine($"Error: .resx file not found at '{resxPath}'");
            Console.Error.WriteLine("Usage: LanguageClassGenerator [path-to-Lang.resx]");
            Environment.Exit(1);
        }

        var keys = ParseResxKeys(resxPath);
        Console.WriteLine($@"Found {keys.Count} string resources in '{resxPath}'");

        var outputPath = args.Length > 1 ? args[1] : Path.Combine(projectRoot, "LangProvider1.cs");

        var code = GenerateLangProvider(keys);
        File.WriteAllText(outputPath, code);
        Console.WriteLine($@"Generated '{outputPath}' with {keys.Count} properties");
    }

    static List<string> ParseResxKeys(string resxPath)
    {
        var doc = XDocument.Load(resxPath);
        return doc.Descendants("data")
            .Where(d => d.Attribute("type") == null || d.Attribute("type")?.Value == "System.String")
            .Select(d => d.Attribute("name")?.Value)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList()!;
    }

    static string GenerateLangProvider(List<string> keys)
    {
        var sb = new StringWriter();
        sb.NewLine = "\n";

        sb.WriteLine("using System.ComponentModel;");
        sb.WriteLine("using System.Globalization;");
        sb.WriteLine("using System.Windows;");
        sb.WriteLine("using System.Windows.Data;");
        sb.WriteLine("using HandyControl.Tools;");
        sb.WriteLine();
        sb.WriteLine("namespace HandyControlDemo.Properties.Langs");
        sb.WriteLine("{");
        sb.WriteLine("    public class LangProvider : INotifyPropertyChanged");
        sb.WriteLine("    {");
        sb.WriteLine("        internal static LangProvider Instance => ResourceHelper.GetResource<LangProvider>(\"FoliConLangs\");");
        sb.WriteLine();
        sb.WriteLine("        private static string _cultureInfoStr;");
        sb.WriteLine();
        sb.WriteLine("        public static CultureInfo Culture");
        sb.WriteLine("        {");
        sb.WriteLine("            get => Lang.Culture;");
        sb.WriteLine("            set");
        sb.WriteLine("            {");
        sb.WriteLine("                if (value == null) return;");
        sb.WriteLine("                if (Equals(_cultureInfoStr, value.EnglishName)) return;");
        sb.WriteLine("                Lang.Culture = value;");
        sb.WriteLine("                _cultureInfoStr = value.EnglishName;");
        sb.WriteLine();
        sb.WriteLine("                Instance.UpdateLangs();");
        sb.WriteLine("            }");
        sb.WriteLine("        }");
        sb.WriteLine();
        sb.WriteLine("        public static string GetLang(string key) => Lang.ResourceManager.GetString(key, Culture);");
        sb.WriteLine();
        sb.WriteLine("        public static void SetLang(DependencyObject dependencyObject, DependencyProperty dependencyProperty, string key)");
        sb.WriteLine("        {");
        sb.WriteLine("            BindingOperations.SetBinding(dependencyObject, dependencyProperty, new Binding(key)");
        sb.WriteLine("            {");
        sb.WriteLine("                Source = Instance,");
        sb.WriteLine("                Mode = BindingMode.OneWay");
        sb.WriteLine("            });");
        sb.WriteLine("        }");
        sb.WriteLine();
        sb.WriteLine("        private void UpdateLangs()");
        sb.WriteLine("        {");

        foreach (var key in keys)
        {
            sb.WriteLine($"            OnPropertyChanged(nameof({key}));");
        }

        sb.WriteLine("        }");
        sb.WriteLine();

        foreach (var key in keys)
        {
            sb.WriteLine($"        public string {key} => Lang.{key};");
            sb.WriteLine();
        }

        sb.WriteLine("        public event PropertyChangedEventHandler PropertyChanged;");
        sb.WriteLine();
        sb.WriteLine("        protected virtual void OnPropertyChanged(string propertyName) =>");
        sb.WriteLine("            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
        sb.WriteLine("    }");
        sb.WriteLine();
        sb.WriteLine("    public class LangKeys");
        sb.WriteLine("    {");

        foreach (var key in keys)
        {
            sb.WriteLine($"        public static readonly string {key} = nameof({key});");
            sb.WriteLine();
        }

        sb.WriteLine("    }");
        sb.WriteLine("}");

        return sb.ToString();
    }
}
