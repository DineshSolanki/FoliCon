#nullable enable
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using FoliCon.Modules.UI;
using GongSolutions.Wpf.DragDrop;

namespace FoliconTest;

public class ReOrderDropHandlerTests
{
    private class DynamicProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _values = new();

        public void SetProperty(string name, object? value) => _values[name] = value;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
            {
                return null;
            }

            if (targetMethod.Name.StartsWith("get_"))
            {
                var propName = targetMethod.Name[4..];
                if (_values.TryGetValue(propName, out var val))
                {
                    return val;
                }
                return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
            }
            if (targetMethod.Name.StartsWith("set_"))
            {
                var propName = targetMethod.Name[4..];
                _values[propName] = args?[0];
                return null;
            }
            return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }

    private static (T proxy, DynamicProxy controller) CreateMock<T>() where T : class
    {
        var proxy = DispatchProxy.Create<T, DynamicProxy>();
        var controller = (DynamicProxy)(object)proxy;
        return (proxy, controller);
    }

    [Fact]
    public void CanAcceptDrop_ReturnsFalse_WhenDropInfoOrDragInfoIsNull()
    {
        Assert.False(ReOrderDropHandler.CanAcceptDrop(null));

        var (dropInfo, _) = CreateMock<IDropInfo>();
        Assert.False(ReOrderDropHandler.CanAcceptDrop(dropInfo));
    }

    [Fact]
    public void CanAcceptDrop_ReturnsFalse_WhenDraggingAcrossDifferentCollections()
    {
        var folders = new ObservableCollection<string> { "MovieA", "MovieB" };
        var icons = new ObservableCollection<string> { "IconA.ico", "IconB.ico" };

        var (dragInfo, dragCtrl) = CreateMock<IDragInfo>();
        dragCtrl.SetProperty("SourceCollection", icons);

        var (dropInfo, dropCtrl) = CreateMock<IDropInfo>();
        dropCtrl.SetProperty("DragInfo", dragInfo);
        dropCtrl.SetProperty("TargetCollection", folders);

        Assert.False(ReOrderDropHandler.CanAcceptDrop(dropInfo));
    }

    [Fact]
    public void CanAcceptDrop_ReturnsTrue_WhenDraggingWithinSameCollection()
    {
        var icons = new ObservableCollection<string> { "IconA.ico", "IconB.ico" };

        var (dragInfo, dragCtrl) = CreateMock<IDragInfo>();
        dragCtrl.SetProperty("SourceCollection", icons);

        var (dropInfo, dropCtrl) = CreateMock<IDropInfo>();
        dropCtrl.SetProperty("DragInfo", dragInfo);
        dropCtrl.SetProperty("TargetCollection", icons);

        Assert.True(ReOrderDropHandler.CanAcceptDrop(dropInfo));
    }

    [Fact]
    public void DragOver_SetsEffectsToNone_WhenDraggingAcrossCollections()
    {
        var handler = new ReOrderDropHandler();
        var folders = new ObservableCollection<string> { "MovieA", "MovieB" };
        var icons = new ObservableCollection<string> { "IconA.ico", "IconB.ico" };

        var (dragInfo, dragCtrl) = CreateMock<IDragInfo>();
        dragCtrl.SetProperty("SourceCollection", icons);

        var (dropInfo, dropCtrl) = CreateMock<IDropInfo>();
        dropCtrl.SetProperty("DragInfo", dragInfo);
        dropCtrl.SetProperty("TargetCollection", folders);
        dropCtrl.SetProperty("Effects", DragDropEffects.Move);

        handler.DragOver(dropInfo);

        Assert.Equal(DragDropEffects.None, dropInfo.Effects);
        Assert.Null(dropInfo.DropTargetAdorner);
        Assert.False(dropInfo.NotHandled); // Must NOT delegate to DefaultDropHandler!
    }

    [Fact]
    public void Drop_DoesNotModifyCollections_WhenDraggingAcrossCollections()
    {
        var handler = new ReOrderDropHandler();
        var folders = new ObservableCollection<string> { "MovieA", "MovieB" };
        var icons = new ObservableCollection<string> { "IconA.ico", "IconB.ico" };

        var (dragInfo, dragCtrl) = CreateMock<IDragInfo>();
        dragCtrl.SetProperty("SourceCollection", icons);
        dragCtrl.SetProperty("SourceItem", "IconA.ico");

        var (dropInfo, dropCtrl) = CreateMock<IDropInfo>();
        dropCtrl.SetProperty("DragInfo", dragInfo);
        dropCtrl.SetProperty("TargetCollection", folders);

        handler.Drop(dropInfo);

        // Icons must NOT be removed from icons collection
        Assert.Equal(2, icons.Count);
        Assert.Contains("IconA.ico", icons);

        // Folders must NOT receive the icon
        Assert.Equal(2, folders.Count);
        Assert.DoesNotContain("IconA.ico", folders);
    }
}
