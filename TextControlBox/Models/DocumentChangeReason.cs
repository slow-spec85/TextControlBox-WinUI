namespace TextControlBoxNS.Models;

/// <summary>
/// Identifies the operation that produced a document change batch.
/// </summary>
public enum DocumentChangeReason
{
    /// <summary>
    /// The document was edited by the user or through an editing API.
    /// </summary>
    Edit,

    /// <summary>
    /// The document was changed by an undo operation.
    /// </summary>
    Undo,

    /// <summary>
    /// The document was changed by a redo operation.
    /// </summary>
    Redo,

    /// <summary>
    /// The document contents were replaced through a load API.
    /// </summary>
    Load,
}
