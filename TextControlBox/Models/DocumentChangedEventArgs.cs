using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TextControlBoxNS.Models;

/// <summary>
/// Provides a versioned batch of incremental document line changes.
/// </summary>
public sealed class DocumentChangedEventArgs : EventArgs
{
    internal DocumentChangedEventArgs(
        long version,
        DocumentChangeReason reason,
        DocumentChange[] changes)
    {
        Version = version;
        Reason = reason;
        Changes = new ReadOnlyCollection<DocumentChange>(changes);
    }

    /// <summary>
    /// Gets the monotonically increasing document version after this batch was applied.
    /// </summary>
    public long Version { get; }

    /// <summary>
    /// Gets the operation that produced the change batch.
    /// </summary>
    public DocumentChangeReason Reason { get; }

    /// <summary>
    /// Gets the changes in the order in which they were applied.
    /// </summary>
    public IReadOnlyList<DocumentChange> Changes { get; }
}
