using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextControlBoxNS.Models;

internal sealed class RegexHighlightRule : IHighlightRule
{
    private readonly SyntaxHighlights _highlight;

    public RegexHighlightRule(SyntaxHighlights highlight)
    {
        _highlight = highlight;
    }

    internal void CompileRegex(TimeSpan matchTimeout)
    {
        _highlight.CompileRegex(matchTimeout);
    }

    public List<HighlightSpan> GetHighlights(ReadOnlySpan<string> lines, string text, string newLineCharacter)
    {
        if (_highlight.PrecompiledRegex == null)
            _highlight.CompileRegex();

        var highlights = new List<HighlightSpan>();

        foreach (Match match in _highlight.PrecompiledRegex.Matches(text))
        {
            highlights.Add(new HighlightSpan
            {
                Start = match.Index,
                Length = match.Length,
                ColorLight = _highlight.ColorLight_Clr,
                ColorDark = _highlight.ColorDark_Clr,
                Style = _highlight.CodeStyle,
                Role = _highlight.Role,
            });
        }
        return highlights;
    }
}
