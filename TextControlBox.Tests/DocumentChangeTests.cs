using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Models;

namespace TextControlBox.Tests;

[TestClass]
public class DocumentChangeTests
{
    [TestMethod]
    public void ImmediateChange_IncrementsVersionAndPublishesEditReason()
    {
        var eventsManager = new EventsManager();
        var manager = new DocumentChangeManager();
        manager.Init(eventsManager);
        DocumentChangedEventArgs? received = null;
        eventsManager.DocumentChanged += args => received = args;

        manager.RecordChange(startLine: 3, removedLineCount: 1, insertedLineCount: 1);

        Assert.IsNotNull(received);
        DocumentChangedEventArgs actual = received!;
        Assert.AreEqual(1L, actual.Version);
        Assert.AreEqual(DocumentChangeReason.Edit, actual.Reason);
        Assert.HasCount(1, actual.Changes);
        Assert.AreEqual(3, actual.Changes[0].StartLine);
        Assert.AreEqual(1L, manager.Version);
    }

    [TestMethod]
    public void Batch_CoalescesRemoveAndInsertIntoReplacement()
    {
        var eventsManager = new EventsManager();
        var manager = new DocumentChangeManager();
        manager.Init(eventsManager);
        var received = new List<DocumentChangedEventArgs>();
        eventsManager.DocumentChanged += received.Add;

        using (manager.BeginBatch(DocumentChangeReason.Load))
        {
            manager.RecordChange(0, removedLineCount: 8, insertedLineCount: 0);
            manager.RecordChange(0, removedLineCount: 0, insertedLineCount: 5);
        }

        Assert.HasCount(1, received);
        Assert.AreEqual(DocumentChangeReason.Load, received[0].Reason);
        DocumentChange change = received[0].Changes[0];
        Assert.AreEqual((0, 8, 5),
            (change.StartLine, change.RemovedLineCount, change.InsertedLineCount));
    }

    [TestMethod]
    public void Batch_CoalescesAdjacentLineReplacements()
    {
        var eventsManager = new EventsManager();
        var manager = new DocumentChangeManager();
        manager.Init(eventsManager);
        DocumentChangedEventArgs? received = null;
        eventsManager.DocumentChanged += args => received = args;

        using (manager.BeginBatch(DocumentChangeReason.Edit))
        {
            manager.RecordChange(4, 1, 1);
            manager.RecordChange(5, 1, 1);
            manager.RecordChange(6, 1, 1);
        }

        Assert.IsNotNull(received);
        DocumentChangedEventArgs actual = received!;
        Assert.HasCount(1, actual.Changes);
        Assert.AreEqual((4, 3, 3),
            (actual.Changes[0].StartLine,
             actual.Changes[0].RemovedLineCount,
             actual.Changes[0].InsertedLineCount));
    }

    [UITestMethod]
    public void TextManager_NoOpLineReplacementDoesNotPublishChange()
    {
        TextManager textManager = CreateTextManager(out EventsManager eventsManager);
        textManager.AddLine("same");
        int eventCount = 0;
        eventsManager.DocumentChanged += _ => eventCount++;

        textManager.SetLineText(0, "same");

        Assert.AreEqual(0, eventCount);
    }

    [UITestMethod]
    public void TextManager_SwapReportsOnlyChangedEndpoints()
    {
        TextManager textManager = CreateTextManager(out EventsManager eventsManager);
        textManager.InsertOrAddRange(new[] { "zero", "one", "two", "three" }, 0);
        DocumentChangedEventArgs? received = null;
        eventsManager.DocumentChanged += args => received = args;

        using (textManager.BeginDocumentChangeBatch(DocumentChangeReason.Edit))
            textManager.SwapLines(0, 3);

        Assert.IsNotNull(received);
        DocumentChangedEventArgs actual = received!;
        Assert.HasCount(2, actual.Changes);
        Assert.AreEqual((0, 1, 1),
            (actual.Changes[0].StartLine,
             actual.Changes[0].RemovedLineCount,
             actual.Changes[0].InsertedLineCount));
        Assert.AreEqual((3, 1, 1),
            (actual.Changes[1].StartLine,
             actual.Changes[1].RemovedLineCount,
             actual.Changes[1].InsertedLineCount));
    }

    [UITestMethod]
    public void LoadLines_PublishesSingleLoadReplacement()
    {
        CoreTextControlBox core = TestHelper.MakeCoreTextbox(3);
        DocumentChangedEventArgs? received = null;
        core.eventsManager.DocumentChanged += args => received = args;

        core.LoadLines(new[] { "new zero", "new one" });

        Assert.IsNotNull(received);
        DocumentChangedEventArgs actual = received!;
        Assert.AreEqual(DocumentChangeReason.Load, actual.Reason);
        Assert.HasCount(1, actual.Changes);
        Assert.AreEqual((0, 3, 2),
            (actual.Changes[0].StartLine,
             actual.Changes[0].RemovedLineCount,
             actual.Changes[0].InsertedLineCount));
    }

    [UITestMethod]
    public void ActionGroupAndUndo_PublishOneVersionedBatchEach()
    {
        CoreTextControlBox core = TestHelper.MakeCoreTextbox(3);
        var received = new List<DocumentChangedEventArgs>();
        core.eventsManager.DocumentChanged += received.Add;

        core.ExecuteActionGroup(() =>
        {
            core.SetLineText(0, "changed zero");
            core.SetLineText(1, "changed one");
        });

        Assert.HasCount(1, received);
        Assert.AreEqual(DocumentChangeReason.Edit, received[0].Reason);
        Assert.HasCount(1, received[0].Changes);
        Assert.AreEqual((0, 2, 2),
            (received[0].Changes[0].StartLine,
             received[0].Changes[0].RemovedLineCount,
             received[0].Changes[0].InsertedLineCount));

        core.Undo();

        Assert.HasCount(2, received);
        Assert.AreEqual(DocumentChangeReason.Undo, received[1].Reason);
        Assert.AreEqual(received[0].Version + 1, received[1].Version);
    }

    [UITestMethod]
    public void PublicEvent_UsesTextControlBoxAsSender()
    {
        TextControlBoxNS.TextControlBox textBox = TestHelper.MakeTextbox(2);
        object? sender = null;
        DocumentChangedEventArgs? received = null;
        textBox.DocumentChanged += (eventSender, args) =>
        {
            sender = eventSender;
            received = args;
        };

        textBox.SetLineText(0, "public change");

        Assert.AreSame(textBox, sender);
        Assert.IsNotNull(received);
    }

    private static TextManager CreateTextManager(out EventsManager eventsManager)
    {
        EditorTestContext context = EditorTestContext.Create();
        eventsManager = context.Events;
        return context.TextManager;
    }
}
