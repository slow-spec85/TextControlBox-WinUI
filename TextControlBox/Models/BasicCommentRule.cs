using System;
using System.Collections.Generic;
using System.Drawing;
using TextControlBoxNS.Extensions;

namespace TextControlBoxNS;

internal sealed class BasicCommentRule : IStatefulHighlightRule
{
    private readonly Windows.UI.Color colorLight;
    private readonly Windows.UI.Color colorDark;

    public BasicCommentRule()
    {
        ColorConverter converter = new();
        colorLight = ((Color)converter.ConvertFromString("#6B6A6A")).ToMediaColor();
        colorDark = ((Color)converter.ConvertFromString("#646464")).ToMediaColor();
    }

    public int InitialState => 0;

    public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
    {
        return InitialState;
    }

    public void GetHighlights(
        int lineNumber,
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(highlights);

        bool insideString = false;
        for (int position = 0; position < line.Length; position++)
        {
            char character = line[position];
            if (character == '"')
            {
                if (insideString && position + 1 < line.Length && line[position + 1] == '"')
                {
                    position++;
                    continue;
                }

                insideString = !insideString;
                continue;
            }

            if (insideString)
                continue;

            if (character == '\'')
            {
                AddHighlight(position, line.Length, highlights);
                return;
            }

            if (IsRemComment(line, position))
            {
                AddHighlight(position, line.Length, highlights);
                return;
            }
        }
    }

    private static bool IsRemComment(ReadOnlySpan<char> line, int position)
    {
        if (position + 3 > line.Length
            || !line[position..(position + 3)].Equals("Rem".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool followsStatementBoundary = position == 0
            || char.IsWhiteSpace(line[position - 1])
            || line[position - 1] == ':';
        bool endsAtTokenBoundary = position + 3 == line.Length
            || char.IsWhiteSpace(line[position + 3]);
        return followsStatementBoundary && endsAtTokenBoundary;
    }

    private void AddHighlight(int start, int end, ICollection<HighlightSpan> highlights)
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
