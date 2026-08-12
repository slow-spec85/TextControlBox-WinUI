using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TextControlBoxNS.Models;
using Windows.UI.Text;
using static TextControlBoxNS.Core.Text.TextManager;

namespace TextControlBoxNS.Core.Renderer;

internal class SyntaxHighlightingRenderer
{
    public readonly static FontWeight BoldFont = new FontWeight(600);
    public const FontStyle ItalicFont = FontStyle.Italic;

    public static void UpdateSyntaxHighlighting(
        LineSliceResult lineSliceResult,
        int startLine,
        string newLineCharacter,
        CanvasTextLayout drawnTextLayout,
        ApplicationTheme theme,
        SyntaxHighlightLanguage syntaxHighlightingLanguage,
        bool syntaxHighlighting,
        StatefulSyntaxHighlightingManager statefulManager,
        SyntaxHighlightingSession session)
    {
        if (!syntaxHighlighting)
            return;

        bool isLightTheme = theme == ApplicationTheme.Light;

        if (syntaxHighlightingLanguage?.HighlightRules != null && syntaxHighlightingLanguage.HighlightRules.Length > 0)
        {
            foreach (var rule in syntaxHighlightingLanguage.HighlightRules)
            {
                if (session.IsQuarantined(rule))
                    continue;

                List<HighlightSpan> spans;
                try
                {
                    spans = rule.GetHighlights(
                        lineSliceResult.Lines,
                        lineSliceResult.Text,
                        newLineCharacter);
                }
                catch (RegexMatchTimeoutException)
                {
                    session.Quarantine(rule);
                    continue;
                }

                foreach (var span in spans)
                {
                    ApplyHighlightSpan(drawnTextLayout, span, isLightTheme, session);
                }
            }
        }
        else if (syntaxHighlightingLanguage?.Highlights != null)
        {
            foreach (var highlight in syntaxHighlightingLanguage.Highlights)
            {
                if (highlight.PrecompiledRegex == null || session.IsQuarantined(highlight))
                    continue;

                var fallbackColor = isLightTheme
                    ? highlight.ColorLight_Clr
                    : highlight.ColorDark_Clr;
                var color = session.ResolveColor(
                    highlight.Role,
                    isLightTheme,
                    fallbackColor);

                try
                {
                    foreach (var match in highlight.PrecompiledRegex.EnumerateMatches(lineSliceResult.Text))
                    {
                        int index = match.Index;
                        int length = match.Length;

                        drawnTextLayout.SetColor(index, length, color);

                        if (highlight.CodeStyle != null)
                        {
                            if (highlight.CodeStyle.Italic)
                                drawnTextLayout.SetFontStyle(index, length, ItalicFont);
                            if (highlight.CodeStyle.Bold)
                                drawnTextLayout.SetFontWeight(index, length, BoldFont);
                            if (highlight.CodeStyle.Underlined)
                                drawnTextLayout.SetUnderline(index, length, true);
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    session.Quarantine(highlight);
                }
            }
        }

        foreach (HighlightSpan span in statefulManager.GetHighlights(
            syntaxHighlightingLanguage,
            startLine,
            lineSliceResult.Lines,
            newLineCharacter))
        {
            ApplyHighlightSpan(drawnTextLayout, span, isLightTheme, session);
        }
    }

    private static void ApplyHighlightSpan(
        CanvasTextLayout drawnTextLayout,
        HighlightSpan span,
        bool isLightTheme,
        SyntaxHighlightingSession session)
    {
        var fallbackColor = isLightTheme ? span.ColorLight : span.ColorDark;
        var color = session.ResolveColor(span.Role, isLightTheme, fallbackColor);
        drawnTextLayout.SetColor(span.Start, span.Length, color);

        if (span.Style != null)
        {
            if (span.Style.Italic)
                drawnTextLayout.SetFontStyle(span.Start, span.Length, ItalicFont);
            if (span.Style.Bold)
                drawnTextLayout.SetFontWeight(span.Start, span.Length, BoldFont);
            if (span.Style.Underlined)
                drawnTextLayout.SetUnderline(span.Start, span.Length, true);
        }
    }

    public static JsonLoadResult GetSyntaxHighlightingFromJson(string json)
    {
        try
        {
            var jsonHighlight = JsonConvert.DeserializeObject<JsonSyntaxHighlighting>(json);
            //Apply the filter as an array
            var highlightLanguage = new SyntaxHighlightLanguage
            {
                Author = jsonHighlight.Author,
                Description = jsonHighlight.Description,
                Highlights = jsonHighlight.Highlights,
                Name = jsonHighlight.Name,
                Filter = jsonHighlight.Filter.Split("|", StringSplitOptions.RemoveEmptyEntries),
            };
            return new JsonLoadResult(true, highlightLanguage);
        }
        catch (JsonReaderException)
        {
            return new JsonLoadResult(false, null);
        }
        catch (JsonSerializationException)
        {
            return new JsonLoadResult(false, null);
        }
    }
}
