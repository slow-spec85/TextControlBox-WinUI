using System;
using System.Collections.Generic;
using System.Drawing;
using TextControlBoxNS.Extensions;

namespace TextControlBoxNS;

/// <summary>
/// Highlights text enclosed by literal delimiters, including ranges that cross line boundaries.
/// </summary>
public sealed class DelimitedHighlightRule : IFragmentAwareStatefulHighlightRule
{
    private const int OutsideRange = 0;
    private const int InsideRange = 1;

    private readonly string startDelimiter;
    private readonly string endDelimiter;
    private readonly StringComparison comparison;
    private readonly Windows.UI.Color colorLight;
    private readonly Windows.UI.Color colorDark;
    private readonly CodeFontStyle style;
    private readonly SyntaxHighlightRole role;

    /// <summary>
    /// Initializes a literal delimited syntax rule.
    /// </summary>
    /// <param name="startDelimiter">The non-empty delimiter that opens a highlighted range.</param>
    /// <param name="endDelimiter">The non-empty delimiter that closes a highlighted range.</param>
    /// <param name="colorLight">The highlight color used by the light theme.</param>
    /// <param name="colorDark">The highlight color used by the dark theme.</param>
    /// <param name="style">Optional font styling for the range.</param>
    /// <param name="ignoreCase">Whether delimiter matching ignores character casing.</param>
    /// <param name="role">The semantic role used by an optional syntax highlighting palette.</param>
    public DelimitedHighlightRule(
        string startDelimiter,
        string endDelimiter,
        Windows.UI.Color colorLight,
        Windows.UI.Color colorDark,
        CodeFontStyle style = null,
        bool ignoreCase = false,
        SyntaxHighlightRole role = SyntaxHighlightRole.Custom)
    {
        ArgumentException.ThrowIfNullOrEmpty(startDelimiter);
        ArgumentException.ThrowIfNullOrEmpty(endDelimiter);

        this.startDelimiter = startDelimiter;
        this.endDelimiter = endDelimiter;
        this.colorLight = colorLight;
        this.colorDark = colorDark;
        this.style = style;
        this.role = role;
        comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    /// <summary>
    /// Initializes a literal delimited syntax rule using hexadecimal color strings.
    /// </summary>
    /// <param name="startDelimiter">The non-empty delimiter that opens a highlighted range.</param>
    /// <param name="endDelimiter">The non-empty delimiter that closes a highlighted range.</param>
    /// <param name="colorLight">The light-theme color in a format accepted by <see cref="ColorConverter"/>.</param>
    /// <param name="colorDark">The dark-theme color in a format accepted by <see cref="ColorConverter"/>.</param>
    /// <param name="style">Optional font styling for the range.</param>
    /// <param name="ignoreCase">Whether delimiter matching ignores character casing.</param>
    /// <param name="role">The semantic role used by an optional syntax highlighting palette.</param>
    public DelimitedHighlightRule(
        string startDelimiter,
        string endDelimiter,
        string colorLight,
        string colorDark,
        CodeFontStyle style = null,
        bool ignoreCase = false,
        SyntaxHighlightRole role = SyntaxHighlightRole.Custom)
        : this(
            startDelimiter,
            endDelimiter,
            ParseColor(colorLight, nameof(colorLight)),
            ParseColor(colorDark, nameof(colorDark)),
            style,
            ignoreCase,
            role)
    {
    }

    /// <inheritdoc/>
    public int InitialState => OutsideRange;

    /// <inheritdoc/>
    public int InferInitialState(ReadOnlySpan<string> lines)
    {
        if (string.Equals(startDelimiter, endDelimiter, comparison))
            return InitialState;

        foreach (string line in lines)
        {
            int startIndex = line.AsSpan().IndexOf(startDelimiter.AsSpan(), comparison);
            int endIndex = line.AsSpan().IndexOf(endDelimiter.AsSpan(), comparison);

            if (startIndex < 0 && endIndex < 0)
                continue;

            return endIndex >= 0 && (startIndex < 0 || endIndex < startIndex)
                ? InsideRange
                : OutsideRange;
        }

        return InitialState;
    }

    /// <inheritdoc/>
    public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
    {
        int position = 0;
        bool insideRange = state == InsideRange;

        while (position <= line.Length)
        {
            if (insideRange)
            {
                int endIndex = line[position..].IndexOf(endDelimiter.AsSpan(), comparison);
                if (endIndex < 0)
                    return InsideRange;

                position += endIndex + endDelimiter.Length;
                insideRange = false;
                continue;
            }

            int startIndex = line[position..].IndexOf(startDelimiter.AsSpan(), comparison);
            if (startIndex < 0)
                return OutsideRange;

            position += startIndex + startDelimiter.Length;
            insideRange = true;
        }

        return insideRange ? InsideRange : OutsideRange;
    }

    /// <inheritdoc/>
    public void GetHighlights(
        int lineNumber,
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(highlights);

        int position = 0;
        bool insideRange = state == InsideRange;

        while (position <= line.Length)
        {
            int highlightStart;
            if (insideRange)
            {
                highlightStart = position;
            }
            else
            {
                int startIndex = line[position..].IndexOf(startDelimiter.AsSpan(), comparison);
                if (startIndex < 0)
                    return;

                highlightStart = position + startIndex;
                position = highlightStart + startDelimiter.Length;
                insideRange = true;
            }

            int endIndex = line[position..].IndexOf(endDelimiter.AsSpan(), comparison);
            int highlightEnd;
            if (endIndex < 0)
            {
                highlightEnd = line.Length;
                position = line.Length + 1;
            }
            else
            {
                highlightEnd = position + endIndex + endDelimiter.Length;
                position = highlightEnd;
                insideRange = false;
            }

            if (highlightEnd > highlightStart)
            {
                highlights.Add(new HighlightSpan
                {
                    Start = highlightStart,
                    Length = highlightEnd - highlightStart,
                    ColorLight = colorLight,
                    ColorDark = colorDark,
                    Style = style,
                    Role = role,
                });
            }

            if (endIndex < 0)
                return;
        }
    }

    private static Windows.UI.Color ParseColor(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
        var converter = new ColorConverter();
        return ((Color)converter.ConvertFromString(value)).ToMediaColor();
    }
}
