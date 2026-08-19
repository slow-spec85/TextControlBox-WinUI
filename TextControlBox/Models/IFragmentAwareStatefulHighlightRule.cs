using System;

namespace TextControlBoxNS;

/// <summary>
/// Extends a stateful syntax rule with support for independently rendered document fragments.
/// </summary>
/// <remarks>
/// Fragment inference is useful for projections such as diffs, logs, or excerpts where omitted
/// lines can contain a delimiter that would otherwise determine the state of the first visible
/// line. Rules that do not implement this interface continue to use
/// <see cref="IStatefulHighlightRule.InitialState"/> at fragment boundaries.
/// </remarks>
public interface IFragmentAwareStatefulHighlightRule : IStatefulHighlightRule
{
    /// <summary>
    /// Infers the state immediately before the first line of an independent fragment.
    /// </summary>
    /// <param name="lines">The visible lines in the fragment, excluding boundary lines.</param>
    /// <returns>The state to use before the fragment's first line.</returns>
    int InferInitialState(ReadOnlySpan<string> lines);
}
