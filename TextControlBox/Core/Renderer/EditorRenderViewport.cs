namespace TextControlBoxNS.Core.Renderer;

internal readonly record struct EditorRenderViewport(
    int FirstVisibleLine,
    int VisibleLineCount,
    float LineHeight,
    float TopOffset,
    WrappedLineLayout WrapLayout = null)
{
    public int LastVisibleLine => checked(FirstVisibleLine + VisibleLineCount - 1);

    public float GetLineTop(int line)
    {
        int rowOffset = WrapLayout is null
            ? line - FirstVisibleLine
            : WrapLayout.GetStartRow(line) - WrapLayout.GetStartRow(FirstVisibleLine);
        return TopOffset + rowOffset * LineHeight;
    }

    public int GetLineRowCount(int line) => WrapLayout?.GetRowCount(line) ?? 1;
}
