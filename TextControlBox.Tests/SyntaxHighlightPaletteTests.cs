using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TextControlBoxNS;
using TextControlBoxNS.Core;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBox.Tests;

[TestClass]
public class SyntaxHighlightPaletteTests
{
    private static readonly Color Red = Color.FromArgb(255, 255, 0, 0);
    private static readonly Color Blue = Color.FromArgb(255, 0, 0, 255);
    private static readonly Color Green = Color.FromArgb(255, 0, 128, 0);
    private static readonly Color Yellow = Color.FromArgb(255, 255, 255, 0);
    private static readonly Color Black = Color.FromArgb(255, 0, 0, 0);

    [TestMethod]
    public void Palette_OverridesKnownRole_AndFallsBackForMissingRole()
    {
        Color light = Color.FromArgb(255, 1, 2, 3);
        Color dark = Color.FromArgb(255, 4, 5, 6);
        SyntaxHighlightPalette palette = new(
            new SyntaxHighlightPaletteEntry(SyntaxHighlightRole.String, light, dark));

        Assert.AreEqual(
            light,
            palette.Resolve(
                SyntaxHighlightRole.String,
                isLightTheme: true,
                fallback: Red));
        Assert.AreEqual(
            Red,
            palette.Resolve(
                SyntaxHighlightRole.Comment,
                isLightTheme: true,
                fallback: Red));
    }

    [TestMethod]
    public void Palette_CopiesEntries()
    {
        SyntaxHighlightPaletteEntry[] entries =
        [
            new(SyntaxHighlightRole.Keyword, Red, Blue)
        ];
        SyntaxHighlightPalette palette = new(entries);

        entries[0] = new(SyntaxHighlightRole.Keyword, Green, Yellow);

        Assert.AreEqual(
            Red,
            palette.Resolve(
                SyntaxHighlightRole.Keyword,
                isLightTheme: true,
                fallback: Black));
    }

    [TestMethod]
    public void Palette_RejectsDuplicateRoles()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new SyntaxHighlightPalette(
            new SyntaxHighlightPaletteEntry(
                SyntaxHighlightRole.Keyword,
                Red,
                Blue),
            new SyntaxHighlightPaletteEntry(
                SyntaxHighlightRole.Keyword,
                Green,
                Yellow)));
    }

    [TestMethod]
    public void LegacyRuleWithoutRole_RemainsCustomAndKeepsFallbackColors()
    {
        SyntaxHighlights rule = new("abc", "#010203", "#040506");

        Assert.AreEqual(SyntaxHighlightRole.Custom, rule.Role);
        Assert.AreEqual(Color.FromArgb(255, 1, 2, 3), rule.ColorLight_Clr);
        Assert.AreEqual(Color.FromArgb(255, 4, 5, 6), rule.ColorDark_Clr);
    }

    [TestMethod]
    public void RegexHighlightRule_CopiesSemanticRoleToSpan()
    {
        SyntaxHighlights definition = new(
            "abc",
            "#010203",
            "#040506",
            role: SyntaxHighlightRole.Keyword);
        RegexHighlightRule rule = new(definition);

        HighlightSpan span = rule.GetHighlights(["abc"], "abc", "\n").Single();

        Assert.AreEqual(SyntaxHighlightRole.Keyword, span.Role);
    }

    [TestMethod]
    public void DelimitedHighlightRule_CopiesSemanticRoleToSpan()
    {
        DelimitedHighlightRule rule = new(
            "/*",
            "*/",
            Green,
            Yellow,
            style: null,
            ignoreCase: false,
            role: SyntaxHighlightRole.Comment);
        List<HighlightSpan> spans = [];

        rule.GetHighlights(0, "/* comment */".AsSpan(), rule.InitialState, spans);

        Assert.AreEqual(SyntaxHighlightRole.Comment, spans.Single().Role);
    }

    [TestMethod]
    public void CsvColumnHighlightRule_UsesValueRole()
    {
        CsvColumnHighlightRule rule = new();

        HighlightSpan span = rule.GetHighlights(["value"], "value", "\n").Single();

        Assert.AreEqual(SyntaxHighlightRole.Value, span.Role);
    }

    [TestMethod]
    public void Session_ResolvesPaletteBeforeRuleFallback()
    {
        SyntaxHighlightingSession session = new()
        {
            Palette = new SyntaxHighlightPalette(
                new SyntaxHighlightPaletteEntry(
                    SyntaxHighlightRole.String,
                    Red,
                    Blue))
        };

        Assert.AreEqual(
            Red,
            session.ResolveColor(SyntaxHighlightRole.String, true, Green));
        Assert.AreEqual(
            Blue,
            session.ResolveColor(SyntaxHighlightRole.String, false, Green));
        Assert.AreEqual(
            Green,
            session.ResolveColor(SyntaxHighlightRole.Comment, true, Green));
    }

    [TestMethod]
    public void BuiltInLegacyRules_DefineSemanticRoles()
    {
        foreach (KeyValuePair<SyntaxHighlightID, SyntaxHighlightLanguage> entry
            in CoreTextControlBox.SyntaxHighlightings)
        {
            if (entry.Value?.Highlights is null)
                continue;

            foreach (SyntaxHighlights highlight in entry.Value.Highlights)
            {
                Assert.AreNotEqual(
                    SyntaxHighlightRole.Custom,
                    highlight.Role,
                    $"{entry.Key} contains an unclassified rule: {highlight.Pattern}");
            }
        }
    }

    [TestMethod]
    public void CSharpAndMarkdown_ExposeRolesUsedByApplicationPalettes()
    {
        SyntaxHighlightLanguage csharp =
            CoreTextControlBox.GetSyntaxHighlightingFromID(SyntaxHighlightID.CSharp);
        SyntaxHighlightLanguage markdown =
            CoreTextControlBox.GetSyntaxHighlightingFromID(SyntaxHighlightID.Markdown);

        CollectionAssert.IsSubsetOf(
            new[]
            {
                SyntaxHighlightRole.Keyword,
                SyntaxHighlightRole.ControlFlow,
                SyntaxHighlightRole.Type,
                SyntaxHighlightRole.Function,
                SyntaxHighlightRole.String,
                SyntaxHighlightRole.Number,
            },
            csharp.Highlights.Select(highlight => highlight.Role).Distinct().ToArray());
        Assert.IsTrue(
            csharp.StatefulHighlightRules.Any(rule => rule is CStyleCommentRule),
            "C# comments must be handled by the stateful C-style rule.");
        CollectionAssert.Contains(
            markdown.Highlights.Select(highlight => highlight.Role).Distinct().ToArray(),
            SyntaxHighlightRole.MarkupName);
        CollectionAssert.Contains(
            markdown.Highlights.Select(highlight => highlight.Role).Distinct().ToArray(),
            SyntaxHighlightRole.String);
        CollectionAssert.Contains(
            markdown.Highlights.Select(highlight => highlight.Role).Distinct().ToArray(),
            SyntaxHighlightRole.Directive);
    }

    [TestMethod]
    public void CSharpControlFlowKeywords_AreClassifiedAsControlFlow()
    {
        string[] keywords =
        {
            "async", "await", "break", "case", "catch", "continue", "default", "do",
            "else", "finally", "for", "foreach", "goto", "if", "return", "switch",
            "throw", "try", "when", "while", "yield",
        };
        SyntaxHighlightLanguage language =
            CoreTextControlBox.GetSyntaxHighlightingFromID(SyntaxHighlightID.CSharp);

        foreach (string keyword in keywords)
        {
            SyntaxHighlightRole effectiveRole = language.Highlights
                .Where(highlight => Regex.IsMatch(keyword, highlight.Pattern))
                .Select(highlight => highlight.Role)
                .LastOrDefault();

            Assert.AreEqual(
                SyntaxHighlightRole.ControlFlow,
                effectiveRole,
                $"C# keyword '{keyword}' has role {effectiveRole}.");
        }
    }
}
