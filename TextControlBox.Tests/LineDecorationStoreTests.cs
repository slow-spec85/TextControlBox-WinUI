using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBox.Tests;

[TestClass]
public class LineDecorationStoreTests
{
    private static readonly Color FirstColor = Color.FromArgb(255, 10, 20, 30);
    private static readonly Color SecondColor = Color.FromArgb(255, 40, 50, 60);

    [TestMethod]
    public void VisibleDecorations_AreClippedToViewport()
    {
        var store = CreateStore();
        store.SetGroup("ranges", new[]
        {
            new LineDecoration(0, 10, FirstColor),
            new LineDecoration(5, 6, SecondColor),
            new LineDecoration(15, 15, FirstColor),
        }, documentLineCount: 20);

        ResolvedLineDecoration[] visible = store.GetVisibleDecorations(5, 8);

        Assert.HasCount(2, visible);
        Assert.AreEqual((5, 8), (visible[0].StartLine, visible[0].EndLine));
        Assert.AreEqual((5, 6), (visible[1].StartLine, visible[1].EndLine));
    }

    [TestMethod]
    public void VisibleDecorations_AreReturnedInStablePaintOrder()
    {
        var store = CreateStore();
        store.SetGroup("low", new[] { new LineDecoration(2, 2, FirstColor, priority: -1) }, 5);
        store.SetGroup("high", new[] { new LineDecoration(2, 2, SecondColor, priority: 10) }, 5);
        store.SetGroup("latest", new[] { new LineDecoration(2, 2, FirstColor, priority: 10) }, 5);

        ResolvedLineDecoration[] visible = store.GetVisibleDecorations(2, 2);

        Assert.HasCount(3, visible);
        Assert.AreEqual(-1, visible[0].Priority);
        Assert.AreEqual(10, visible[1].Priority);
        Assert.AreEqual(SecondColor, visible[1].BackgroundColor);
        Assert.AreEqual(FirstColor, visible[2].BackgroundColor);
    }

    [TestMethod]
    public void InvalidLineIndexes_AreRejectedWithoutReplacingExistingGroup()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new LineDecoration(-1, 0, FirstColor));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new LineDecoration(2, 1, FirstColor));

        var store = CreateStore();
        store.SetGroup("group", new[] { new LineDecoration(0, 0, FirstColor) }, 2);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            store.SetGroup("group", new[] { new LineDecoration(2, 2, SecondColor) }, 2));

        ResolvedLineDecoration[] visible = store.GetVisibleDecorations(0, 1);
        Assert.HasCount(1, visible);
        Assert.AreEqual(FirstColor, visible[0].BackgroundColor);
    }

    [TestMethod]
    public void SetGroup_ReplacesAtomically_AndRemoveGroupInvalidatesOnce()
    {
        int invalidationCount = 0;
        var store = new LineDecorationStore();
        store.Init(() => invalidationCount++);
        store.SetGroup("group", new[] { new LineDecoration(0, 0, FirstColor) }, 4);
        store.SetGroup("group", new[] { new LineDecoration(3, 3, SecondColor) }, 4);

        Assert.IsEmpty(store.GetVisibleDecorations(0, 2));
        Assert.HasCount(1, store.GetVisibleDecorations(3, 3));
        Assert.IsTrue(store.RemoveGroup("group"));
        Assert.IsFalse(store.RemoveGroup("group"));
        Assert.AreEqual(3, invalidationCount);
    }

    [TestMethod]
    public void InsertingLines_ShiftsOrExpandsRanges()
    {
        var store = CreateStore();
        store.SetGroup("group", new[] { new LineDecoration(2, 4, FirstColor) }, 8);

        store.OnLinesInserted(index: 2, count: 2);
        ResolvedLineDecoration[] shiftedDecorations = store.GetVisibleDecorations(0, 10);
        Assert.HasCount(1, shiftedDecorations);
        ResolvedLineDecoration shifted = shiftedDecorations[0];
        Assert.AreEqual((4, 6), (shifted.StartLine, shifted.EndLine));

        store.OnLinesInserted(index: 5, count: 1);
        ResolvedLineDecoration[] expandedDecorations = store.GetVisibleDecorations(0, 10);
        Assert.HasCount(1, expandedDecorations);
        ResolvedLineDecoration expanded = expandedDecorations[0];
        Assert.AreEqual((4, 7), (expanded.StartLine, expanded.EndLine));
    }

    [TestMethod]
    public void RemovingLines_ShrinksRanges_AndRemovesFullyDeletedRanges()
    {
        var store = CreateStore();
        store.SetGroup("group", new[]
        {
            new LineDecoration(2, 6, FirstColor),
            new LineDecoration(4, 5, SecondColor),
            new LineDecoration(8, 9, SecondColor),
        }, 12);

        store.OnLinesRemoved(index: 4, count: 2);
        ResolvedLineDecoration[] remaining = store.GetVisibleDecorations(0, 10);

        Assert.HasCount(2, remaining);
        Assert.AreEqual((2, 4), (remaining[0].StartLine, remaining[0].EndLine));
        Assert.AreEqual((6, 7), (remaining[1].StartLine, remaining[1].EndLine));
    }

    [TestMethod]
    public void TextManager_LineMutationsUpdateDecorations()
    {
        EditorTestContext editor = EditorTestContext.Create("zero", "one", "two", "three");
        LineDecorationStore store = editor.LineDecorations;
        TextManager textManager = editor.TextManager;
        store.SetGroup("group", new[] { new LineDecoration(1, 2, FirstColor) }, 4);

        textManager.InsertOrAdd(1, "inserted");
        textManager.DeleteAt(2);

        ResolvedLineDecoration[] decorations =
            store.GetVisibleDecorations(0, textManager.LinesCount - 1);
        Assert.HasCount(1, decorations);
        ResolvedLineDecoration decoration = decorations[0];
        Assert.AreEqual((2, 2), (decoration.StartLine, decoration.EndLine));
    }

    [TestMethod]
    public void GetLinesForRendering_JoinsRequestedSpanOnNet8()
    {
        TextManager textManager = EditorTestContext
            .Create("zero", "one", "two", "three")
            .TextManager;

        var result = textManager.GetLinesForRendering(1, 2);

        Assert.AreEqual($"one{textManager.NewLineCharacter}two", result.Text);
        Assert.AreEqual(2, result.Lines.Length);
    }

    private static LineDecorationStore CreateStore()
    {
        return EditorTestContext.CreateLineDecorationStore();
    }
}
