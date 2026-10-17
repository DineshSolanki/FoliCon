namespace FoliCon.Modules.UI;

public class ReOrderDropHandler : IDropTarget
{
    private readonly DefaultDropHandler _defaultDropHandler = new();

    public void DragOver(IDropInfo dropInfo)
    {
        if (!CanAcceptDrop(dropInfo))
        {
            dropInfo.Effects = DragDropEffects.None;
            dropInfo.DropTargetAdorner = null;
            return;
        }

        _defaultDropHandler.DragOver(dropInfo);
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (!CanAcceptDrop(dropInfo))
        {
            return;
        }

        _defaultDropHandler.Drop(dropInfo);
    }

    public static bool CanAcceptDrop(IDropInfo dropInfo)
    {
        if (dropInfo?.DragInfo == null)
        {
            return false;
        }

        // Prevent dragging across different collections (e.g. from icons list into folder list)
        if (dropInfo.DragInfo.SourceCollection == null ||
            dropInfo.TargetCollection == null ||
            !ReferenceEquals(dropInfo.DragInfo.SourceCollection, dropInfo.TargetCollection))
        {
            return false;
        }

        return true;
    }
}