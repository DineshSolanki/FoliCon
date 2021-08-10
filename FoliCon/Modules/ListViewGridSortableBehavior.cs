using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

//https://www.codewrecks.com/post/old/2014/04/sorting-a-wpf-listview-in-grid-mode/
//Usage:<GridViewColumn DisplayMemberBinding="{Binding Year}" Width="60">
//    <GridViewColumn.Header>
//    <GridViewColumnHeader modules:ListViewGridSortableBehavior.SortHeaderString="Year">
//    <StackPanel Orientation="Horizontal">
//    <Label Content="↓" Visibility="{Binding Path=SortDownVisibility, RelativeSource={RelativeSource AncestorType={x:Type GridViewColumnHeader}}}"></Label>
//    <Label Content="↑" Visibility="{Binding Path=SortUpVisibility, RelativeSource={RelativeSource AncestorType={x:Type GridViewColumnHeader}}}"></Label>
//    <Label Content="Author"></Label>
//    </StackPanel>
//    </GridViewColumnHeader>
//    </GridViewColumn.Header>
//    </GridViewColumn>

namespace FoliCon.Modules
{
    public class ListViewGridSortableBehavior : Behavior<ListView>
    {
        public static readonly DependencyProperty HeaderStringProperty =
            DependencyProperty.Register(nameof(HeaderString), typeof(object), typeof(GridViewColumnHeader));
        public object HeaderString
        {
            get => GetValue(HeaderStringProperty);
            set => SetValue(HeaderStringProperty, value);
        }
        protected override void OnAttached()
        {
            AssociatedObject?.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(GridHeaderClickEventHandler));
            base.OnAttached();
        }

        private GridViewColumnHeader _lastHeaderClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        void GridHeaderClickEventHandler(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader headerClicked) return;
            if (headerClicked.Role == GridViewColumnHeaderRole.Padding) return;
            var direction = headerClicked != _lastHeaderClicked
                ? ListSortDirection.Ascending
                : _lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            if (_lastHeaderClicked != null) 
            {
                SetSortDownVisibility(_lastHeaderClicked, Visibility.Collapsed);
                SetSortUpVisibility(_lastHeaderClicked, Visibility.Collapsed);
            }
            //string header = headerClicked.Column.Header as string;
            var sortString = GetSortHeaderString(headerClicked);
            if (string.IsNullOrEmpty(sortString)) return;
            Sort(sortString, direction);
            if (direction == ListSortDirection.Ascending)
            {
                SetSortDownVisibility(headerClicked, Visibility.Collapsed);
                SetSortUpVisibility(headerClicked, Visibility.Visible);
            }
            else
            {
                SetSortDownVisibility(headerClicked, Visibility.Visible);
                SetSortUpVisibility(headerClicked, Visibility.Collapsed);
            }
            // Remove arrow from previously sorted header 
            if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
            {
                _lastHeaderClicked.Column.HeaderTemplate = null;
            }
            _lastHeaderClicked = headerClicked;
            _lastDirection = direction;
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            var dataView =
              CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
            dataView.SortDescriptions.Clear();
            var sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
        public static readonly DependencyProperty SortHeaderStringProperty =
           DependencyProperty.RegisterAttached
           (
               "SortHeaderString",
               typeof(string),
               typeof(GridViewColumnHeader)
           );
        public static string GetSortHeaderString(DependencyObject obj)
        {
            return (string)obj.GetValue(SortHeaderStringProperty);
        }
        public static void SetSortHeaderString(DependencyObject obj, string value)
        {
            obj.SetValue(SortHeaderStringProperty, value);
        }
        public static readonly DependencyProperty SortDownVisibilityProperty =
          DependencyProperty.RegisterAttached
          (
              "SortDownVisibility",
              typeof(Visibility),
              typeof(GridViewColumnHeader),
              new UIPropertyMetadata(Visibility.Collapsed)
          );
        public static Visibility GetSortDownVisibility(DependencyObject obj)
        {
            return (Visibility)obj.GetValue(SortDownVisibilityProperty);
        }
        public static void SetSortDownVisibility(DependencyObject obj, Visibility value)
        {
            obj.SetValue(SortDownVisibilityProperty, value);
        }
        public static readonly DependencyProperty SortUpVisibilityProperty =
         DependencyProperty.RegisterAttached
         (
             "SortUpVisibility",
             typeof(Visibility),
             typeof(GridViewColumnHeader),
             new UIPropertyMetadata(Visibility.Collapsed)
         );
        public static Visibility GetSortUpVisibility(DependencyObject obj)
        {
            return (Visibility)obj.GetValue(SortUpVisibilityProperty);
        }
        public static void SetSortUpVisibility(DependencyObject obj, Visibility value)
        {
            obj.SetValue(SortUpVisibilityProperty, value);
        }
    }
}
