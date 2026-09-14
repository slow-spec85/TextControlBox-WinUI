using System;
using System.Collections.Generic;
using System.Drawing;
using TextControlBoxNS.Extensions;

namespace TextControlBoxNS;

internal sealed class YamlCommentRule : IStatefulHighlightRule
{
    private const int OutsideBlockScalar = 0;

    private readonly Windows.UI.Color colorLight;
    private readonly Windows.UI.Color colorDark;

    public YamlCommentRule(string colorLight, string colorDark)
    {
        this.colorLight = ParseColor(colorLight, nameof(colorLight));
        this.colorDark = ParseColor(colorDark, nameof(colorDark));
    }

    public int InitialState => OutsideBlockScalar;

    public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
    {
        if (IsInsideBlockScalarLine(line, state))
            return state;

        return ScanYamlLine(line, null);
    }

    public void GetHighlights(
        int lineNumber,
        ReadOnlySpan<char> line,
        int state,
        ICollection<HighlightSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(highlights);
        if (!IsInsideBlockScalarLine(line, state))
            ScanYamlLine(line, highlights);
    }

    private int ScanYamlLine(
        ReadOnlySpan<char> line,
        ICollection<HighlightSpan> highlights)
    {
        bool insideSingleQuotedScalar = false;
        bool insideDoubleQuotedScalar = false;
        int codeEnd = line.Length;

        for (int position = 0; position < line.Length; position++)
        {
            char character = line[position];
            if (insideSingleQuotedScalar)
            {
                if (character != '\'')
                    continue;

                if (position + 1 < line.Length && line[position + 1] == '\'')
                {
                    position++;
                    continue;
                }

                insideSingleQuotedScalar = false;
                continue;
            }

            if (insideDoubleQuotedScalar)
            {
                if (character == '\\' && position + 1 < line.Length)
                {
                    position++;
                    continue;
                }

                if (character == '"')
                    insideDoubleQuotedScalar = false;
                continue;
            }

            if (character == '\'')
            {
                insideSingleQuotedScalar = true;
                continue;
            }

            if (character == '"')
            {
                insideDoubleQuotedScalar = true;
                continue;
            }

            if (character == '#'
                && (position == 0 || char.IsWhiteSpace(line[position - 1])))
            {
                codeEnd = position;
                AddHighlight(position, line.Length, highlights);
                break;
            }
        }

        return IsBlockScalarHeader(line[..codeEnd])
            ? CountLeadingSpaces(line) + 1
            : OutsideBlockScalar;
    }

    private static bool IsInsideBlockScalarLine(ReadOnlySpan<char> line, int state)
    {
        if (state == OutsideBlockScalar)
            return false;
        if (line.Trim().IsEmpty)
            return true;

        return CountLeadingSpaces(line) >= state;
    }

    private static bool IsBlockScalarHeader(ReadOnlySpan<char> line)
    {
        ReadOnlySpan<char> trimmedLine = line.TrimEnd();
        int position = trimmedLine.Length - 1;
        int modifiers = 0;
        while (position >= 0
            && modifiers < 2
            && (trimmedLine[position] is '+' or '-'
                || trimmedLine[position] is >= '1' and <= '9'))
        {
            position--;
            modifiers++;
        }

        if (position < 0 || trimmedLine[position] is not ('|' or '>'))
            return false;
        if (position == 0)
            return true;

        char precedingCharacter = trimmedLine[position - 1];
        return char.IsWhiteSpace(precedingCharacter)
            || precedingCharacter is ':' or '-';
    }

    private static int CountLeadingSpaces(ReadOnlySpan<char> line)
    {
        int count = 0;
        while (count < line.Length && line[count] == ' ')
            count++;
        return count;
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
