using System;
using System.Collections.Generic;
using System.Drawing;
using TextControlBoxNS.Extensions;

namespace TextControlBoxNS;

internal sealed class PowerShellLineCommentRule : IFragmentAwareStatefulHighlightRule
{
    private const int OutsideHereString = 0;
    private const int InsideSingleQuotedHereString = 1;
    private const int InsideDoubleQuotedHereString = 2;

    private readonly Windows.UI.Color colorLight;
    private readonly Windows.UI.Color colorDark;

    public PowerShellLineCommentRule(string colorLight, string colorDark)
    {
        this.colorLight = ParseColor(colorLight, nameof(colorLight));
        this.colorDark = ParseColor(colorDark, nameof(colorDark));
    }

    public int InitialState => OutsideHereString;

    public int InferInitialState(ReadOnlySpan<string> lines)
    {
        foreach (string line in lines)
        {
            ReadOnlySpan<char> trimmedLine = line.AsSpan().TrimStart();
            if (trimmedLine.StartsWith("'@".AsSpan(), StringComparison.Ordinal))
                return InsideSingleQuotedHereString;
            if (trimmedLine.StartsWith("\"@".AsSpan(), StringComparison.Ordinal))
                return InsideDoubleQuotedHereString;

            int state = ScanOutsideHereString(line.AsSpan(), null);
            if (state != OutsideHereString)
                return OutsideHereString;
        }

        return InitialState;
    }

    public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
    {
        if (state != OutsideHereString)
            return IsHereStringTerminator(line, state) ? OutsideHereString : state;

        return ScanOutsideHereString(line, null);
    }

    public void GetHighlights(
        int lineNumber,
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(highlights);
        if (state == OutsideHereString)
            ScanOutsideHereString(line, highlights);
    }

    private int ScanOutsideHereString(
        ReadOnlySpan<char> line,
        ICollection<HighlightSpan> highlights)
    {
        bool insideSingleQuotedString = false;
        bool insideDoubleQuotedString = false;

        for (int position = 0; position < line.Length; position++)
        {
            char character = line[position];
            if (insideSingleQuotedString)
            {
                if (character != '\'')
                    continue;

                if (position + 1 < line.Length && line[position + 1] == '\'')
                {
                    position++;
                    continue;
                }

                insideSingleQuotedString = false;
                continue;
            }

            if (insideDoubleQuotedString)
            {
                if (character == '`' && position + 1 < line.Length)
                {
                    position++;
                    continue;
                }

                if (character == '"')
                    insideDoubleQuotedString = false;
                continue;
            }

            if (character == '\'')
            {
                insideSingleQuotedString = true;
                continue;
            }

            if (character == '"')
            {
                insideDoubleQuotedString = true;
                continue;
            }

            if (character == '#')
            {
                AddHighlight(position, line.Length, highlights);
                return OutsideHereString;
            }

            if (character == '@'
                && position + 1 < line.Length
                && line[(position + 2)..].Trim().IsEmpty)
            {
                if (line[position + 1] == '\'')
                    return InsideSingleQuotedHereString;
                if (line[position + 1] == '"')
                    return InsideDoubleQuotedHereString;
            }
        }

        return OutsideHereString;
    }

    private static bool IsHereStringTerminator(ReadOnlySpan<char> line, int state)
    {
        ReadOnlySpan<char> trimmedLine = line.TrimStart();
        ReadOnlySpan<char> delimiter = state == InsideSingleQuotedHereString
            ? "'@".AsSpan()
            : "\"@".AsSpan();
        return trimmedLine.StartsWith(delimiter, StringComparison.Ordinal);
    }

    private void AddHighlight(
        int start,
        int end,
        ICollection<HighlightSpan> highlights)
    {
        if (highlights is null)
            return;

        highlights.Add(new HighlightSpan
        {
            Start = start,
            Length = end - start,
            ColorLight = colorLight,
            ColorDark = colorDark,
            Role = SyntaxHighlightRole.Comment,
        });
    }

    private static Windows.UI.Color ParseColor(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
        ColorConverter converter = new();
        return ((Color)converter.ConvertFromString(value)).ToMediaColor();
    }
}
