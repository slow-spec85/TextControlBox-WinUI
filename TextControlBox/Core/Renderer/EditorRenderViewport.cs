namespace TextControlBoxNS.Core.Renderer;

internal readonly record struct EditorRenderViewport(
    int FirstVisibleLine,
    int VisibleLineCount,
    float LineHeight,
    float TopOffset)
{
    public int LastVisibleLine => checked(FirstVisibleLine + VisibleLineCount - 1);
}
