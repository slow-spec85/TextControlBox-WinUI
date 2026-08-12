using System;
using System.Collections.Generic;
using System.Drawing;
using TextControlBoxNS.Extensions;

namespace TextControlBoxNS;

internal sealed class CStyleCommentRule : IStatefulHighlightRule
{
    private const int OutsideComment = 0;
    private const int InsideComment = 1;
    private const int InsideVerbatimString = 2;
    private const int InsideTemplateString = 3;
    private const int InsideRawStringOffset = 4;

    private readonly Windows.UI.Color colorLight;
    private readonly Windows.UI.Color colorDark;
    private readonly bool supportsLineComments;
    private readonly bool supportsHashLineComments;
    private readonly bool supportsBacktickStrings;
    private readonly bool supportsVerbatimStrings;
    private readonly bool supportsRawStrings;

    public CStyleCommentRule(
        string colorLight,
        string colorDark,
        bool supportsLineComments = true,
        bool supportsHashLineComments = false,
        bool supportsBacktickStrings = false,
        bool supportsVerbatimStrings = false,
        bool supportsRawStrings = false)
    {
        this.colorLight = ParseColor(colorLight, nameof(colorLight));
        this.colorDark = ParseColor(colorDark, nameof(colorDark));
        this.supportsLineComments = supportsLineComments;
        this.supportsHashLineComments = supportsHashLineComments;
        this.supportsBacktickStrings = supportsBacktickStrings;
        this.supportsVerbatimStrings = supportsVerbatimStrings;
        this.supportsRawStrings = supportsRawStrings;
    }

    public int InitialState => OutsideComment;

    public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
    {
        return ScanLine(line, state, null);
    }

    public void GetHighlights(
        int lineNumber,
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(highlights);
        ScanLine(line, state, highlights);
    }

    private int ScanLine(
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights)
    {
        int position = 0;

        if (state == InsideComment)
        {
            position = ScanComment(line, 0, highlights);
            if (position < 0)
                return InsideComment;
        }
        else if (state == InsideVerbatimString)
        {
            position = FindVerbatimStringEnd(line, 0);
            if (position < 0)
                return InsideVerbatimString;
        }
        else if (state == InsideTemplateString)
        {
            position = FindEscapedStringEnd(line, 0, '`');
            if (position < 0)
                return InsideTemplateString;
        }
        else if (state >= InsideRawStringOffset)
        {
            int quoteCount = state - InsideRawStringOffset;
            position = FindRawStringEnd(line, 0, quoteCount);
            if (position < 0)
                return state;
        }

        while (position < line.Length)
        {
            char character = line[position];

            if (character == '/' && position + 1 < line.Length)
            {
                char nextCharacter = line[position + 1];
                if (nextCharacter == '*')
                {
                    int commentEnd = ScanComment(line, position, highlights);
                    if (commentEnd < 0)
                        return InsideComment;

                    position = commentEnd;
                    continue;
                }

                if (supportsLineComments && nextCharacter == '/')
                {
                    AddHighlight(position, line.Length, highlights);
                    return OutsideComment;
                }
            }

            if (supportsHashLineComments && character == '#')
            {
                AddHighlight(position, line.Length, highlights);
                return OutsideComment;
            }

            if (character == '"')
            {
                int quoteCount = CountConsecutiveCharacters(line, position, '"');
                if (supportsRawStrings && quoteCount >= 3)
                {
                    position = FindRawStringEnd(line, position + quoteCount, quoteCount);
                    if (position < 0)
                        return InsideRawStringOffset + quoteCount;
                    continue;
                }

                if (supportsVerbatimStrings && IsVerbatimStringStart(line, position))
                {
                    position = FindVerbatimStringEnd(line, position + 1);
                    if (position < 0)
                        return InsideVerbatimString;
                    continue;
                }

                position = FindEscapedStringEnd(line, position + 1, character);
                if (position < 0)
                    return OutsideComment;
                continue;
            }

            if (character == '\'')
            {
                position = FindEscapedStringEnd(line, position + 1, character);
                if (position < 0)
                    return OutsideComment;
                continue;
            }

            if (supportsBacktickStrings && character == '`')
            {
                position = FindEscapedStringEnd(line, position + 1, character);
                if (position < 0)
                    return InsideTemplateString;
                continue;
            }

            position++;
        }

        return OutsideComment;
    }

    private int ScanComment(
        ReadOnlySpan<char> line,
        int start,
        ICollection<HighlightSpan> highlights)
    {
        int searchStart = start == 0 ? 0 : start + 2;
        int relativeEnd = line[searchStart..].IndexOf("*/".AsSpan(), StringComparison.Ordinal);
        int end = relativeEnd < 0 ? line.Length : searchStart + relativeEnd + 2;

        AddHighlight(start, end, highlights);

        return relativeEnd < 0 ? -1 : end;
    }

    private void AddHighlight(
        int start,
        int end,
        ICollection<HighlightSpan> highlights)
    {
        if (end > start && highlights is not null)
        {
            highlights.Add(new HighlightSpan
            {
                Start = start,
                Length = end - start,
                ColorLight = colorLight,
                ColorDark = colorDark,
                Role = SyntaxHighlightRole.Comment,
            });
        }
    }

    private static int FindEscapedStringEnd(
        ReadOnlySpan<char> line,
        int position,
        char quote)
    {
        bool escaped = false;
        while (position < line.Length)
        {
            char character = line[position++];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character == quote)
                return position;
        }

        return -1;
    }

    private static int FindVerbatimStringEnd(ReadOnlySpan<char> line, int position)
    {
        while (position < line.Length)
        {
            if (line[position] != '"')
            {
                position++;
                continue;
            }

            if (position + 1 < line.Length && line[position + 1] == '"')
            {
                position += 2;
                continue;
            }

            return position + 1;
        }

        return -1;
    }

    private static int FindRawStringEnd(
        ReadOnlySpan<char> line,
        int position,
        int quoteCount)
    {
        while (position < line.Length)
        {
            int relativeQuote = line[position..].IndexOf('"');
            if (relativeQuote < 0)
                return -1;

            position += relativeQuote;
            int candidateCount = CountConsecutiveCharacters(line, position, '"');
            if (candidateCount >= quoteCount)
                return position + quoteCount;

            position += candidateCount;
        }

        return -1;
    }

    private static int CountConsecutiveCharacters(
        ReadOnlySpan<char> line,
        int position,
        char character)
    {
        int count = 0;
        while (position + count < line.Length && line[position + count] == character)
            count++;
        return count;
    }

    private static bool IsVerbatimStringStart(ReadOnlySpan<char> line, int quotePosition)
    {
        return quotePosition > 0 && line[quotePosition - 1] == '@'
            || quotePosition > 1
                && line[quotePosition - 1] == '$'
                && line[quotePosition - 2] == '@';
    }

    private static Windows.UI.Color ParseColor(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
        var converter = new ColorConverter();
        return ((Color)converter.ConvertFromString(value)).ToMediaColor();
    }
}
