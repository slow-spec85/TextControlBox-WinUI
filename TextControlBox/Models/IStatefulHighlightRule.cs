using System;
using System.Collections.Generic;

namespace TextControlBoxNS;

/// <summary>
/// Defines a line-oriented syntax rule whose state can continue across line boundaries.
/// </summary>
/// <remarks>
/// States are integer values owned by the rule. Implementations must be deterministic and must
/// not store document-specific state in the rule instance because a language definition can be
/// shared by multiple editor controls.
/// </remarks>
public interface IStatefulHighlightRule
{
    /// <summary>
    /// Gets the state used before the first document line.
    /// </summary>
    int InitialState { get; }

    /// <summary>
    /// Computes the state after a line without producing highlight spans.
    /// </summary>
    /// <param name="lineNumber">The zero-based document line number.</param>
    /// <param name="line">The line text without its line ending.</param>
    /// <param name="state">The state produced by the preceding line.</param>
    /// <returns>The state after processing <paramref name="line"/>.</returns>
    int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state);

    /// <summary>
    /// Adds the highlights for one line.
    /// </summary>
    /// <remarks>
    /// <see cref="HighlightSpan.Start"/> is relative to the beginning of the supplied line.
    /// Implementations must not add spans outside that line.
    /// </remarks>
    /// <param name="lineNumber">The zero-based document line number.</param>
    /// <param name="line">The line text without its line ending.</param>
    /// <param name="state">The state produced by the preceding line.</param>
    /// <param name="highlights">The destination for line-relative highlight spans.</param>
    void GetHighlights(
        int lineNumber,
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights);
}
