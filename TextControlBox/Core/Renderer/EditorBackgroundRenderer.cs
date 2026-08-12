using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Helper;

namespace TextControlBoxNS.Core.Renderer;

internal sealed class EditorBackgroundRenderer
{
    private readonly List<ResolvedLineDecoration> visibleDecorations = [];
    private LineDecorationStore lineDecorationStore;
    private LineGutterRenderer lineGutterRenderer;
    private TextDecorationRenderer textDecorationRenderer;
    private TextRenderer textRenderer;
    private LineHighlighterRenderer lineHighlighterRenderer;
    private TextManager textManager;
    private CursorManager cursorManager;
    private ScrollManager scrollManager;
    private ZoomManager zoomManager;
    private FocusManager focusManager;
    private DesignHelper designHelper;

    public void Init(
        LineDecorationStore decorationStore,
        LineGutterRenderer gutterRenderer,
        TextDecorationRenderer rangeDecorationRenderer,
        TextRenderer currentTextRenderer,
        LineHighlighterRenderer currentLineHighlighterRenderer,
        TextManager manager,
        CursorManager currentCursorManager,
        ScrollManager currentScrollManager,
        ZoomManager currentZoomManager,
        FocusManager currentFocusManager,
        DesignHelper currentDesignHelper)
    {
        lineDecorationStore = decorationStore;
        lineGutterRenderer = gutterRenderer;
        textDecorationRenderer = rangeDecorationRenderer;
        textRenderer = currentTextRenderer;
        lineHighlighterRenderer = currentLineHighlighterRenderer;
        textManager = manager;
        cursorManager = currentCursorManager;
        scrollManager = currentScrollManager;
        zoomManager = currentZoomManager;
        focusManager = currentFocusManager;
        designHelper = currentDesignHelper;
    }

    public void Draw(
        CanvasControl canvas,
        CanvasDrawEventArgs args,
        float lineGutterWidth)
    {
        float fontSize = zoomManager.ZoomedFontSize;
        float lineHeight = fontSize + textManager._LineSpacing;
        if (lineHeight <= 0 || textManager.LinesCount == 0)
            return;

        int firstVisibleLine = Math.Min(
            (int)((scrollManager.VerticalScroll * scrollManager.DefaultVerticalScrollSensitivity) / lineHeight),
            textManager.LinesCount);
        int visibleLineCount = Math.Min(
            (int)(canvas.ActualHeight / lineHeight),
            textManager.LinesCount - firstVisibleLine);
        if (visibleLineCount <= 0)
            return;

        float topOffset = lineHeight / scrollManager.DefaultVerticalScrollSensitivity;
        EditorRenderViewport viewport = new(
            firstVisibleLine,
            visibleLineCount,
            lineHeight,
            topOffset);
        float textViewportWidth = Math.Max(0, (float)canvas.ActualWidth - lineGutterWidth);

        lineGutterRenderer.Draw(canvas, args, viewport, lineGutterWidth);

        lineDecorationStore.AppendVisibleDecorations(
            firstVisibleLine,
            viewport.LastVisibleLine,
            visibleDecorations);
        foreach (ResolvedLineDecoration decoration in visibleDecorations)
        {
            float y = ((decoration.StartLine - firstVisibleLine) * lineHeight) + viewport.TopOffset;
            float height = (decoration.EndLine - decoration.StartLine + 1) * lineHeight;
            args.DrawingSession.FillRectangle(
                Utils.CreateLineAlignedRect(
                    lineGutterWidth,
                    y,
                    textViewportWidth,
                    height,
                    canvas.DpiScale),
                decoration.BackgroundColor);
        }

        if (lineHighlighterRenderer.CanRender(focusManager)
            && cursorManager.LineNumber >= firstVisibleLine
            && cursorManager.LineNumber <= viewport.LastVisibleLine)
        {
            float currentLineY = ((cursorManager.LineNumber - firstVisibleLine) * lineHeight) + viewport.TopOffset;
            lineHighlighterRenderer.Render(
                textViewportWidth,
                currentLineY,
                lineHeight,
                canvas.DpiScale,
                args,
                designHelper.LineHighlighterBrush,
                lineGutterWidth);
        }

        textDecorationRenderer.DrawBackgroundAndBorder(
            args,
            textRenderer,
            canvas.DpiScale,
            lineGutterWidth);
    }
}
