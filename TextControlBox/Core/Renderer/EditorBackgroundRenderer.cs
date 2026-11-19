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
        if (GetViewport(canvas.ActualHeight) is not { VisibleLineCount: > 0 } viewport)
            return;

        float textViewportWidth = Math.Max(0, (float)canvas.ActualWidth - lineGutterWidth);

        bool hasLineGutter = lineGutterRenderer.DrawBackground(canvas, args, viewport, lineGutterWidth);

        lineDecorationStore.AppendVisibleDecorations(
            viewport.FirstVisibleLine,
            viewport.LastVisibleLine,
            visibleDecorations);
        foreach (ResolvedLineDecoration decoration in visibleDecorations)
        {
            float y = viewport.GetLineTop(decoration.StartLine);
            float height = viewport.GetLineTop(decoration.EndLine + 1) - y;
            args.DrawingSession.FillRectangle(
                Utils.CreateLineAlignedRect(
                    lineGutterWidth,
                    y,
                    textViewportWidth,
                    height,
                    canvas.DpiScale),
                decoration.BackgroundColor);
        }

        textDecorationRenderer.DrawBackgroundAndBorder(
            args,
            textRenderer,
            canvas.DpiScale,
            lineGutterWidth);

        DrawCurrentLineHighlight(canvas, args, viewport);
        if (hasLineGutter)
            lineGutterRenderer.DrawMarkers(canvas, args, viewport, lineGutterWidth, textRenderer.TextVerticalOffset);
    }

    public void DrawCurrentLineHighlight(CanvasControl canvas, CanvasDrawEventArgs args)
    {
        if (GetViewport(canvas.ActualHeight) is { VisibleLineCount: > 0 } viewport)
            DrawCurrentLineHighlight(canvas, args, viewport);
    }

    private void DrawCurrentLineHighlight(CanvasControl canvas, CanvasDrawEventArgs args, EditorRenderViewport viewport)
    {
        if (lineHighlighterRenderer.CanRender(focusManager))
        {
            lineHighlighterRenderer.Render(
                args.DrawingSession,
                viewport,
                cursorManager.LineNumber,
                (float)canvas.ActualWidth,
                canvas.DpiScale,
                designHelper.LineHighlighterBrush);
        }
    }

    private EditorRenderViewport? GetViewport(double height)
    {
        if (textRenderer.IsWordWrapEnabled)
            return textRenderer.WrappedViewport;

        float lineHeight = zoomManager.ZoomedFontSize + textManager._LineSpacing;
        if (lineHeight <= 0 || textManager.LinesCount == 0)
            return null;

        int firstVisibleLine = Math.Min(
            (int)((scrollManager.VerticalScroll * scrollManager.DefaultVerticalScrollSensitivity) / lineHeight),
            textManager.LinesCount);
        int visibleLineCount = Math.Min(
            (int)(height / lineHeight),
            textManager.LinesCount - firstVisibleLine);
        float topOffset = lineHeight / scrollManager.DefaultVerticalScrollSensitivity;
        return new EditorRenderViewport(firstVisibleLine, visibleLineCount, lineHeight, topOffset);
    }
}
