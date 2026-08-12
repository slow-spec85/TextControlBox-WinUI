using System;
using Windows.UI;

namespace TextControlBoxNS.Models;

/// <summary>
/// Describes a background decoration for an inclusive, zero-based range of document lines.
/// </summary>
public sealed class LineDecoration
{
    /// <summary>
    /// Initializes a line decoration.
    /// </summary>
    /// <param name="startLine">The first decorated line, using a zero-based index.</param>
    /// <param name="endLine">The last decorated line, using a zero-based inclusive index.</param>
    /// <param name="backgroundColor">The color painted behind the line contents.</param>
    /// <param name="priority">
    /// The overlap priority. Decorations with a greater value are painted later.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either line index is negative or <paramref name="endLine"/> precedes
    /// <paramref name="startLine"/>.
    /// </exception>
    public LineDecoration(int startLine, int endLine, Color backgroundColor, int priority = 0)
    {
        if (startLine < 0)
            throw new ArgumentOutOfRangeException(nameof(startLine));

        if (endLine < startLine)
            throw new ArgumentOutOfRangeException(nameof(endLine));

        StartLine = startLine;
        EndLine = endLine;
        BackgroundColor = backgroundColor;
        Priority = priority;
    }

    /// <summary>
    /// Gets the first decorated line, using a zero-based index.
    /// </summary>
    public int StartLine { get; }

    /// <summary>
    /// Gets the last decorated line, using a zero-based inclusive index.
    /// </summary>
    public int EndLine { get; }

    /// <summary>
    /// Gets the color painted behind the line contents.
    /// </summary>
    public Color BackgroundColor { get; }

    /// <summary>
    /// Gets the overlap priority. Decorations with a greater value are painted later.
    /// </summary>
    public int Priority { get; }
}
