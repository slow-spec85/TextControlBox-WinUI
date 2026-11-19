using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas;
using TextControlBoxNS.Core.Selection;
using TextControlBoxNS.Helper;

namespace TextControlBoxNS.Core.Renderer;

internal class LineHighlighterRenderer
{
    private LineHighlighterManager lineHighlighterManager;
    private SelectionManager selectionManager;
    public void Init(LineHighlighterManager lineHighlighterManager, SelectionManager selectionManager)
    {
        this.selectionManager = selectionManager;
        this.lineHighlighterManager = lineHighlighterManager;
    }

    public void Render(
        CanvasDrawingSession drawingSession,
        EditorRenderViewport viewport,
        int line,
        float width,
        float dpiScale,
        CanvasSolidColorBrush backgroundBrush)
    {
        if (backgroundBrush == null || line < viewport.FirstVisibleLine || line > viewport.LastVisibleLine)
            return;

        float y = viewport.GetLineTop(line);
        float height = viewport.GetLineRowCount(line) * viewport.LineHeight;
        drawingSession.FillRectangle(
            Utils.CreateLineAlignedRect(0, y, width, height, dpiScale),
            backgroundBrush);
    }

    public bool CanRender(FocusManager focusManager)
    {
        if(lineHighlighterManager._ShowLineHighlighter && !selectionManager.HasSelection)
        {
            return lineHighlighterManager._HighlightLineWhenNotFocused ? true : focusManager.HasFocus;
        }
        return false;
    }
}
