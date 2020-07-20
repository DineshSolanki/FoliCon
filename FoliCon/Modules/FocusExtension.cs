using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FoliCon.Modules
{
    /// <summary>
    /// This class registers an attached property that can be set to trigger
    /// changes in focus.  Basically, when the IsFocused property is set
    /// to true, this extension is called in order to set the focus to the
    /// calling control.  In this way, it can be bound to the viewmodel
    /// and focus can be set in the view without having access nor 
    /// knowledge of it.
    /// </summary>
    /// <example>
    /// in viewmodel source file
    /// 
    /// public class ViewModel: INotifyPropertyChanged
    /// {
    ///   public bool IsControlFocused { get; set; } // must implement property changed
    /// }
    /// 
    /// in view xaml file
    /// 
    /// <Button Supporting:FocusExtension.IsFocused="{Binding IsControlFocused}">TestButton</Button>
    /// </example>
    public static class FocusExtension
    {
        public static bool GetIsFocused(DependencyObject depObj)
        {
            return (bool)depObj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject depObj, bool isFocused)
        {
            depObj.SetValue(IsFocusedProperty, isFocused);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusExtension),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged)
                );

        private static void OnIsFocusedPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            var element = (depObj as UIElement);

            if (element != null)
            {
                // Don't care about false values.
                if ((bool)args.NewValue)
                {
                   
                    // only focusable if these two are true
                    // optional to raise exception if they aren't rather than just ignoring.
                    //if (element.Focusable && element.IsVisible)
                    if (element.Focusable)
                    {
                        var action = new Action(() => element.Dispatcher.BeginInvoke((Action)(() => element.Focus())));
                        Task.Factory.StartNew(action);
                        var action2 = new Action(() => element.Dispatcher.BeginInvoke((Action)(() => Keyboard.Focus(element))));
                        Task.Factory.StartNew(action2);
                        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(element), element);
                    }
                }
            }
        }
    }
}
