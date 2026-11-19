using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Linq;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Renderer;
using TextControlBoxNS.Helper;
using Windows.UI;

namespace TextControlBox.Tests;

[TestClass]
public class WrappedLineLayoutTests
{
    [UITestMethod]
    public void CurrentLineOverlayUsesSelectedItemColorAndCanRestoreTheDefault()
    {
        Border selectedItem = (Border)XamlReader.Load("""
            <Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    Background="{ThemeResource ListViewItemBackgroundSelected}" />
            """);
        SolidColorBrush selectedBackground = (SolidColorBrush)selectedItem.Background;

        CoreTextControlBox editor = TestHelper.MakeCoreTextbox();
        editor.RequestedTheme = ElementTheme.Dark;
        DesignHelper design = editor.designHelper;
        using CanvasDevice device = new();
        using CanvasRenderTarget target = new(device, 16, 16, 96);

        design.LineHighlighterColorOverride = selectedBackground.Color;
        design.CreateColorResources(target);
        Assert.AreEqual(selectedBackground.Color, design.LineHighlighterBrush.Color);

        editor.RequestedTheme = ElementTheme.Light;
        design.CreateColorResources(target);
        Assert.AreEqual(selectedBackground.Color, design.LineHighlighterBrush.Color);

        design.LineHighlighterColorOverride = null;
        design.CreateColorResources(target);
        Assert.AreEqual(design.LightDesign.LineHighlighterColor, design.LineHighlighterBrush.Color);
    }

    [UITestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CurrentLineOverlayCoversWrappedRowsOverDiffColors(bool darkTheme)
    {
        const int canvasWidth = 64;
        const int rowHeight = 20;
        const int gutterWidth = 8;
        using CanvasDevice device = new();
        using CanvasTextFormat format = new()
        {
            FontFamily = "Consolas",
            FontSize = 14,
            WordWrapping = CanvasWordWrapping.Wrap
        };
        WrappedLineLayout layout = new(device, format, canvasWidth - gutterWidth, ["before", new string('x', 80), "after"]);
        Assert.IsGreaterThan(1, layout.GetRowCount(1));
        EditorRenderViewport viewport = new(0, 3, rowHeight, 0, layout);

        DesignHelper design = new();
        Color overlayColor = darkTheme ? design.DarkDesign.LineHighlighterColor : design.LightDesign.LineHighlighterColor;
        Color diffColor = darkTheme ? Color.FromArgb(255, 38, 94, 77) : Color.FromArgb(255, 231, 244, 231);
        Color inlineColor = darkTheme ? Color.FromArgb(255, 106, 153, 85) : Color.FromArgb(255, 183, 230, 192);
        using CanvasSolidColorBrush overlay = new(device, overlayColor);
        using CanvasRenderTarget target = new(device, canvasWidth, layout.RowCount * rowHeight, 96);
        using (CanvasDrawingSession drawingSession = target.CreateDrawingSession())
        {
            drawingSession.Clear(diffColor);
            drawingSession.FillRectangle(32, 0, 32, (float)target.Size.Height, inlineColor);
            LineHighlighterRenderer renderer = new();
            renderer.Render(drawingSession, viewport, 1, canvasWidth, 1, overlay);
        }

        Color[] pixels = target.GetPixelColors();
        int firstRow = layout.GetStartRow(1);
        int nextLine = layout.GetStartRow(2);
        for (int row = 0; row < layout.RowCount; row++)
        {
            int offset = (row * rowHeight + rowHeight / 2) * canvasWidth;
            Color linePixel = pixels[offset + 16];
            Color inlinePixel = pixels[offset + 48];
            Assert.AreEqual(linePixel, pixels[offset]);
            Assert.AreEqual(inlinePixel, pixels[offset + canvasWidth - 1]);
            if (row >= firstRow && row < nextLine)
            {
                Assert.AreNotEqual(diffColor, linePixel);
                Assert.AreNotEqual(inlineColor, inlinePixel);
                Assert.AreNotEqual(linePixel, inlinePixel);
            }
            else
            {
                Assert.AreEqual(diffColor, linePixel);
                Assert.AreEqual(inlineColor, inlinePixel);
            }
        }
    }

    [UITestMethod]
    public void CaretUsesVisualRowTopIncludingAfterScroll()
    {
        using CanvasDevice device = new();
        CoreTextControlBox editor = TestHelper.MakeCoreTextbox();
        using CanvasTextFormat format = editor.textLayoutManager.CreateCanvasTextFormat();
        format.WordWrapping = CanvasWordWrapping.Wrap;
        using CanvasTextLayout layout = new(device, new string('x', 80), format, 60, 0);
        float rowHeight = format.LineSpacing;
        float firstRowY = rowHeight / 4;
        int continuation = layout.LineMetrics[0].CharacterCount;
        int secondContinuation = continuation + layout.LineMetrics[1].CharacterCount;

        Assert.AreEqual((double)firstRowY, TextRenderer.GetWrappedCaretPosition(layout, 0, firstRowY).Y);
        Assert.AreEqual((double)(firstRowY + rowHeight), TextRenderer.GetWrappedCaretPosition(layout, continuation, firstRowY).Y);
        Assert.AreEqual((double)(firstRowY + rowHeight * 2), TextRenderer.GetWrappedCaretPosition(layout, secondContinuation, firstRowY).Y);
        Assert.AreEqual((double)firstRowY, TextRenderer.GetWrappedCaretPosition(layout, secondContinuation, firstRowY - rowHeight * 2).Y);
    }

    [UITestMethod]
    public void ContinuationsMapToOriginalLineAndHaveNoLineNumber()
    {
        using CanvasDevice device = new();
        using CanvasTextFormat format = new()
        {
            FontFamily = "Consolas",
            FontSize = 14,
            WordWrapping = CanvasWordWrapping.Wrap
        };
        string[] lines = ["a", new string('x', 80), "", "b"];
        WrappedLineLayout layout = new(device, format, 60, lines);

        int longLineRows = layout.GetRowCount(1);
        Assert.IsGreaterThan(1, longLineRows);
        Assert.AreEqual(1, layout.GetStartRow(1));
        Assert.AreEqual(1, layout.GetLineAtRow(layout.GetStartRow(2) - 1));
        Assert.AreEqual(2, layout.GetLineAtRow(layout.GetStartRow(2)));
        Assert.AreEqual(1, layout.GetRowCount(2));
        Assert.AreEqual(3, layout.GetLineAtRow(layout.RowCount - 1));
        using CanvasTextLayout rendered = new(device, string.Join("\n", lines), format, 60, 0);
        Assert.AreEqual(rendered.LineCount, layout.RowCount);

        CoreTextControlBox editor = TestHelper.MakeCoreTextbox();
        editor.lineNumberManager.SetCustomLabels(["10", "11", "", "13"]);
        editor.lineNumberRenderer.GenerateLineNumberText(lines.Length, 0, layout);
        string[] labels = editor.lineNumberRenderer.LineNumberTextToRender.Split(Environment.NewLine);

        Assert.AreEqual("11", labels[layout.GetStartRow(1)]);
        Assert.IsTrue(labels.Skip(layout.GetStartRow(1) + 1).Take(longLineRows - 1).All(string.IsNullOrEmpty));
        Assert.AreEqual("", labels[layout.GetStartRow(2)]);
        Assert.AreEqual("13", labels[layout.GetStartRow(3)]);

        WrappedLineLayout wideLayout = new(device, format, 2000, lines);
        Assert.AreEqual(lines.Length, wideLayout.RowCount);
    }

    [UITestMethod]
    public void WordWrapIsLimitedToReadOnlyModeAndPreservesTextAndSelection()
    {
        TextControlBoxNS.TextControlBox editor = TestHelper.MakeTextbox();
        string document = editor.GetText();
        editor.SetSelection(0, 4);
        string selection = editor.SelectedText;
        Assert.AreEqual("Line", selection);

        editor.IsWordWrapEnabled = true;
        Assert.IsFalse(editor.IsWordWrapEnabled);

        editor.IsReadOnly = true;
        editor.IsWordWrapEnabled = true;
        Assert.IsTrue(editor.IsWordWrapEnabled);
        Assert.AreEqual(document, editor.GetText());
        Assert.AreEqual(selection, editor.SelectedText);

        editor.IsReadOnly = false;
        Assert.IsFalse(editor.IsWordWrapEnabled);
    }
}
