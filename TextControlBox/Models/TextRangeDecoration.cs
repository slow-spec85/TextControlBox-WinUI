using System;
using Windows.UI;

namespace TextControlBoxNS.Models;

/// <summary>
/// Describes visual decoration for a zero-based, end-exclusive range of characters on one line.
/// </summary>
public sealed class TextRangeDecoration
{
    private float cornerRadius;
    private float horizontalPadding;

    /// <summary>
    /// Initializes a text range decoration.
    /// </summary>
    /// <param name="line">The zero-based document line.</param>
    /// <param name="startColumn">The zero-based first decorated character column.</param>
    /// <param name="length">The number of decorated characters.</param>
    /// <param name="foregroundColor">An optional text foreground color.</param>
    /// <param name="backgroundColor">An optional color painted behind the characters.</param>
    /// <param name="borderColor">An optional color painted around the character range.</param>
    /// <param name="borderThickness">The border thickness in effective pixels.</param>
    /// <param name="priority">
    /// The overlap priority. Decorations with a greater value are applied later.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a position, length, or border thickness is invalid.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when no visual property is specified.
    /// </exception>
    public TextRangeDecoration(
        int line,
        int startColumn,
        int length,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        Color? borderColor = null,
        float borderThickness = 1,
        int priority = 0)
    {
        if (line < 0)
            throw new ArgumentOutOfRangeException(nameof(line));

        if (startColumn < 0)
            throw new ArgumentOutOfRangeException(nameof(startColumn));

        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length > int.MaxValue - startColumn)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (!float.IsFinite(borderThickness) || borderThickness <= 0)
            throw new ArgumentOutOfRangeException(nameof(borderThickness));

        if (!foregroundColor.HasValue && !backgroundColor.HasValue && !borderColor.HasValue)
            throw new ArgumentException("At least one decoration color must be specified.");

        Line = line;
        StartColumn = startColumn;
        Length = length;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        BorderColor = borderColor;
        BorderThickness = borderThickness;
        Priority = priority;
    }

    /// <summary>Gets the zero-based document line.</summary>
    public int Line { get; }

    /// <summary>Gets the zero-based first decorated character column.</summary>
    public int StartColumn { get; }

    /// <summary>Gets the number of decorated characters.</summary>
    public int Length { get; }

    /// <summary>Gets the optional text foreground color.</summary>
    public Color? ForegroundColor { get; }

    /// <summary>Gets the optional color painted behind the characters.</summary>
    public Color? BackgroundColor { get; }

    /// <summary>Gets the optional border color.</summary>
    public Color? BorderColor { get; }

    /// <summary>Gets the border thickness in effective pixels.</summary>
    public float BorderThickness { get; }

    /// <summary>
    /// Gets the uniform radius applied to background and border corners, in effective pixels.
    /// </summary>
    public float CornerRadius
    {
        get => cornerRadius;
        init
        {
            if (!float.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(CornerRadius));

            cornerRadius = value;
        }
    }

    /// <summary>
    /// Gets the extra space added to both horizontal sides of the decorated text range,
    /// in effective pixels.
    /// </summary>
    public float HorizontalPadding
    {
        get => horizontalPadding;
        init
        {
            if (!float.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(HorizontalPadding));

            horizontalPadding = value;
        }
    }

    /// <summary>Gets the overlap priority. Greater values are applied later.</summary>
    public int Priority { get; }
}
