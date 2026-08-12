using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using TextControlBoxNS.Core.Text;
using Windows.Foundation;

namespace TextControlBoxNS.Core.Renderer;

internal readonly record struct RenderedTextRangeDecoration(
    int Start,
    int Length,
    Windows.UI.Color? ForegroundColor,
    Windows.UI.Color? BackgroundColor,
    Windows.UI.Color? BorderColor,
    float BorderThickness,
    float CornerRadius,
    float HorizontalPadding);

internal sealed class TextDecorationRenderer
{
    private readonly List<ResolvedTextRangeDecoration> visibleDecorations = [];
    private readonly List<RenderedTextRangeDecoration> renderedDecorations = [];
    private int[] lineOffsets = [];
    private TextDecorationStore textDecorationStore;
    private TextManager textManager;
    private ScrollManager scrollManager;
    private long preparedRevision = -1;
    private int preparedStartLine = -1;
    private int preparedLineCount = -1;
    private string preparedLineEnding;

    public void Init(
        TextDecorationStore decorationStore,
        TextManager manager,
        ScrollManager currentScrollManager)
    {
        textDecorationStore = decorationStore;
        textManager = manager;
        scrollManager = currentScrollManager;
    }

    public void ApplyForeground(
        CanvasTextLayout textLayout,
        int firstVisibleLine,
        int visibleLineCount)
    {
        if (textLayout is null)
            return;

        Prepare(firstVisibleLine, visibleLineCount);
        foreach (RenderedTextRangeDecoration decoration in renderedDecorations)
        {
            if (decoration.ForegroundColor is { } foregroundColor)
                textLayout.SetColor(decoration.Start, decoration.Length, foregroundColor);
        }
    }

    public void DrawBackgroundAndBorder(
        CanvasDrawEventArgs args,
        TextRenderer textRenderer,
        float dpiScale,
        float horizontalOrigin)
    {
        CanvasTextLayout textLayout = textRenderer.DrawnTextLayout;
        if (textLayout is null || textRenderer.NumberOfRenderedLines <= 0)
            return;

        Prepare(textRenderer.NumberOfStartLine, textRenderer.NumberOfRenderedLines);
        float marginTop = textRenderer.SingleLineHeight
            / scrollManager.DefaultVerticalScrollSensitivity;

        foreach (RenderedTextRangeDecoration decoration in renderedDecorations)
        {
            if (!decoration.BackgroundColor.HasValue && !decoration.BorderColor.HasValue)
                continue;

            CanvasTextLayoutRegion[] regions = textLayout.GetCharacterRegions(
                decoration.Start,
                decoration.Length);
            foreach (CanvasTextLayoutRegion region in regions)
            {
                Rect bounds = CreateDecorationBounds(
                    region.LayoutBounds,
                    dpiScale,
                    horizontalOrigin + textRenderer.HorizontalOffset,
                    marginTop,
                    decoration.HorizontalPadding,
                    decoration.BorderColor.HasValue ? decoration.BorderThickness : 0);
                float cornerRadius = ClampCornerRadius(bounds, decoration.CornerRadius);

                if (decoration.BackgroundColor is { } backgroundColor)
                {
                    if (cornerRadius > 0)
                    {
                        args.DrawingSession.FillRoundedRectangle(
                            bounds,
                            cornerRadius,
                            cornerRadius,
                            backgroundColor);
                    }
                    else
                    {
                        args.DrawingSession.FillRectangle(bounds, backgroundColor);
                    }
                }

                if (decoration.BorderColor is { } borderColor)
                {
                    Rect borderBounds = CreateBorderBounds(bounds, decoration.BorderThickness);
                    float borderRadius = Math.Max(
                        0,
                        cornerRadius - (decoration.BorderThickness / 2));
                    if (borderRadius > 0)
                    {
                        args.DrawingSession.DrawRoundedRectangle(
                            borderBounds,
                            borderRadius,
                            borderRadius,
                            borderColor,
                            decoration.BorderThickness);
                    }
                    else
                    {
                        args.DrawingSession.DrawRectangle(
                            borderBounds,
                            borderColor,
                            decoration.BorderThickness);
                    }
                }
            }
        }
    }

    internal static Rect CreateDecorationBounds(
        Rect layoutBounds,
        float dpiScale,
        float horizontalOffset,
        float verticalOffset,
        float horizontalPadding,
        float borderThickness)
    {
        // The stroke is centered on its path. Expand the outer bounds by the full
        // stroke thickness so HorizontalPadding remains clear space between the
        // glyph and the inside edge of the border on both sides.
        float horizontalOutset = horizontalPadding + borderThickness;
        Rect paddedBounds = new(
            layoutBounds.X - horizontalOutset,
            layoutBounds.Y,
            layoutBounds.Width + (horizontalOutset * 2),
            layoutBounds.Height);
        return CreatePixelAlignedDecorationBounds(
            paddedBounds,
            dpiScale,
            horizontalOffset,
            verticalOffset);
    }

    private static Rect CreatePixelAlignedDecorationBounds(
        Rect bounds,
        float dpiScale,
        float horizontalOffset,
        float verticalOffset)
    {
        double scale = dpiScale > 0 && float.IsFinite(dpiScale) ? dpiScale : 1;
        double left = AlignToNearestPixel(bounds.Left + horizontalOffset, scale);
        double top = AlignToNearestPixel(bounds.Top + verticalOffset, scale);
        double right = AlignToNearestPixel(bounds.Right + horizontalOffset, scale);
        double bottom = AlignToNearestPixel(bounds.Bottom + verticalOffset, scale);

        return new Rect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }

    private static double AlignToNearestPixel(double value, double scale)
    {
        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }

    internal static float ClampCornerRadius(Rect bounds, float cornerRadius)
    {
        return (float)Math.Min(
            cornerRadius,
            Math.Min(bounds.Width, bounds.Height) / 2);
    }

    internal static Rect CreateBorderBounds(Rect bounds, float borderThickness)
    {
        double inset = borderThickness / 2;
        return new Rect(
            bounds.X + inset,
            bounds.Y + inset,
            Math.Max(0, bounds.Width - borderThickness),
            Math.Max(0, bounds.Height - borderThickness));
    }

    internal RenderedTextRangeDecoration[] GetRenderedDecorations(
        int firstVisibleLine,
        int visibleLineCount)
    {
        Prepare(firstVisibleLine, visibleLineCount);
        return [.. renderedDecorations];
    }

    private void Prepare(int firstVisibleLine, int visibleLineCount)
    {
        if (preparedRevision == textDecorationStore.Revision
            && preparedStartLine == firstVisibleLine
            && preparedLineCount == visibleLineCount
            && string.Equals(preparedLineEnding, textManager.NewLineCharacter, StringComparison.Ordinal))
        {
            return;
        }

        preparedRevision = textDecorationStore.Revision;
        preparedStartLine = firstVisibleLine;
        preparedLineCount = visibleLineCount;
        preparedLineEnding = textManager.NewLineCharacter;
        renderedDecorations.Clear();

        if (firstVisibleLine < 0 || visibleLineCount <= 0)
            return;

        int lastVisibleLine = checked(firstVisibleLine + visibleLineCount - 1);
        textDecorationStore.AppendVisibleDecorations(
            firstVisibleLine,
            lastVisibleLine,
            visibleDecorations);
        if (visibleDecorations.Count == 0)
            return;

        if (lineOffsets.Length < visibleLineCount)
            lineOffsets = new int[visibleLineCount];

        int renderedIndex = 0;
        for (int lineOffset = 0; lineOffset < visibleLineCount; lineOffset++)
        {
            lineOffsets[lineOffset] = renderedIndex;
            renderedIndex = checked(
                renderedIndex
                + textManager.GetLineLength(firstVisibleLine + lineOffset)
                + (lineOffset + 1 < visibleLineCount ? textManager.NewLineCharacter.Length : 0));
        }

        foreach (ResolvedTextRangeDecoration decoration in visibleDecorations)
        {
            int start = checked(
                lineOffsets[decoration.Line - firstVisibleLine]
                + decoration.StartColumn);
            renderedDecorations.Add(new RenderedTextRangeDecoration(
                start,
                decoration.Length,
                decoration.ForegroundColor,
                decoration.BackgroundColor,
                decoration.BorderColor,
                decoration.BorderThickness,
                decoration.CornerRadius,
                decoration.HorizontalPadding));
        }
    }
}
