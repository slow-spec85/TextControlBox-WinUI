using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Media;
using System;
using TextControlBoxNS.Core.Text;
using Windows.Foundation;

namespace TextControlBoxNS.Core;

internal class TextLayoutManager
{
    private const float TextVerticalCenterDivisor = 1.5f;

    private TextManager textManager;
    private ZoomManager zoomManager;
    public void Init(TextManager textManager, ZoomManager zoomManager)
    {
        this.textManager = textManager;
        this.zoomManager = zoomManager;
    }

    public float LineHeight => zoomManager.ZoomedFontSize + textManager._LineSpacing;

    public float TextVerticalOffset => CalculateTextVerticalOffset(
        zoomManager.ZoomedFontSize,
        textManager._LineSpacing);

    public CanvasTextLayout CreateTextResource(ICanvasResourceCreatorWithDpi resourceCreator, CanvasTextLayout textLayout, CanvasTextFormat textFormat, string text, Size targetSize)
    {
        if (textLayout != null)
            textLayout.Dispose();
        
        textLayout = CreateTextLayout(resourceCreator, textFormat, text, targetSize);
        textLayout.Options = CanvasDrawTextOptions.EnableColorFont;

        return textLayout;
    }
    public CanvasTextFormat CreateCanvasTextFormat()
    {
        return CreateCanvasTextFormat(zoomManager.ZoomedFontSize, LineHeight, textManager._FontFamily);
    }

    public CanvasTextFormat CreateCanvasTextFormat(float zoomedFontSize, float lineSpacing, FontFamily fontFamily)
    {
        CanvasTextFormat textFormat = new CanvasTextFormat()
        {
            FontSize = zoomedFontSize,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
            WordWrapping = CanvasWordWrapping.NoWrap,
            LineSpacingMode = CanvasLineSpacingMode.Default,
            LineSpacing = lineSpacing,
        };
        textFormat.IncrementalTabStop = (float)Math.Round(zoomedFontSize * 3f); //default 137px
        textFormat.FontFamily = fontFamily.Source;
        textFormat.TrimmingGranularity = CanvasTextTrimmingGranularity.None;
        textFormat.TrimmingSign = CanvasTrimmingSign.None;
        return textFormat;
    }
    public CanvasTextLayout CreateTextLayout(ICanvasResourceCreator resourceCreator, CanvasTextFormat textFormat, string text, Size canvasSize)
    {
        return new CanvasTextLayout(resourceCreator, text, textFormat, (float)canvasSize.Width, (float)canvasSize.Height);
    }
    public CanvasTextLayout CreateTextLayout(ICanvasResourceCreator resourceCreator, CanvasTextFormat textFormat, string text, float width, float height)
    {
        return new CanvasTextLayout(resourceCreator, text, textFormat, width, height);
    }
    public CanvasTextFormat CreateLinenumberTextFormat()
    {
        CanvasTextFormat textFormat = CreateCanvasTextFormat(
            zoomManager.ZoomedFontSize,
            LineHeight,
            textManager._FontFamily);
        textFormat.HorizontalAlignment = CanvasHorizontalAlignment.Right;
        return textFormat;
    }

    internal static float CalculateTextVerticalOffset(float fontSize, float additionalLineSpacing)
    {
        return fontSize + (additionalLineSpacing / TextVerticalCenterDivisor);
    }

    public (CanvasTextLayout spaceGlyph, CanvasTextLayout tabGlyph) CreateGlyphs(ICanvasResourceCreator resourceCreator, CanvasTextFormat textFormat)
    {
        float width = zoomManager.ZoomedFontSize * 2;
        float height = zoomManager.ZoomedFontSize * 2;
        CanvasTextLayout spaceGlyph = new CanvasTextLayout(resourceCreator, "·", textFormat, width, height);
        CanvasTextLayout tabGlyph = new CanvasTextLayout(resourceCreator, "→", textFormat, width, height);

        return (spaceGlyph, tabGlyph);
    }
}
