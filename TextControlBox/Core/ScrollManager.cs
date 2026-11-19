using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using TextControlBoxNS.Core.Renderer;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Helper;

namespace TextControlBoxNS.Core;

internal class ScrollManager
{

    public double _HorizontalScrollSensitivity = 1;
    public double _VerticalScrollSensitivity = 1;
    public int DefaultVerticalScrollSensitivity = 4;
    public float OldHorizontalScrollValue = 0;

    public double VerticalScroll { get => verticalScrollBar.Value; set { verticalScrollBar.Value = value < 0 ? 0 : value; canvasHelper.UpdateAll(); } }
    public double HorizontalScroll { get => horizontalScrollBar.Value; set { horizontalScrollBar.Value = value < 0 ? 0 : value; canvasHelper.UpdateAll(); } }

    public ScrollBar verticalScrollBar;
    public ScrollBar horizontalScrollBar;
    private CanvasUpdateManager canvasHelper;
    private TextRenderer textRenderer;
    private CursorManager cursorManager;
    private TextManager textManager;
    private CoreTextControlBox coreTextbox;
    private Grid scrollGrid;
    private ZoomManager zoomManager;
    public void Init(CoreTextControlBox coreTextbox, CanvasUpdateManager canvasHelper, TextManager textManager, TextRenderer textRenderer, CursorManager cursorManager, ZoomManager zoomManager, ScrollBar verticalScrollBar, ScrollBar horizontalScrollBar)
    {
        this.verticalScrollBar = coreTextbox.verticalScrollBar;
        this.horizontalScrollBar = coreTextbox.horizontalScrollBar;
        scrollGrid = coreTextbox.scrollGrid;
        this.canvasHelper = canvasHelper;
        this.textRenderer = textRenderer;
        this.cursorManager = cursorManager;
        this.textManager = textManager;
        this.coreTextbox = coreTextbox;
        this.zoomManager = zoomManager;
        verticalScrollBar.Loaded += VerticalScrollbar_Loaded;
        verticalScrollBar.Scroll += VerticalScrollBar_Scroll;
        horizontalScrollBar.Scroll += HorizontalScrollBar_Scroll;
    }

    internal void VerticalScrollbar_Loaded(object sender, RoutedEventArgs e)
    {
        verticalScrollBar.Maximum = ((textManager.LinesCount + 1) * textRenderer.SingleLineHeight - scrollGrid.ActualHeight) / DefaultVerticalScrollSensitivity;
        verticalScrollBar.ViewportSize = coreTextbox.ActualHeight;
    }
    internal void VerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
    {
        //only update when a line was scrolled
        if (textRenderer.IsScrolledToDifferentRow)
        {
            canvasHelper.UpdateAll();
        }
    }

    internal void HorizontalScrollBar_Scroll(object sender, ScrollEventArgs e)
    {
        canvasHelper.UpdateAll();
    }

    public void UpdateWhenScrolled()
    {
        canvasHelper.UpdateAll();
    }

    public void ScrollLineIntoViewIfOutside(int line, bool update = true)
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            if (textRenderer.TryGetWrappedScrollRow(line, null, out int row))
                ScrollWrappedRowIntoView(row, center: false, update: update);
        }
        else if (textRenderer.OutOfRenderedArea(line))
            ScrollLineIntoView(line, update);
    }

    public void ScrollOneLineUp(bool update = true)
    {
        verticalScrollBar.Value -= textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        if(update)
            canvasHelper.UpdateAll();
    }
    public void ScrollOneLineDown(bool update = true)
    {
        verticalScrollBar.Value += textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        if(update)
            canvasHelper.UpdateAll();
    }

    public void ScrollLineIntoView(int line, bool update = true)
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            if (textRenderer.TryGetWrappedScrollRow(line, null, out int row))
                ScrollWrappedRowIntoView(row, center: true, update: update);
            return;
        }

        verticalScrollBar.Value = (line - textRenderer.NumberOfRenderedLines / 2) * textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        
        if(update)
            canvasHelper.UpdateAll();
    }

    public void ScrollTopIntoView(bool update = true)
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            ScrollWrappedCursorIntoView(center: false, update: update);
            return;
        }

        verticalScrollBar.Value = (cursorManager.LineNumber - 1) * textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        if(update)
            canvasHelper.UpdateAll();
    }
    public void ScrollBottomIntoView(bool update = true)
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            ScrollWrappedCursorIntoView(center: false, update: update);
            return;
        }

        verticalScrollBar.Value = (cursorManager.LineNumber - textRenderer.NumberOfRenderedLines + 1) * textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        if(update)
            canvasHelper.UpdateAll();
    }

    public void ScrollPageUp()
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            ScrollWrappedPage(-1);
            return;
        }

        cursorManager.LineNumber -= textRenderer.NumberOfRenderedLines;
        if (cursorManager.LineNumber < 0)
            cursorManager.LineNumber = 0;

        verticalScrollBar.Value -= textRenderer.NumberOfRenderedLines * textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        canvasHelper.UpdateAll();
    }


    public void ScrollPageDown()
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            ScrollWrappedPage(1);
            return;
        }

        cursorManager.LineNumber += textRenderer.NumberOfRenderedLines;
        if (cursorManager.LineNumber > textManager.LinesCount - 1)
            cursorManager.LineNumber = textManager.LinesCount - 1;
        verticalScrollBar.Value += textRenderer.NumberOfRenderedLines * textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        canvasHelper.UpdateAll();
    }

    public void UpdateScrollToShowCursor(bool update = true)
    {
        if (textRenderer.IsWordWrapEnabled)
        {
            ScrollWrappedCursorIntoView(center: false, update: update);
            return;
        }

        if (textRenderer.NumberOfStartLine + textRenderer.NumberOfRenderedLines - 1 <= cursorManager.LineNumber)
        {
            verticalScrollBar.Value =
                (cursorManager.LineNumber - textRenderer.NumberOfRenderedLines + 2) *
                textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        }
        else if (textRenderer.NumberOfStartLine > cursorManager.LineNumber)
        {
            verticalScrollBar.Value =
                cursorManager.LineNumber *
                textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        }

        if (update)
            canvasHelper.UpdateAll();
    }

    public bool ScrollIntoViewHorizontal(CanvasControl canvasText, bool update = true)
    {
        if (textRenderer.IsWordWrapEnabled)
            return false;

        float curPosInLine = CursorHelper.GetCursorPositionInLine(
            textRenderer.CurrentLineTextLayout,
            cursorManager.currentCursorPosition,
            0
        );

        if (curPosInLine == OldHorizontalScrollValue)
            return false;

        double visibleStart = horizontalScrollBar.Value;
        double visibleEnd = visibleStart + canvasText.ActualWidth;

        bool changed = false;
        if (curPosInLine < visibleStart + 3)
        {
            changed = true;
            horizontalScrollBar.Value = Math.Max(curPosInLine - 3, horizontalScrollBar.Minimum);
        }
        else if (curPosInLine > visibleEnd)
        {
            changed = true;
            horizontalScrollBar.Value = Math.Min(curPosInLine - canvasText.ActualWidth + 5, horizontalScrollBar.Maximum + 5);
        }

        OldHorizontalScrollValue = curPosInLine;

        if (update)
            canvasHelper.UpdateAll();

        return changed;
    }

    public void EnsureHorizontalScrollBounds(CanvasControl canvasText, LongestLineManager longestLineManager, bool triggeredByCursor, bool forceRecalculateLongestLine = false)
    {
        if (textRenderer.IsWordWrapEnabled)
            return;

        longestLineManager.CheckRecalculateLongestLine(forceRecalculateLongestLine);

        //Apply longest width to scrollbar
        horizontalScrollBar.ViewportSize = canvasText.ActualWidth;
        horizontalScrollBar.Maximum = (longestLineManager.longestLineWidth.Width <= canvasText.ActualWidth ? 0 : longestLineManager.longestLineWidth.Width - canvasText.ActualWidth + (zoomManager.ZoomedFontSize / 2));

        if(ScrollIntoViewHorizontal(canvasText, false))
        {
            if (triggeredByCursor)
                canvasHelper.UpdateText();
            else
                canvasHelper.UpdateCursor();
        }
    }

    public void ScrollWrappedCursorIntoView(bool center, bool update = true)
    {
        if (textRenderer.TryGetWrappedScrollRow(cursorManager.LineNumber, cursorManager.CharacterPosition, out int row))
            ScrollWrappedRowIntoView(row, center, update);
    }

    private void ScrollWrappedRowIntoView(int row, bool center, bool update)
    {
        double rowStep = (double)textRenderer.SingleLineHeight / DefaultVerticalScrollSensitivity;
        int first = textRenderer.GetScrollRow();
        int pageSize = Math.Max(1, (int)(textRenderer.ViewportHeight / textRenderer.SingleLineHeight));
        int target = first;
        if (center)
            target = row - pageSize / 2;
        else if (row < first)
            target = row;
        else if (row >= first + pageSize)
            target = row - pageSize + 1;

        verticalScrollBar.Value = Math.Max(0, target) * rowStep;
        if (update)
            canvasHelper.UpdateAll();
    }

    private void ScrollWrappedPage(int direction)
    {
        double page = textRenderer.ViewportHeight / DefaultVerticalScrollSensitivity;
        verticalScrollBar.Value += direction * page;
        int line = textRenderer.GetWrappedLineAt(direction < 0 ? 0 : textRenderer.ViewportHeight - 1);
        if (line >= 0)
        {
            cursorManager.LineNumber = line;
            cursorManager.CharacterPosition = Math.Min(cursorManager.CharacterPosition, textManager.GetLineLength(line));
        }
        canvasHelper.UpdateAll();
    }
}
