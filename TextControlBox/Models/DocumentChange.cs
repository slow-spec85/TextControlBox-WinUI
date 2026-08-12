using System;

namespace TextControlBoxNS.Models;

/// <summary>
/// Describes one contiguous line-range replacement in a text document.
/// </summary>
public sealed class DocumentChange
{
    /// <summary>
    /// Initializes a document line-range change.
    /// </summary>
    /// <param name="startLine">The zero-based line at which the replacement starts.</param>
    /// <param name="removedLineCount">The number of lines removed from the old document.</param>
    /// <param name="insertedLineCount">The number of lines inserted into the new document.</param>
    public DocumentChange(int startLine, int removedLineCount, int insertedLineCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startLine);
        ArgumentOutOfRangeException.ThrowIfNegative(removedLineCount);
        ArgumentOutOfRangeException.ThrowIfNegative(insertedLineCount);

        if (removedLineCount == 0 && insertedLineCount == 0)
        {
            throw new ArgumentException(
                "A document change must remove or insert at least one line.");
        }

        StartLine = startLine;
        RemovedLineCount = removedLineCount;
        InsertedLineCount = insertedLineCount;
    }

    /// <summary>
    /// Gets the zero-based line at which the replacement starts.
    /// </summary>
    public int StartLine { get; }

    /// <summary>
    /// Gets the number of lines removed from the old document.
    /// </summary>
    public int RemovedLineCount { get; }

    /// <summary>
    /// Gets the number of lines inserted into the new document.
    /// </summary>
    public int InsertedLineCount { get; }
}
