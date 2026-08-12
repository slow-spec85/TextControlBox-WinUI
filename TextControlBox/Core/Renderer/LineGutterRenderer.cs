using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Helper;
using Windows.UI;

namespace TextControlBoxNS.Core.Renderer;

internal sealed class LineGutterRenderer
{
    private readonly List<ResolvedLineGutterDecoration> visibleDecorations = [];
    private readonly StringBuilder markerText = new();
    private ResolvedLineGutterDecoration?[] resolvedLines = [];
    private Color?[] resolvedBackgrounds = [];
    private int[] markerStarts = [];
    private int[] markerLengths = [];
    private CanvasTextFormat textFormat;
    private CanvasTextLayout textLayout;
    private LineGutterDecorationStore decorationStore;
    private TextManager textManager;
    private TextLayoutManager textLayoutManager;
    private ZoomManager zoomManager;
    private string formattedFontFamily;
    private float formattedFontSize = -1;
    private float formattedLineHeight = -1;

    public void Init(
        LineGutterDecorationStore store,
        TextManager currentTextManager,
        TextLayoutManager currentTextLayoutManager,
        ZoomManager currentZoomManager)
    {
        decorationStore = store;
        textManager = currentTextManager;
        textLayoutManager = currentTextLayoutManager;
        zoomManager = currentZoomManager;
    }

    public void Draw(
        CanvasControl canvas,
        CanvasDrawEventArgs args,
        EditorRenderViewport viewport,
        float gutterWidth)
    {
        if (viewport.VisibleLineCount <= 0
            || viewport.LineHeight <= 0
            || gutterWidth <= 0
            || !decorationStore.HasDecorations)
        {
            return;
        }

        EnsureBuffers(viewport.VisibleLineCount);
        Array.Clear(resolvedLines, 0, viewport.VisibleLineCount);
        Array.Clear(resolvedBackgrounds, 0, viewport.VisibleLineCount);

        decorationStore.AppendVisibleDecorations(
            viewport.FirstVisibleLine,
            viewport.LastVisibleLine,
            visibleDecorations);

        foreach (ResolvedLineGutterDecoration decoration in visibleDecorations)
        {
            int lineOffset = decoration.Line - viewport.FirstVisibleLine;
            if (decoration.BackgroundColor is { } backgroundColor)
                resolvedBackgrounds[lineOffset] = backgroundColor;

            resolvedLines[lineOffset] = decoration;
        }

        DrawBackgrounds(canvas, args, viewport, gutterWidth);

        markerText.Clear();
        for (int lineOffset = 0; lineOffset < viewport.VisibleLineCount; lineOffset++)
        {
            markerStarts[lineOffset] = markerText.Length;
            if (resolvedLines[lineOffset] is { } decoration)
                markerText.Append(decoration.Text);

            markerLengths[lineOffset] = markerText.Length - markerStarts[lineOffset];
            if (lineOffset + 1 < viewport.VisibleLineCount)
                markerText.Append(textManager.NewLineCharacter);
        }

        EnsureTextFormat();
        textLayout?.Dispose();
        textLayout = textLayoutManager.CreateTextLayout(
            canvas,
            textFormat,
            markerText.ToString(),
            gutterWidth,
            (float)canvas.Size.Height);

        for (int lineOffset = 0; lineOffset < viewport.VisibleLineCount; lineOffset++)
        {
            if (resolvedLines[lineOffset] is not { } decoration)
                continue;

            int markerLength = markerLengths[lineOffset];
            if (markerLength > 0)
            {
                textLayout.SetColor(
                    markerStarts[lineOffset],
                    markerLength,
                    decoration.ForegroundColor);
            }
        }

        args.DrawingSession.DrawTextLayout(
            textLayout,
            0,
            textLayoutManager.TextVerticalOffset,
            Color.FromArgb(0, 0, 0, 0));
    }

    private void DrawBackgrounds(
        CanvasControl canvas,
        CanvasDrawEventArgs args,
        EditorRenderViewport viewport,
        float gutterWidth)
    {
        int lineOffset = 0;
        while (lineOffset < viewport.VisibleLineCount)
        {
            Color? backgroundColor = resolvedBackgrounds[lineOffset];
            if (!backgroundColor.HasValue)
            {
                lineOffset++;
                continue;
            }

            int blockStart = lineOffset;
            do
            {
                lineOffset++;
            }
            while (lineOffset < viewport.VisibleLineCount
                && Nullable.Equals(resolvedBackgrounds[lineOffset], backgroundColor));

            float top = (blockStart * viewport.LineHeight) + viewport.TopOffset;
            float bottom = (lineOffset * viewport.LineHeight) + viewport.TopOffset;
            args.DrawingSession.FillRectangle(
                Utils.CreateLineAlignedRect(
                    0,
                    top,
                    gutterWidth,
                    bottom - top,
                    canvas.DpiScale),
                backgroundColor.Value);
        }
    }

    public void CheckDispose()
    {
        textLayout?.Dispose();
        textFormat?.Dispose();
    }

    private void EnsureBuffers(int visibleLineCount)
    {
        if (resolvedLines.Length >= visibleLineCount)
            return;

        resolvedLines = new ResolvedLineGutterDecoration?[visibleLineCount];
        resolvedBackgrounds = new Color?[visibleLineCount];
        markerStarts = new int[visibleLineCount];
        markerLengths = new int[visibleLineCount];
    }

    private void EnsureTextFormat()
    {
        string fontFamily = textManager._FontFamily.Source;
        float fontSize = zoomManager.ZoomedFontSize;
        float lineHeight = textLayoutManager.LineHeight;
        if (textFormat is not null
            && formattedFontSize.Equals(fontSize)
            && formattedLineHeight.Equals(lineHeight)
            && string.Equals(formattedFontFamily, fontFamily, StringComparison.Ordinal))
        {
            return;
        }

        textFormat?.Dispose();
        textFormat = textLayoutManager.CreateCanvasTextFormat(
            fontSize,
            lineHeight,
            textManager._FontFamily);
        textFormat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
        formattedFontFamily = fontFamily;
        formattedFontSize = fontSize;
        formattedLineHeight = lineHeight;
    }
}
