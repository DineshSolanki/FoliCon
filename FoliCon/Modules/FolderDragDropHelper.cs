using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules;

public interface IFileDragDropTarget
{
    void OnFileDrop(string[] filePaths, string senderName);
}

public class FolderDragDropHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static bool GetIsFileDragDropEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFileDragDropEnabledProperty);
    }

    public static void SetIsFileDragDropEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFileDragDropEnabledProperty, value);
    }

    public static bool GetFileDragDropTarget(DependencyObject obj)
    {
        return (bool)obj.GetValue(FileDragDropTargetProperty);
    }

    public static void SetFileDragDropTarget(DependencyObject obj, bool value)
    {
        obj.SetValue(FileDragDropTargetProperty, value);
    }

    public static readonly DependencyProperty IsFileDragDropEnabledProperty =
        DependencyProperty.RegisterAttached("IsFileDragDropEnabled", typeof(bool), typeof(FolderDragDropHelper),
            new PropertyMetadata(OnFileDragDropEnabled));

    public static readonly DependencyProperty FileDragDropTargetProperty =
        DependencyProperty.RegisterAttached("FileDragDropTarget", typeof(object), typeof(FolderDragDropHelper),
            null);

    private static void OnFileDragDropEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == e.OldValue)
        {
            Logger.Trace("OnFileDragDropEnabled: New value is same as old value, returning");
            return;
        }
        if (d is not Control control)
        {
            Logger.Trace("OnFileDragDropEnabled: DependencyObject is not a Control, returning");
            return;
        }
        control.Drop += OnDrop;
        control.DragOver += Control_DragOver;
        Logger.Trace("OnFileDragDropEnabled: Added Drop and DragOver event handlers to {ControlName}", control.Name);
    }

    private static void Control_DragOver(object sender, DragEventArgs e)
    {
        Logger.Trace("Control_DragOver: {@DragEventArgs}", e);
        var dt = e.Data.GetData(DataFormats.FileDrop);
        var data = (dt as Array)?.GetValue(0)?.ToString();
        
        Logger.Trace("Control_DragOver: Data: {Data}", data ?? "null");
        
        e.Effects = Directory.Exists(data) ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs dragEventArgs)
    {
        Logger.Trace("OnDrop: {@DragEventArgs}", dragEventArgs);
        if (sender is not DependencyObject d)
        {
            Logger.Trace("OnDrop: Sender is not a DependencyObject, returning");
            return;
        }
        var target = d.GetValue(FileDragDropTargetProperty);
        if (target is IFileDragDropTarget fileTarget)
        {
            fileTarget.OnFileDrop((string[])dragEventArgs.Data.GetData(DataFormats.FileDrop), ((Control)sender).Name);
        }
        else
        {
            Logger.Warn("OnDrop: FileDragDropTarget object is not of type IFileDragDropTarget");
            throw new ArgumentException("FileDragDropTarget object must be of type IFileDragDropTarget");
        }
    }
}