using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FoliCon.Modules
{
    // <summary>
    /// IFileDragDropTarget Interface
    /// </summary>
    public interface IFileDragDropTarget
    {
        void OnFileDrop(string[] filepaths);
    }

    /// <summary>
    /// FileDragDropHelper
    /// </summary>
    public class FolderDragDropHelper
    {
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
                DependencyProperty.RegisterAttached("IsFileDragDropEnabled", typeof(bool), typeof(FolderDragDropHelper), new PropertyMetadata(OnFileDragDropEnabled));

        public static readonly DependencyProperty FileDragDropTargetProperty =
                DependencyProperty.RegisterAttached("FileDragDropTarget", typeof(object), typeof(FolderDragDropHelper), null);

        private static void OnFileDragDropEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;
            Control control = d as Control;
            if (control != null){
                control.Drop += OnDrop;
                control.DragOver += Control_DragOver;
            }
        }

        private static void Control_DragOver(object sender, DragEventArgs e)
        {
            string data = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();

            if (Directory.Exists(data))
            {
                e.Effects = DragDropEffects.Link;
            }
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
       
        private static void OnDrop(object _sender, DragEventArgs _dragEventArgs)
        {
            DependencyObject d = _sender as DependencyObject;
            if (d == null) return;
            object target = d.GetValue(FileDragDropTargetProperty);
            if (target is IFileDragDropTarget fileTarget)
            {
               // if (_dragEventArgs.Data.GetDataPresent(DataFormats.FileDrop))
              //  {
                    
                        fileTarget.OnFileDrop((string[])_dragEventArgs.Data.GetData(DataFormats.FileDrop));
                        
              //  }
            }
            else
            {
                throw new Exception("FileDragDropTarget object must be of type IFileDragDropTarget");
            }
        }
    }
}
