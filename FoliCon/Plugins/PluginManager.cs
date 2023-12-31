
using System.Composition.Hosting;
using System.Runtime.Loader;
using FoliCon.Plugins.Overlay;

namespace FoliCon.Plugins;

public class PluginManager
{
    private const string PluginsPath = @".\Plugins";

    [System.Composition.ImportMany(nameof(OverlayPluginBase))]
    public IEnumerable<OverlayPluginBase> Plugins { get; private set; } = null!;

    public static PluginManager Instance { get; } = new();

    private PluginManager()
    {
    }

    public void Initialize()
    {
        Directory.CreateDirectory(PluginsPath);
        var assemblies = Directory
            .GetFiles(PluginsPath, "*.dll")
            .Select(Path.GetFullPath)
            .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath);

        var configuration = new ContainerConfiguration()
            .WithAssemblies(assemblies);

        using var container = configuration.CreateContainer();
        Plugins = container.GetExports<OverlayPluginBase>();
    }
}