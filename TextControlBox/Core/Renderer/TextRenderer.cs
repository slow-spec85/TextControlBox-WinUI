using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Helper;
using Windows.Foundation;

namespace TextControlBoxNS.Core.Renderer;

internal class TextRenderer
{
    public CanvasTextFormat TextFormat = null;
    public CanvasTextLayout DrawnTextLayout = null;
    public CanvasTextLayout CurrentLineTextLayout = null;


    public bool NeedsUpdateTextLayout = true;
    public bool NeedsTextFormatUpdate = true;
    public float SingleLineHeight { get => TextFormat == null ? 0 : TextFormat.LineSpacing; }
    public float TextVerticalOffset => textLayoutManager.TextVerticalOffset - WrappedViewportOffset;
    public float BackgroundVerticalOffset => WrappedViewport?.TopOffset ?? ScrollTopInset;
    public float HorizontalOffset => (float)-scrollManager.HorizontalScroll;
    public int NumberOfStartLine = 0;
    public int NumberOfRenderedLines = 0;
    public string RenderedText = "";
    public string OldRenderedText = null;
    private int oldNumberOfStartLine = -1;
    private long oldSyntaxStateRevision = -1;
    private CanvasTextFormat wrappedFormat;
    private WrappedLineLayout wrappedLayout;
    private float wrappedWidth;
    private int firstVisualRow;
    private int? wrapScrollAnchor;

    public bool IsWordWrapEnabled { get; private set; }
    public double ViewportHeight => coreTextbox.canvasText.ActualHeight;
    public EditorRenderViewport? WrappedViewport { get; private set; }
    private float ScrollTopInset => SingleLineHeight / scrollManager.DefaultVerticalScrollSensitivity;
    private float WrappedViewportOffset => ScrollTopInset - BackgroundVerticalOffset;

    private CursorManager cursorManager;
    private TextManager textManager;
    private ScrollManager scrollManager;
    private LineNumberRenderer lineNumberRenderer;
    private TextLayoutManager textLayoutManager;
    private DesignHelper designHelper;
    private Grid scrollGrid;
    private LongestLineManager longestLineManager;
    private SearchManager searchManager;
    private CoreTextControlBox coreTextbox;
    private CanvasUpdateManager canvasUpdateManager;
    private ZoomManager zoomManager;
    private WhitespaceCharactersRenderer invisibleCharactersRenderer;
    private LinkRenderer linkRenderer;
    private LinkHighlightManager linkHighlightManager;
    private StatefulSyntaxHighlightingManager statefulSyntaxHighlightingManager;
    private TextDecorationRenderer textDecorationRenderer;

    public void Init(
        CursorManager cursorManager,
        DesignHelper designHelper,
        TextLayoutManager textLayoutManager,
        TextManager textManager,
        ScrollManager scrollManager,
        LineNumberRenderer lineNumberRenderer,
        LongestLineManager longestLineManager,
        CoreTextControlBox textbox,
        SearchManager searchManager,
        CanvasUpdateManager canvasUpdateManager,
        ZoomManager zoomManager,
        WhitespaceCharactersRenderer invisibleCharactersRenderer,
        LinkRenderer linkRenderer,
        LinkHighlightManager linkHighlightManager,
        StatefulSyntaxHighlightingManager statefulSyntaxManager,
        TextDecorationRenderer rangeDecorationRenderer)
    {
        this.cursorManager = cursorManager;
        this.textManager = textManager;
        this.designHelper = designHelper;
        this.textLayoutManager = textLayoutManager;
        this.scrollManager = scrollManager;
        this.lineNumberRenderer = lineNumberRenderer;
        this.longestLineManager = longestLineManager;
        this.searchManager = searchManager;
        this.coreTextbox = textbox;
        this.scrollGrid = textbox.scrollGrid;
        this.canvasUpdateManager = canvasUpdateManager;
        this.zoomManager = zoomManager;
        this.invisibleCharactersRenderer = invisibleCharactersRenderer;
        this.linkRenderer = linkRenderer;
        this.linkHighlightManager = linkHighlightManager;
        statefulSyntaxHighlightingManager = statefulSyntaxManager;
        textDecorationRenderer = rangeDecorationRenderer;
    }

    public void CheckDispose()
    {
        TextFormat?.Dispose();
        wrappedFormat?.Dispose();
        DrawnTextLayout?.Dispose();
        CurrentLineTextLayout?.Dispose();
        invisibleCharactersRenderer.CheckDispose();
    }

    //Check whether the current line is outside the bounds of the visible area
    public bool OutOfRenderedArea(int line)
    {
        return line < NumberOfStartLine || line >= NumberOfStartLine + NumberOfRenderedLines;
    }

    public void UpdateCurrentLineTextLayout(CanvasControl canvasText)
    {
        CurrentLineTextLayout?.Dispose();
        if (IsWordWrapEnabled)
        {
            CurrentLineTextLayout = CreateWrappedLineLayout(cursorManager.LineNumber);
            return;
        }

        CurrentLineTextLayout =
            cursorManager.LineNumber < textManager.LinesCount ?
            textLayoutManager.CreateTextLayout(
            canvasText,
            TextFormat,
            textManager.GetLineText(cursorManager.LineNumber) + "|",
            canvasText.Size) :
            null;
    }

    public void SetWordWrap(bool enabled)
    {
        if (IsWordWrapEnabled == enabled)
            return;

        wrapScrollAnchor ??= GetTopDocumentLine();
        IsWordWrapEnabled = enabled;
        InvalidateWrappedDocument(keepScrollAnchor: true);
        NeedsUpdateTextLayout = true;
    }

    public void InvalidateWrappedDocument(bool keepScrollAnchor = false)
    {
        wrappedLayout = null;
        if (!keepScrollAnchor)
            wrapScrollAnchor = null;
    }

    private int GetTopDocumentLine()
    {
        int row = GetScrollRow();
        return wrappedLayout?.GetLineAtRow(row) ?? row;
    }

    public int GetWrappedLineAt(double y)
    {
        if (!PrepareWrappedLayout() || wrappedLayout.RowCount == 0)
            return -1;

        double row = Math.Floor((y - ScrollTopInset) / SingleLineHeight);
        return wrappedLayout.GetLineAtRow(GetScrollRow() + Math.Max(0, (int)row));
    }

    public int HitTestWrappedLine(int line, Point point)
    {
        int length = textManager.GetLineLength(line);
        if (length == 0)
            return 0;

        using CanvasTextLayout layout = CreateWrappedLineLayout(line);
        if (layout is null)
            return 0;

        float y = (float)point.Y - GetWrappedLineTop(line);
        layout.HitTest((float)point.X, y, out CanvasTextLayoutRegion region, out bool trailing);
        int column = region.CharacterIndex + (trailing ? region.CharacterCount : 0);
        return Math.Clamp(column, 0, length);
    }

    internal static Point GetWrappedCaretPosition(CanvasTextLayout layout, int column, float lineTop)
    {
        column = Math.Max(0, column);
        // Win2D positions glyphs relative to the baseline. The caret uses the editor's row top.
        return new Point(
            layout.GetCaretPosition(column, false).X,
            lineTop + GetWrappedCaretRow(layout, column) * layout.LineSpacing);
    }

    private static int GetWrappedCaretRow(CanvasTextLayout layout, int column)
    {
        if (layout.LineCount <= 1)
            return 0;

        CanvasLineMetrics[] rows = layout.LineMetrics;
        for (int row = 0; row < rows.Length - 1; row++)
        {
            if (column < rows[row].CharacterCount)
                return row;

            column -= rows[row].CharacterCount;
        }

        return rows.Length - 1;
    }

    public float GetWrappedLineTop(int line) =>
        (wrappedLayout.GetStartRow(line) - GetScrollRow()) * SingleLineHeight
        + ScrollTopInset;

    public bool TryGetWrappedScrollRow(int line, int? column, out int row)
    {
        row = 0;
        if (!PrepareWrappedLayout() || line < 0 || line >= textManager.LinesCount)
            return false;

        row = wrappedLayout.GetStartRow(line);
        if (column is > 0 && wrappedLayout.GetRowCount(line) > 1)
        {
            using CanvasTextLayout layout = CreateWrappedLineLayout(line);
            row += GetWrappedCaretRow(layout, Math.Min(column.Value, textManager.GetLineLength(line)));
        }

        return true;
    }

    private CanvasTextLayout CreateWrappedLineLayout(int line)
    {
        if (!PrepareWrappedLayout() || line < 0 || line >= textManager.LinesCount)
            return null;

        return textLayoutManager.CreateTextLayout(
            coreTextbox.canvasText, wrappedFormat, textManager.GetLineText(line), wrappedWidth, 0);
    }

    public bool IsScrolledToDifferentRow => GetScrollRow() != (IsWordWrapEnabled ? firstVisualRow : NumberOfStartLine);

    public int GetScrollRow() => SingleLineHeight > 0
        ? (int)(scrollManager.VerticalScroll * scrollManager.DefaultVerticalScrollSensitivity / SingleLineHeight)
        : 0;

    private bool PrepareWrappedLayout()
    {
        CanvasControl canvas = coreTextbox.canvasText;
        if (!IsWordWrapEnabled || !canvas.ReadyToDraw || SingleLineHeight <= 0 || canvas.ActualWidth <= 0)
            return false;

        float width = (float)canvas.ActualWidth;
        if (wrappedFormat is null)
        {
            wrappedFormat = textLayoutManager.CreateCanvasTextFormat();
            wrappedFormat.WordWrapping = CanvasWordWrapping.Wrap;
        }

        if (wrappedLayout is null || width != wrappedWidth)
        {
            int? anchor = wrapScrollAnchor;
            if (wrappedLayout is not null)
                anchor ??= GetTopDocumentLine();

            wrappedWidth = width;
            wrappedLayout = new WrappedLineLayout(canvas, wrappedFormat, width, textManager.totalLines.Span);
            wrapScrollAnchor = null;
            NeedsUpdateTextLayout = true;
            UpdateWrappedScrollBounds();
            if (anchor is >= 0 && anchor < textManager.LinesCount)
            {
                scrollManager.verticalScrollBar.Value =
                    (double)wrappedLayout.GetStartRow(anchor.Value) * SingleLineHeight / scrollManager.DefaultVerticalScrollSensitivity;
            }
        }

        return true;
    }

    private void UpdateWrappedScrollBounds()
    {
        double maxOffset = Math.Min(
            (wrappedLayout.RowCount - 1d) * SingleLineHeight,
            (wrappedLayout.RowCount + 1d) * SingleLineHeight - ViewportHeight);
        scrollManager.verticalScrollBar.Maximum = Math.Max(0, maxOffset / scrollManager.DefaultVerticalScrollSensitivity);
        scrollManager.verticalScrollBar.ViewportSize = ViewportHeight / scrollManager.DefaultVerticalScrollSensitivity;
    }

    private (int startLine, int linesToRender) CalculateWrappedLinesToRender()
    {
        UpdateWrappedScrollBounds();
        firstVisualRow = GetScrollRow();
        int start = wrappedLayout.GetLineAtRow(firstVisualRow);
        if (start < 0 || firstVisualRow >= wrappedLayout.RowCount)
        {
            WrappedViewport = null;
            return (0, 0);
        }

        int rows = (int)Math.Ceiling(ViewportHeight / SingleLineHeight);
        int end = wrappedLayout.GetLineAtRow(firstVisualRow + Math.Max(0, rows - 1));
        WrappedViewport = new EditorRenderViewport(
            start, end - start + 1, SingleLineHeight, GetWrappedLineTop(start), wrappedLayout);
        return (start, end - start + 1);
    }

    public (int startLine, int linesToRender) CalculateLinesToRender()
    {
        if (PrepareWrappedLayout())
            return CalculateWrappedLinesToRender();

        WrappedViewport = null;
        var singleLineHeight = SingleLineHeight;

        //Measure text position and apply the value to the scrollbar
        scrollManager.verticalScrollBar.Maximum = ((textManager.LinesCount + 1) * singleLineHeight - scrollGrid.ActualHeight) / scrollManager.DefaultVerticalScrollSensitivity;
        scrollManager.verticalScrollBar.ViewportSize = coreTextbox.canvasText.ActualHeight;
        if (!IsWordWrapEnabled && wrapScrollAnchor is int anchor)
        {
            wrapScrollAnchor = null;
            scrollManager.verticalScrollBar.Value =
                (double)anchor * singleLineHeight / scrollManager.DefaultVerticalScrollSensitivity;
        }

        //Calculate number of lines that need to be rendered
        int linesToRenderCount = (int)(coreTextbox.canvasText.ActualHeight / singleLineHeight);
        linesToRenderCount = Math.Min(linesToRenderCount, textManager.LinesCount);

        int startLine = (int)((scrollManager.VerticalScroll * scrollManager.DefaultVerticalScrollSensitivity) / singleLineHeight);
        startLine = Math.Min(startLine, textManager.LinesCount);

        int linesToRender = Math.Min(linesToRenderCount, textManager.LinesCount - startLine);

        return (startLine, linesToRender);
    }

    public void Draw(CanvasControl canvasText, CanvasDrawEventArgs args)
    {
        //Create resources and layouts:
        if (NeedsTextFormatUpdate || TextFormat == null)
        {
            lineNumberRenderer.CreateLineNumberTextFormat();

            if (wrappedLayout is not null)
                wrapScrollAnchor = GetTopDocumentLine();
            InvalidateWrappedDocument(keepScrollAnchor: true);
            wrappedFormat?.Dispose();
            wrappedFormat = null;
            TextFormat?.Dispose();
            TextFormat = textLayoutManager.CreateCanvasTextFormat();
            NeedsTextFormatUpdate = false;
            NeedsUpdateTextLayout = true;

            invisibleCharactersRenderer.UpdateTextFormat(canvasText, TextFormat);
        }
        designHelper.CreateColorResources(args.DrawingSession);

        EditorRenderViewport? previousWrappedViewport = WrappedViewport;
        (NumberOfStartLine, NumberOfRenderedLines) = CalculateLinesToRender();
        var renderTextData = textManager.GetLinesForRendering(NumberOfStartLine, NumberOfRenderedLines);
        var linesToRenderRef = renderTextData.Lines;
        RenderedText = renderTextData.Text;
        long syntaxStateRevision = statefulSyntaxHighlightingManager.Revision;

        //check rendering and calculation updates
        lineNumberRenderer.CheckGenerateLineNumberText();

        using CanvasCommandList canvasCommandList = new CanvasCommandList(args.DrawingSession);
        bool layoutChanged = (OldRenderedText != null && OldRenderedText.Length != RenderedText.Length)
            || !RenderedText.Equals(OldRenderedText, StringComparison.Ordinal)
            || oldNumberOfStartLine != NumberOfStartLine
            || oldSyntaxStateRevision != syntaxStateRevision
            || NeedsUpdateTextLayout;
        if (layoutChanged)
        {
            NeedsUpdateTextLayout = false;
            OldRenderedText = RenderedText;
            oldNumberOfStartLine = NumberOfStartLine;
            oldSyntaxStateRevision = syntaxStateRevision;

            DrawnTextLayout = WrappedViewport.HasValue
                ? textLayoutManager.CreateTextResource(canvasText, DrawnTextLayout, wrappedFormat, RenderedText, new Size(wrappedWidth, 0))
                : textLayoutManager.CreateTextResource(canvasText, DrawnTextLayout, TextFormat, RenderedText, new Size(coreTextbox.ActualWidth, canvasText.Size.Height));
            SyntaxHighlightingRenderer.UpdateSyntaxHighlighting(renderTextData, NumberOfStartLine, textManager.NewLineCharacter, DrawnTextLayout, designHelper._AppTheme, textManager._SyntaxHighlighting, coreTextbox.EnableSyntaxHighlighting, statefulSyntaxHighlightingManager, coreTextbox.syntaxHighlightingSession);
            textDecorationRenderer.ApplyForeground(
                DrawnTextLayout,
                NumberOfStartLine,
                NumberOfRenderedLines);
        }

        scrollManager.EnsureHorizontalScrollBounds(canvasText, longestLineManager, false, zoomManager.ZoomNeedsRecalculateLongestLine);
        if (zoomManager.ZoomNeedsRecalculateLongestLine)
            zoomManager.ZoomNeedsRecalculateLongestLine = false;

        if (linkHighlightManager.HighlightLinks)
        {
            linkHighlightManager.FindAndComputeLinkPositions();
            linkRenderer.HighlightLinks();
        }

        using (var ccls = canvasCommandList.CreateDrawingSession())
        {
            //Only update the textformat when the text changes:
            //render the search highlights
            if (searchManager.IsSearchOpen)
                SearchHighlightsRenderer.RenderHighlights(
                    args,
                    ccls,
                    DrawnTextLayout,
                    RenderedText,
                    searchManager.MatchingSearchLines,
                    searchManager.searchParameter.SearchExpression,
                    (float)-scrollManager.HorizontalScroll,
                    BackgroundVerticalOffset,
                    designHelper._Design.SearchHighlightColor
                    );

            ccls.DrawTextLayout(
                DrawnTextLayout,
                (float)-scrollManager.HorizontalScroll,
                TextVerticalOffset,
                designHelper.TextColorBrush);

            invisibleCharactersRenderer.DrawTabsAndSpaces(
                args,
                ccls,
                RenderedText,
                DrawnTextLayout,
                TextVerticalOffset);
        }
        args.DrawingSession.DrawImage(canvasCommandList);
        canvasUpdateManager.UpdateBackground();
        if ((IsWordWrapEnabled || previousWrappedViewport.HasValue)
            && (layoutChanged || previousWrappedViewport != WrappedViewport))
        {
            // These canvases can draw before the text canvas updates the shared viewport.
            canvasUpdateManager.UpdateSelection();
            canvasUpdateManager.UpdateCursor();
        }

        //Only update if needed, to reduce updates when scrolling
        if (previousWrappedViewport != WrappedViewport || lineNumberRenderer.CanUpdateCanvas())
        {
            canvasUpdateManager.UpdateLineNumbers();
        }
    }
}
