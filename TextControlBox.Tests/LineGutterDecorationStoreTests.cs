using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TextControlBoxNS.Core;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBox.Tests;

[TestClass]
public class LineGutterDecorationStoreTests
{
    private static readonly Color FirstColor = Color.FromArgb(255, 10, 20, 30);
    private static readonly Color SecondColor = Color.FromArgb(255, 40, 50, 60);

    [TestMethod]
    public void Constructor_RejectsInvalidLineAndMultilineText()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new LineGutterDecoration(-1, "+", FirstColor));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new LineGutterDecoration(0, null!, FirstColor));
        Assert.ThrowsExactly<ArgumentException>(() =>
            new LineGutterDecoration(0, "+\r\n-", FirstColor));
    }

    [TestMethod]
    public void VisibleDecorations_AreFilteredByViewport()
    {
        LineGutterDecorationStore store = CreateStore();
        store.SetGroup("markers", new[]
        {
            new LineGutterDecoration(0, "-", FirstColor),
            new LineGutterDecoration(2, "+", SecondColor),
            new LineGutterDecoration(4, "+", FirstColor),
        }, documentLineCount: 5);

        ResolvedLineGutterDecoration[] visible = store.GetVisibleDecorations(1, 3);

        Assert.HasCount(1, visible);
        Assert.AreEqual(2, visible[0].Line);
        Assert.AreEqual("+", visible[0].Text);
    }

    [TestMethod]
    public void VisibleDecorations_AreReturnedInStablePaintOrder()
    {
        LineGutterDecorationStore store = CreateStore();
        store.SetGroup("low", new[]
        {
            new LineGutterDecoration(1, "low", FirstColor, priority: -1),
        }, 3);
        store.SetGroup("high", new[]
        {
            new LineGutterDecoration(1, "high", SecondColor, priority: 10),
        }, 3);
        store.SetGroup("latest", new[]
        {
            new LineGutterDecoration(1, "latest", FirstColor, priority: 10),
        }, 3);

        ResolvedLineGutterDecoration[] visible = store.GetVisibleDecorations(1, 1);

        Assert.HasCount(3, visible);
        Assert.AreEqual("low", visible[0].Text);
        Assert.AreEqual("high", visible[1].Text);
        Assert.AreEqual("latest", visible[2].Text);
    }

    [TestMethod]
    public void InvalidReplacement_DoesNotChangeExistingGroup()
    {
        LineGutterDecorationStore store = CreateStore();
        store.SetGroup("markers", new[]
        {
            new LineGutterDecoration(0, "+", FirstColor),
        }, documentLineCount: 2);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            store.SetGroup("markers", new[]
            {
                new LineGutterDecoration(2, "-", SecondColor),
            }, documentLineCount: 2));

        ResolvedLineGutterDecoration[] remaining = store.GetVisibleDecorations(0, 1);
        Assert.HasCount(1, remaining);
        Assert.AreEqual("+", remaining[0].Text);
        Assert.AreEqual(FirstColor, remaining[0].ForegroundColor);
    }

    [TestMethod]
    public void GroupReplacementRemovalAndClear_InvalidateOnlyForChanges()
    {
        int invalidationCount = 0;
        var store = new LineGutterDecorationStore();
        store.Init(() => invalidationCount++);

        store.SetGroup("markers", new[]
        {
            new LineGutterDecoration(0, "+", FirstColor),
        }, 3);
        store.SetGroup("markers", new[]
        {
            new LineGutterDecoration(2, "-", SecondColor),
        }, 3);

        Assert.IsEmpty(store.GetVisibleDecorations(0, 1));
        Assert.HasCount(1, store.GetVisibleDecorations(2, 2));
        Assert.IsTrue(store.RemoveGroup("markers"));
        Assert.IsFalse(store.RemoveGroup("markers"));
        Assert.IsFalse(store.HasDecorations);

        store.SetGroup("markers", new[]
        {
            new LineGutterDecoration(1, "+", FirstColor),
        }, 3);
        store.Clear();
        store.Clear();

        Assert.AreEqual(5, invalidationCount);
        Assert.IsFalse(store.HasDecorations);
    }

    [TestMethod]
    public void EmptyReplacement_RemovesGroupAndReleasesGutterState()
    {
        LineGutterDecorationStore store = CreateStore();
        store.SetGroup("markers", new[]
        {
            new LineGutterDecoration(0, "+", FirstColor),
        }, 1);

        store.SetGroup(
            "markers",
            Array.Empty<LineGutterDecoration>(),
            documentLineCount: 1);

        Assert.IsFalse(store.HasDecorations);
        Assert.IsEmpty(store.GetVisibleDecorations(0, 0));
    }

    private static LineGutterDecorationStore CreateStore()
    {
        var store = new LineGutterDecorationStore();
        store.Init(static () => { });
        return store;
    }
}
