using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
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
        float canvasWidth,
        float y,
        float lineHeight,
        float dpiScale,
        CanvasDrawEventArgs args,
        CanvasSolidColorBrush backgroundBrush,
        float x = 0)
    {
        if (backgroundBrush == null)
            return;

        args.DrawingSession.FillRectangle(
            Utils.CreateLineAlignedRect(x, y, canvasWidth, lineHeight, dpiScale),
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
