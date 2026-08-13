using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Renderer;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Models;
using Windows.Foundation;
using Windows.UI;

namespace TextControlBox.Tests;

[TestClass]
public class TextDecorationStoreTests
{
    private static readonly Color FirstColor = Color.FromArgb(255, 10, 20, 30);
    private static readonly Color SecondColor = Color.FromArgb(255, 40, 50, 60);

    [TestMethod]
    public void Constructor_RejectsInvalidRangesAndEmptyVisuals()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(-1, 0, 1, foregroundColor: FirstColor));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, -1, 1, foregroundColor: FirstColor));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, 0, 0, foregroundColor: FirstColor));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, int.MaxValue, 1, foregroundColor: FirstColor));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, 0, 1, borderColor: FirstColor, borderThickness: 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, 0, 1, borderColor: FirstColor) { CornerRadius = -1 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, 0, 1, borderColor: FirstColor) { CornerRadius = float.NaN });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, 0, 1, borderColor: FirstColor) { HorizontalPadding = -1 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new TextRangeDecoration(0, 0, 1, borderColor: FirstColor) { HorizontalPadding = float.PositiveInfinity });
        Assert.ThrowsExactly<ArgumentException>(() =>
            new TextRangeDecoration(0, 0, 1));
    }

    [UITestMethod]
    public void VisibleDecorations_AreFilteredAndReturnedInStablePriorityOrder()
    {
        TestContext context = CreateContext("zero", "one", "two", "three");
        context.Store.SetGroup("low", new[]
        {
            new TextRangeDecoration(1, 0, 1, foregroundColor: FirstColor, priority: -1),
            new TextRangeDecoration(3, 0, 1, foregroundColor: FirstColor),
        }, context.TextManager);
        context.Store.SetGroup("high", new[]
        {
            new TextRangeDecoration(2, 0, 1, foregroundColor: SecondColor, priority: 10),
        }, context.TextManager);
        context.Store.SetGroup("latest", new[]
        {
            new TextRangeDecoration(1, 1, 1, foregroundColor: FirstColor, priority: 10),
        }, context.TextManager);

        ResolvedTextRangeDecoration[] visible = context.Store.GetVisibleDecorations(1, 2);

        Assert.HasCount(3, visible);
        Assert.AreEqual(-1, visible[0].Priority);
        Assert.AreEqual(SecondColor, visible[1].ForegroundColor);
        Assert.AreEqual(1, visible[2].Line);
        Assert.AreEqual(1, visible[2].StartColumn);
    }

    [UITestMethod]
    public void InvalidReplacement_DoesNotChangeExistingGroup()
    {
        TestContext context = CreateContext("abc", "def");
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(0, 0, 2, backgroundColor: FirstColor),
        }, context.TextManager);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            context.Store.SetGroup("group", new[]
            {
                new TextRangeDecoration(1, 2, 2, backgroundColor: SecondColor),
            }, context.TextManager));

        ResolvedTextRangeDecoration[] decorations = context.Store.GetVisibleDecorations(0, 1);
        Assert.HasCount(1, decorations);
        ResolvedTextRangeDecoration decoration = decorations[0];
        Assert.AreEqual(FirstColor, decoration.BackgroundColor);
    }

    [UITestMethod]
    public void SetGroup_ReplacesAtomically_AndRemoveOrEmptyReplacementClearsGroup()
    {
        TestContext context = CreateContext("abc", "def");
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(0, 0, 1, foregroundColor: FirstColor),
        }, context.TextManager);
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(1, 0, 1, foregroundColor: SecondColor),
        }, context.TextManager);

        Assert.IsEmpty(context.Store.GetVisibleDecorations(0, 0));
        Assert.HasCount(1, context.Store.GetVisibleDecorations(1, 1));
        Assert.IsTrue(context.Store.RemoveGroup("group"));
        Assert.IsFalse(context.Store.RemoveGroup("group"));

        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(0, 0, 1, foregroundColor: FirstColor),
        }, context.TextManager);
        context.Store.SetGroup("group", Array.Empty<TextRangeDecoration>(), context.TextManager);
        Assert.IsEmpty(context.Store.GetVisibleDecorations(0, 1));
    }

    [UITestMethod]
    public void LineInsertionAndRemoval_ShiftAndRemoveDecorations()
    {
        TestContext context = CreateContext("zero", "one", "two", "three");
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(1, 0, 2, borderColor: FirstColor),
        }, context.TextManager);

        context.TextManager.InsertOrAdd(1, "inserted");
        ResolvedTextRangeDecoration[] decorations = context.Store.GetVisibleDecorations(0, 4);
        Assert.HasCount(1, decorations);
        ResolvedTextRangeDecoration shifted = decorations[0];
        Assert.AreEqual(2, shifted.Line);

        context.TextManager.RemoveRange(0, 1);
        decorations = context.Store.GetVisibleDecorations(0, 3);
        Assert.HasCount(1, decorations);
        shifted = decorations[0];
        Assert.AreEqual(1, shifted.Line);

        context.TextManager.DeleteAt(1);
        Assert.IsEmpty(context.Store.GetVisibleDecorations(0, 2));
    }

    [UITestMethod]
    public void LineTextShrink_ClipsOrRemovesInvalidRanges()
    {
        TestContext context = CreateContext("abcdef");
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(0, 2, 4, backgroundColor: FirstColor),
        }, context.TextManager);

        context.TextManager.SetLineText(0, "abcd");
        ResolvedTextRangeDecoration[] decorations = context.Store.GetVisibleDecorations(0, 0);
        Assert.HasCount(1, decorations);
        ResolvedTextRangeDecoration clipped = decorations[0];
        Assert.AreEqual(2, clipped.Length);

        context.TextManager.SetLineText(0, "ab");
        Assert.IsEmpty(context.Store.GetVisibleDecorations(0, 0));
    }

    [UITestMethod]
    public void SwappingLines_MovesDecorationsWithTheirText()
    {
        TestContext context = CreateContext("zero", "one", "two");
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(0, 0, 2, foregroundColor: FirstColor),
        }, context.TextManager);

        Assert.IsTrue(context.TextManager.SwapLines(0, 2));

        ResolvedTextRangeDecoration[] decorations = context.Store.GetVisibleDecorations(0, 2);
        Assert.HasCount(1, decorations);
        ResolvedTextRangeDecoration moved = decorations[0];
        Assert.AreEqual(2, moved.Line);
    }

    [UITestMethod]
    public void RenderedIndexes_AreRelativeToVisibleTextAndIncludeLineEndings()
    {
        TestContext context = CreateContext("zero", "one", "two", "three");
        context.Store.SetGroup("group", new[]
        {
            new TextRangeDecoration(0, 0, 1, foregroundColor: FirstColor),
            new TextRangeDecoration(2, 1, 2, borderColor: SecondColor)
            {
                CornerRadius = 2,
                HorizontalPadding = 1,
            },
        }, context.TextManager);
        var renderer = new TextDecorationRenderer();
        renderer.Init(context.Store, context.TextManager, new ScrollManager());

        RenderedTextRangeDecoration[] rendered = renderer.GetRenderedDecorations(
            firstVisibleLine: 1,
            visibleLineCount: 2);

        Assert.HasCount(1, rendered);
        RenderedTextRangeDecoration decoration = rendered[0];
        Assert.AreEqual("one".Length + context.TextManager.NewLineCharacter.Length + 1, decoration.Start);
        Assert.AreEqual(2, decoration.Length);
        Assert.AreEqual(2, decoration.CornerRadius);
        Assert.AreEqual(1, decoration.HorizontalPadding);
    }

    [TestMethod]
    public void BorderBounds_KeepCenteredStrokeInsideDecorationRow()
    {
        Rect bounds = new(10, 20, 30, 12);

        Rect borderBounds = TextDecorationRenderer.CreateBorderBounds(bounds, 2);

        Assert.AreEqual(11, borderBounds.X, 0.001);
        Assert.AreEqual(21, borderBounds.Y, 0.001);
        Assert.AreEqual(28, borderBounds.Width, 0.001);
        Assert.AreEqual(10, borderBounds.Height, 0.001);
    }

    [TestMethod]
    public void DecorationBounds_KeepHorizontalPaddingInsideBorder()
    {
        Rect layoutBounds = new(10.2, 20.2, 30.1, 12.1);

        Rect decorationBounds = TextDecorationRenderer.CreateDecorationBounds(
            layoutBounds,
            dpiScale: 1,
            horizontalOffset: 0,
            verticalOffset: 0,
            horizontalPadding: 1,
            borderThickness: 1);

        Assert.AreEqual(8, decorationBounds.X, 0.001);
        Assert.AreEqual(34, decorationBounds.Width, 0.001);
    }

    [TestMethod]
    public void DecorationBounds_AlignToPhysicalPixelsAtFractionalDpi()
    {
        Rect layoutBounds = new(10.2, 20.2, 30.1, 12.1);

        Rect decorationBounds = TextDecorationRenderer.CreateDecorationBounds(
            layoutBounds,
            dpiScale: 1.25f,
            horizontalOffset: 0,
            verticalOffset: 0,
            horizontalPadding: 1,
            borderThickness: 1);

        Assert.AreEqual(8, decorationBounds.X, 0.001);
        Assert.AreEqual(34.4, decorationBounds.Width, 0.001);
    }

    [TestMethod]
    public void DecorationBounds_ApplyTextViewportOriginBeforeAlignment()
    {
        Rect layoutBounds = new(0, 20, 10, 12);

        Rect decorationBounds = TextDecorationRenderer.CreateDecorationBounds(
            layoutBounds,
            dpiScale: 1,
            horizontalOffset: 24,
            verticalOffset: 0,
            horizontalPadding: 1,
            borderThickness: 1);

        Assert.AreEqual(22, decorationBounds.X, 0.001);
        Assert.AreEqual(14, decorationBounds.Width, 0.001);
    }

    [TestMethod]
    public void CornerRadius_IsLimitedByShortestDecorationSide()
    {
        Rect bounds = new(0, 0, 3, 12);

        float cornerRadius = TextDecorationRenderer.ClampCornerRadius(bounds, 4);

        Assert.AreEqual(1.5f, cornerRadius, 0.001f);
    }

    private static TestContext CreateContext(params string[] lines)
    {
        EditorTestContext editor = EditorTestContext.Create(lines);
        return new TestContext(editor.TextManager, editor.TextDecorations);
    }

    private sealed record TestContext(
        TextManager TextManager,
        TextDecorationStore Store);
}
