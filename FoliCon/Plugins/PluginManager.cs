using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using FoliCon.Plugins.Overlay;

namespace FoliCon.Plugins;

public class PluginManager
{
    private const string PluginsPath = @".\Plugins";
    private CompositionContainer _container;
    private DirectoryCatalog _catalog;

    [System.Composition.ImportMany]
    public IEnumerable<OverlayPluginBase> Plugins { get; private set; } = null!;

    public static PluginManager Instance { get; } = new();

    private PluginManager()
    {
    }

    public void Initialize()
    {
        Directory.CreateDirectory(PluginsPath);
        _catalog = new DirectoryCatalog(PluginsPath);
        _container = new CompositionContainer(_catalog);
        _container.ComposeParts(this);
    }
}