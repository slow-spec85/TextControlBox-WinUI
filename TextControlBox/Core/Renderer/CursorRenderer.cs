using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using System.Numerics;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Helper;

namespace TextControlBoxNS.Core.Renderer;

internal class CursorRenderer
{
    public CursorSize _CursorSize = null;

    private CursorManager cursorManager;
    private CurrentLineManager currentLineManager;
    private TextRenderer textRenderer;
    private FocusManager focusManager;
    private TextManager textManager;
    private ScrollManager scrollManager;
    private DesignHelper designHelper;
    private EventsManager eventsManager;
    private LongestLineManager longestLineManager;
    private CaretBlinkManager caretBlinkManager;

    public void Init(
        CursorManager cursorManager,
        CurrentLineManager currentLineManager,
        TextRenderer textRenderer,
        FocusManager focusManager,
        TextManager textManager,
        ScrollManager scrollManager,
        DesignHelper designHelper,
        EventsManager eventsManager,
        LongestLineManager longestLineManager,
        CaretBlinkManager caretBlinkManager)
    {
        this.cursorManager = cursorManager;
        this.currentLineManager = currentLineManager;
        this.textRenderer = textRenderer;
        this.focusManager = focusManager;
        this.textManager = textManager;
        this.scrollManager = scrollManager;
        this.designHelper = designHelper;
        this.eventsManager = eventsManager;
        this.longestLineManager = longestLineManager;
        this.caretBlinkManager = caretBlinkManager;
    }

    public void RenderCursor(CanvasTextLayout textLayout, int characterPosition, float xOffset, float y, float lineHeight, CursorSize customSize, CanvasDrawEventArgs args, CanvasSolidColorBrush cursorColorBrush)
    {
        if (textLayout == null)
            return;


        Vector2 vector = textLayout.GetCaretPosition(characterPosition < 0 ? 0 : characterPosition, false);
        if (customSize == null)
            args.DrawingSession.FillRectangle(vector.X + xOffset, y, 2, lineHeight, cursorColorBrush);
        else
            args.DrawingSession.FillRectangle(vector.X + xOffset + customSize.OffsetX, y + customSize.OffsetY, (float)customSize.Width, (float)customSize.Height, cursorColorBrush);
    }

    public void Draw(CanvasControl canvasText, CanvasControl canvasCursor, CanvasDrawEventArgs args)
    {
        currentLineManager.UpdateCurrentLine(cursorManager.LineNumber);
        if (textRenderer.DrawnTextLayout == null)
            return;

        int currentLineLength = currentLineManager.Length;
        if (cursorManager.LineNumber >= textManager.LinesCount)
        {
            cursorManager.LineNumber = textManager.LinesCount - 1;
            cursorManager.CharacterPosition = currentLineLength;
        }

        float renderPosY = (float)((cursorManager.LineNumber - textRenderer.NumberOfStartLine) * textRenderer.SingleLineHeight) + textRenderer.SingleLineHeight / scrollManager.DefaultVerticalScrollSensitivity;  
        if (renderPosY > textRenderer.NumberOfRenderedLines * textRenderer.SingleLineHeight || renderPosY < 0)
            return;

        textRenderer.UpdateCurrentLineTextLayout(canvasText);

        scrollManager.EnsureHorizontalScrollBounds(canvasText, longestLineManager, true);


        if (focusManager.HasFocus)
        {
            int characterPos = cursorManager.CharacterPosition;
            if (characterPos > currentLineLength)
                characterPos = currentLineLength;

            // Only paint the caret during the "on" phase of the blink.
            if (caretBlinkManager.IsCaretVisible)
            {
                RenderCursor(
                    textRenderer.CurrentLineTextLayout,
                    characterPos,
                    (float)-scrollManager.HorizontalScroll,
                    renderPosY,
                    textRenderer.SingleLineHeight,
                    _CursorSize,
                    args,
                    designHelper.CursorColorBrush);
            }

            if (!cursorManager.Equals(cursorManager.currentCursorPosition, cursorManager.oldCursorPosition))
            {
                cursorManager.oldCursorPosition.SetChangeValues(cursorManager.currentCursorPosition);
                eventsManager.CallSelectionChanged();
            }
        }
    }
}
