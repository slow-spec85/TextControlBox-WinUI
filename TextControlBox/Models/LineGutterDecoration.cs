using System;
using Windows.UI;

namespace TextControlBoxNS.Models;

/// <summary>
/// Describes text and colors rendered in the line gutter for one document line.
/// </summary>
public sealed class LineGutterDecoration
{
    /// <summary>
    /// Initializes a line gutter decoration.
    /// </summary>
    /// <param name="line">The zero-based document line.</param>
    /// <param name="text">The single-line marker text. An empty value is allowed.</param>
    /// <param name="foregroundColor">The marker text color.</param>
    /// <param name="backgroundColor">An optional gutter background color for the line.</param>
    /// <param name="priority">
    /// The overlap priority. Decorations with a greater value are applied later.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="line"/> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> contains a line break.
    /// </exception>
    public LineGutterDecoration(
        int line,
        string text,
        Color foregroundColor,
        Color? backgroundColor = null,
        int priority = 0)
    {
        if (line < 0)
            throw new ArgumentOutOfRangeException(nameof(line));

        ArgumentNullException.ThrowIfNull(text);
        if (text.Contains('\r') || text.Contains('\n'))
        {
            throw new ArgumentException("Gutter marker text cannot contain line breaks.", nameof(text));
        }

        Line = line;
        Text = text;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        Priority = priority;
    }

    /// <summary>Gets the zero-based document line.</summary>
    public int Line { get; }

    /// <summary>Gets the single-line marker text.</summary>
    public string Text { get; }

    /// <summary>Gets the marker text color.</summary>
    public Color ForegroundColor { get; }

    /// <summary>Gets the optional gutter background color for the line.</summary>
    public Color? BackgroundColor { get; }

    /// <summary>Gets the overlap priority. Greater values are applied later.</summary>
    public int Priority { get; }
}
