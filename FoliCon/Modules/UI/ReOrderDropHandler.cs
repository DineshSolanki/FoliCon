namespace FoliCon.Modules.UI;

public class ReOrderDropHandler : IDropTarget
{
    private readonly DefaultDropHandler _defaultDropHandler = new();

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.DragInfo.VisualSource != dropInfo.VisualTarget)
        {
            dropInfo.Effects = DragDropEffects.None;
            dropInfo.NotHandled = true;
            return;
        }

        _defaultDropHandler.DragOver(dropInfo);
    }

    public void Drop(IDropInfo dropInfo)
    {
        _defaultDropHandler.Drop(dropInfo);
    }
}