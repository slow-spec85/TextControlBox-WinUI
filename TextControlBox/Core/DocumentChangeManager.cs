using System;
using System.Collections.Generic;
using TextControlBoxNS.Models;

namespace TextControlBoxNS.Core;

internal sealed class DocumentChangeManager
{
    private readonly List<DocumentChange> pendingChanges = [];
    private EventsManager eventsManager;
    private int batchDepth;
    private DocumentChangeReason batchReason;

    public long Version { get; private set; }

    public void Init(EventsManager manager)
    {
        eventsManager = manager;
    }

    public DocumentChangeBatch BeginBatch(DocumentChangeReason reason)
    {
        if (batchDepth == 0)
            batchReason = reason;

        var batch = new DocumentChangeBatch(this);
        batchDepth++;
        return batch;
    }

    public void RecordChange(int startLine, int removedLineCount, int insertedLineCount)
    {
        if (removedLineCount == 0 && insertedLineCount == 0)
            return;

        var change = new DocumentChange(startLine, removedLineCount, insertedLineCount);
        AppendOrMerge(change);

        if (batchDepth == 0)
            Flush(DocumentChangeReason.Edit);
    }

    internal void EndBatch()
    {
        if (batchDepth <= 0)
            throw new InvalidOperationException("No document change batch is active.");

        batchDepth--;
        if (batchDepth == 0)
            Flush(batchReason);
    }

    private void AppendOrMerge(DocumentChange change)
    {
        if (pendingChanges.Count == 0)
        {
            pendingChanges.Add(change);
            return;
        }

        DocumentChange previous = pendingChanges[^1];

        // A remove followed by an insert at the same position is one replacement.
        if (previous.StartLine == change.StartLine
            && previous.InsertedLineCount == 0
            && change.RemovedLineCount == 0)
        {
            pendingChanges[^1] = new DocumentChange(
                previous.StartLine,
                previous.RemovedLineCount,
                change.InsertedLineCount);
            return;
        }

        // Adjacent one-for-one line edits can be represented as one larger replacement.
        if (previous.RemovedLineCount == previous.InsertedLineCount
            && change.RemovedLineCount == change.InsertedLineCount
            && change.StartLine == previous.StartLine + previous.InsertedLineCount)
        {
            pendingChanges[^1] = new DocumentChange(
                previous.StartLine,
                checked(previous.RemovedLineCount + change.RemovedLineCount),
                checked(previous.InsertedLineCount + change.InsertedLineCount));
            return;
        }

        pendingChanges.Add(change);
    }

    private void Flush(DocumentChangeReason reason)
    {
        if (pendingChanges.Count == 0)
            return;

        Version = checked(Version + 1);
        DocumentChange[] changes = [.. pendingChanges];
        pendingChanges.Clear();
        eventsManager.CallDocumentChanged(new DocumentChangedEventArgs(Version, reason, changes));
    }
}

internal sealed class DocumentChangeBatch : IDisposable
{
    private DocumentChangeManager manager;

    public DocumentChangeBatch(DocumentChangeManager changeManager)
    {
        manager = changeManager;
    }

    public void Dispose()
    {
        if (manager is null)
            return;

        DocumentChangeManager activeManager = manager;
        manager = null;
        activeManager.EndBatch();
    }
}
