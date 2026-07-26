#nullable enable
using FoliCon.Modules.Overlays.Designer;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Cursors = System.Windows.Input.Cursors;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for OverlayDesigner.xaml.
///
/// Owns the direct-manipulation layer: a selection outline plus eight resize handles drawn
/// over the rendered preview. Pointer input is translated from zoomed canvas space into
/// design-surface coordinates and pushed to the ViewModel, which owns all document state.
/// A drag is applied live but recorded as a single undo entry on release.
/// </summary>
public partial class OverlayDesigner
{
    private const double handleSize = 8;

    /// <summary>Which part of the selection a drag is manipulating.</summary>
    private enum DragMode
    {
        None,
        Move,
        ResizeLeft,
        ResizeRight,
        ResizeTop,
        ResizeBottom,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight
    }

    private readonly Rectangle _selectionOutline;
    private readonly List<Rectangle> _handles = [];

    private DragMode _dragMode = DragMode.None;
    private Point _dragStartDesignPoint;
    private Rect _dragStartBounds;

    private OverlayDesignerViewModel? ViewModel => DataContext as OverlayDesignerViewModel;

    public OverlayDesigner()
    {
        InitializeComponent();

        _selectionOutline = new Rectangle
        {
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 1.5,
            StrokeDashArray = [4, 3],
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };

        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!AdornerLayer.Children.Contains(_selectionOutline))
        {
            AdornerLayer.Children.Add(_selectionOutline);
            CreateHandles();
        }

        CanvasRoot.MouseLeftButtonDown += OnCanvasMouseDown;
        CanvasRoot.MouseMove += OnCanvasMouseMove;
        CanvasRoot.MouseLeftButtonUp += OnCanvasMouseUp;
        CanvasRoot.MouseLeave += OnCanvasMouseLeave;

        // Arrow-key nudge is bound at the dialog level, so focus must land here.
        Focus();
        UpdateAdorner();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is OverlayDesignerViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is OverlayDesignerViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        UpdateAdorner();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // An empty name means "everything changed" (a load or undo), so refresh unconditionally.
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName is nameof(OverlayDesignerViewModel.SelectedElement)
                or nameof(OverlayDesignerViewModel.SelectedLeft)
                or nameof(OverlayDesignerViewModel.SelectedTop)
                or nameof(OverlayDesignerViewModel.SelectedWidth)
                or nameof(OverlayDesignerViewModel.SelectedHeight)
                or nameof(OverlayDesignerViewModel.Zoom)
                or nameof(OverlayDesignerViewModel.CanvasWidth))
        {
            Dispatcher.BeginInvoke(UpdateAdorner, DispatcherPriority.Render);
        }
    }

    #region Adorner

    private void CreateHandles()
    {
        foreach (var mode in new[]
                 {
                     DragMode.ResizeTopLeft, DragMode.ResizeTop, DragMode.ResizeTopRight,
                     DragMode.ResizeLeft, DragMode.ResizeRight,
                     DragMode.ResizeBottomLeft, DragMode.ResizeBottom, DragMode.ResizeBottomRight
                 })
        {
            var handle = new Rectangle
            {
                Width = handleSize,
                Height = handleSize,
                Fill = Brushes.White,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1,
                Tag = mode,
                Cursor = CursorFor(mode),
                Visibility = Visibility.Collapsed
            };

            _handles.Add(handle);
            AdornerLayer.Children.Add(handle);
        }
    }

    /// <summary>Repositions the outline and handles over the current selection.</summary>
    private void UpdateAdorner()
    {
        var viewModel = ViewModel;
        var selected = viewModel?.SelectedElement;

        if (viewModel == null || selected == null || !viewModel.HasDocument)
        {
            SetAdornerVisibility(Visibility.Collapsed);
            return;
        }

        var canvasBounds = OverlayGeometry.DesignToCanvas(selected.DesignBounds, viewModel.Zoom);

        SetAdornerVisibility(Visibility.Visible);

        _selectionOutline.Width = Math.Max(0, canvasBounds.Width);
        _selectionOutline.Height = Math.Max(0, canvasBounds.Height);
        Canvas.SetLeft(_selectionOutline, canvasBounds.X);
        Canvas.SetTop(_selectionOutline, canvasBounds.Y);

        foreach (var handle in _handles)
        {
            var position = HandlePosition((DragMode)handle.Tag, canvasBounds);
            Canvas.SetLeft(handle, position.X - handleSize / 2);
            Canvas.SetTop(handle, position.Y - handleSize / 2);
        }
    }

    private void SetAdornerVisibility(Visibility visibility)
    {
        _selectionOutline.Visibility = visibility;
        foreach (var handle in _handles)
        {
            handle.Visibility = visibility;
        }
    }

    private static Point HandlePosition(DragMode mode, Rect bounds) => mode switch
    {
        DragMode.ResizeTopLeft => new Point(bounds.Left, bounds.Top),
        DragMode.ResizeTop => new Point(bounds.Left + bounds.Width / 2, bounds.Top),
        DragMode.ResizeTopRight => new Point(bounds.Right, bounds.Top),
        DragMode.ResizeLeft => new Point(bounds.Left, bounds.Top + bounds.Height / 2),
        DragMode.ResizeRight => new Point(bounds.Right, bounds.Top + bounds.Height / 2),
        DragMode.ResizeBottomLeft => new Point(bounds.Left, bounds.Bottom),
        DragMode.ResizeBottom => new Point(bounds.Left + bounds.Width / 2, bounds.Bottom),
        DragMode.ResizeBottomRight => new Point(bounds.Right, bounds.Bottom),
        _ => new Point(bounds.Left, bounds.Top)
    };

    private static System.Windows.Input.Cursor CursorFor(DragMode mode) => mode switch
    {
        DragMode.ResizeLeft or DragMode.ResizeRight => Cursors.SizeWE,
        DragMode.ResizeTop or DragMode.ResizeBottom => Cursors.SizeNS,
        DragMode.ResizeTopLeft or DragMode.ResizeBottomRight => Cursors.SizeNWSE,
        DragMode.ResizeTopRight or DragMode.ResizeBottomLeft => Cursors.SizeNESW,
        _ => Cursors.SizeAll
    };

    #endregion

    #region Pointer input

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel?.SelectedElement == null)
        {
            return;
        }

        var canvasPoint = e.GetPosition(CanvasRoot);
        _dragMode = HitTest(canvasPoint, viewModel);

        if (_dragMode == DragMode.None)
        {
            return;
        }

        _dragStartDesignPoint = OverlayGeometry.CanvasToDesign(canvasPoint, viewModel.Zoom);
        _dragStartBounds = viewModel.SelectedElement.DesignBounds;

        CanvasRoot.CaptureMouse();
        Focus();
        e.Handled = true;
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel == null)
        {
            return;
        }

        var canvasPoint = e.GetPosition(CanvasRoot);

        if (_dragMode == DragMode.None)
        {
            // Hover feedback: the cursor advertises what a press would do.
            CanvasRoot.Cursor = HitTest(canvasPoint, viewModel) switch
            {
                DragMode.None => Cursors.Arrow,
                var mode => CursorFor(mode)
            };
            return;
        }

        var designPoint = OverlayGeometry.CanvasToDesign(canvasPoint, viewModel.Zoom);
        var dx = designPoint.X - _dragStartDesignPoint.X;
        var dy = designPoint.Y - _dragStartDesignPoint.Y;

        // Applied live so the preview tracks the pointer; recorded once on release.
        viewModel.ApplyGesture(ComputeDraggedBounds(_dragStartBounds, dx, dy));
        e.Handled = true;
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragMode == DragMode.None)
        {
            return;
        }

        _dragMode = DragMode.None;
        CanvasRoot.ReleaseMouseCapture();
        ViewModel?.EndGesture();
        e.Handled = true;
    }

    private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
    {
        // Only reset the cursor; an in-flight drag keeps running via mouse capture.
        if (_dragMode == DragMode.None)
        {
            CanvasRoot.Cursor = Cursors.Arrow;
        }
    }

    /// <summary>
    /// Resolves a canvas point to a handle, the selection interior, or nothing.
    /// Handles win over the interior so edge drags resize rather than move.
    /// </summary>
    private DragMode HitTest(Point canvasPoint, OverlayDesignerViewModel viewModel)
    {
        var selected = viewModel.SelectedElement;
        if (selected == null)
        {
            return DragMode.None;
        }

        var bounds = OverlayGeometry.DesignToCanvas(selected.DesignBounds, viewModel.Zoom);

        foreach (var handle in _handles)
        {
            var mode = (DragMode)handle.Tag;
            var centre = HandlePosition(mode, bounds);
            var hitBox = new Rect(
                centre.X - handleSize, centre.Y - handleSize,
                handleSize * 2, handleSize * 2);

            if (hitBox.Contains(canvasPoint))
            {
                return mode;
            }
        }

        return bounds.Contains(canvasPoint) ? DragMode.Move : DragMode.None;
    }

    /// <summary>
    /// Applies a pointer delta to the gesture's starting bounds. Resizing clamps at zero so
    /// dragging an edge past its opposite side collapses rather than inverting the rectangle.
    /// </summary>
    private Rect ComputeDraggedBounds(Rect start, double dx, double dy)
    {
        var left = start.Left;
        var top = start.Top;
        var right = start.Right;
        var bottom = start.Bottom;

        switch (_dragMode)
        {
            case DragMode.Move:
                return new Rect(start.X + dx, start.Y + dy, start.Width, start.Height);

            case DragMode.ResizeLeft: left += dx; break;
            case DragMode.ResizeRight: right += dx; break;
            case DragMode.ResizeTop: top += dy; break;
            case DragMode.ResizeBottom: bottom += dy; break;

            case DragMode.ResizeTopLeft: left += dx; top += dy; break;
            case DragMode.ResizeTopRight: right += dx; top += dy; break;
            case DragMode.ResizeBottomLeft: left += dx; bottom += dy; break;
            case DragMode.ResizeBottomRight: right += dx; bottom += dy; break;

            default:
                return start;
        }

        return new Rect(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Abs(right - left),
            Math.Abs(bottom - top));
    }

    #endregion

    /// <summary>
    /// Opens HandyControl's colour picker for the title colour.
    ///
    /// ColorPicker is a Control with Confirmed/Canceled events rather than an inline editor,
    /// so it is hosted in a PopupWindow — the pattern HandyControl intends. The chosen colour
    /// is written back as a hex string because that is what the schema field stores.
    /// </summary>
    private void OnPickTitleColourClicked(object sender, RoutedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel == null)
        {
            return;
        }

        var picker = SingleOpenHelper.CreateControl<HandyControl.Controls.ColorPicker>();
        var window = new PopupWindow
        {
            PopupElement = picker,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            AllowsTransparency = true,
            WindowStyle = WindowStyle.None,
            MinWidth = 0,
            MinHeight = 0,
            Title = "Title colour"
        };

        picker.SelectedBrush = viewModel.TitleForegroundBrush as SolidColorBrush ?? Brushes.White;

        picker.Confirmed += (_, args) =>
        {
            viewModel.ApplyTitleColour(args.Info);
            window.Close();
        };
        picker.Canceled += (_, _) => window.Close();

        window.Show(this, false);
    }

    /// <summary>
    /// Steps back to the template picker rather than leaving the designer, so the author can
    /// start a different overlay without reopening the dialog. Confirms first when there are
    /// edits, because nothing can be saved yet.
    /// </summary>
    private void OnBackToTemplatesClicked(object sender, RoutedEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel == null || !viewModel.ConfirmDiscardIfDirty())
        {
            return;
        }

        viewModel.ReturnToTemplates();
    }

    /// <summary>
    /// Leaves the designer entirely.
    ///
    /// No prompt here: Prism routes the request through
    /// <see cref="OverlayDesignerViewModel.CanCloseDialog"/>, which owns the discard
    /// confirmation so the window's X button is covered too. Asking again would double-prompt.
    /// </summary>
    private void OnCloseClicked(object sender, RoutedEventArgs e) =>
        ViewModel?.RequestClose.Invoke(ButtonResult.Cancel);

}
