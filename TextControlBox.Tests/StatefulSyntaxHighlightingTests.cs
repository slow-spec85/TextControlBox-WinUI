using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using TextControlBoxNS;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Text;

namespace TextControlBox.Tests;

[TestClass]
public class StatefulSyntaxHighlightingTests
{
    [TestMethod]
    public void CStyleCommentRule_IgnoresDelimitersInsideQuotedLiterals()
    {
        var rule = new CStyleCommentRule("#112233", "#445566");
        string[] lines =
        {
            "var rule = new DelimitedHighlightRule(\"/*\", \"*/\", \"#112233\", \"#445566\");",
            "int state = rule.GetStateAfterLine(0, \"prefix /* open\".AsSpan(), rule.InitialState);",
            "rule.GetHighlights(1, \"inside */ suffix\".AsSpan(), state, highlights);",
            "char quote = '\\\"';",
        };

        int state = rule.InitialState;
        var highlights = new List<HighlightSpan>();
        foreach (string line in lines)
        {
            highlights.Clear();
            rule.GetHighlights(0, line.AsSpan(), state, highlights);
            Assert.IsEmpty(highlights);
            state = rule.GetStateAfterLine(0, line.AsSpan(), state);
        }

        Assert.AreEqual(rule.InitialState, state);
    }

    [TestMethod]
    public void CStyleCommentRule_IgnoresDelimiterAfterEscapedQuote()
    {
        var rule = new CStyleCommentRule("#112233", "#445566");
        const string line = "var value = \"escaped \\\" /* still string\";";
        var highlights = new List<HighlightSpan>();

        rule.GetHighlights(0, line.AsSpan(), rule.InitialState, highlights);

        Assert.IsEmpty(highlights);
        Assert.AreEqual(
            rule.InitialState,
            rule.GetStateAfterLine(0, line.AsSpan(), rule.InitialState));
    }

    [TestMethod]
    public void CStyleCommentRule_StopsBlockCommentParsingAtLineComment()
    {
        var rule = new CStyleCommentRule("#112233", "#445566");
        const string line = "var value = 1; // literal /* is not a block comment";
        var highlights = new List<HighlightSpan>();

        rule.GetHighlights(0, line.AsSpan(), rule.InitialState, highlights);
        int state = rule.GetStateAfterLine(0, line.AsSpan(), rule.InitialState);

        Assert.HasCount(1, highlights);
        Assert.AreEqual(line.IndexOf("//", StringComparison.Ordinal), highlights[0].Start);
        Assert.AreEqual(SyntaxHighlightRole.Comment, highlights[0].Role);
        Assert.AreEqual(rule.InitialState, state);
    }

    [TestMethod]
    public void CStyleCommentRule_ContinuesRealCommentAcrossLines()
    {
        var rule = new CStyleCommentRule("#112233", "#445566");
        var openingHighlights = new List<HighlightSpan>();
        var highlights = new List<HighlightSpan>();

        rule.GetHighlights(0, "prefix /* open".AsSpan(), rule.InitialState, openingHighlights);
        int state = rule.GetStateAfterLine(0, "prefix /* open".AsSpan(), rule.InitialState);
        rule.GetHighlights(1, "inside */ suffix".AsSpan(), state, highlights);

        Assert.AreNotEqual(rule.InitialState, state);
        Assert.HasCount(1, openingHighlights);
        Assert.AreEqual("prefix ".Length, openingHighlights[0].Start);
        Assert.HasCount(1, highlights);
        Assert.AreEqual((0, "inside */".Length),
            (highlights[0].Start, highlights[0].Length));
        Assert.AreEqual(
            rule.InitialState,
            rule.GetStateAfterLine(1, "inside */ suffix".AsSpan(), state));
    }

    [TestMethod]
    public void DelimitedRule_ContinuesAcrossLinesAndClosesAtDelimiter()
    {
        var rule = new DelimitedHighlightRule("/*", "*/", "#112233", "#445566");

        int state = rule.GetStateAfterLine(0, "prefix /* open".AsSpan(), rule.InitialState);
        var highlights = new List<HighlightSpan>();
        rule.GetHighlights(1, "inside */ suffix".AsSpan(), state, highlights);

        Assert.AreNotEqual(rule.InitialState, state);
        Assert.HasCount(1, highlights);
        Assert.AreEqual(0, highlights[0].Start);
        Assert.AreEqual("inside */".Length, highlights[0].Length);
        Assert.AreEqual(
            rule.InitialState,
            rule.GetStateAfterLine(1, "inside */ suffix".AsSpan(), state));
    }

    [TestMethod]
    public void DelimitedRule_WithSameOpeningAndClosingDelimiter_ReturnsToInitialState()
    {
        var rule = new DelimitedHighlightRule("```", "```", "#112233", "#445566");
        const string line = "```code``` tail";
        var highlights = new List<HighlightSpan>();

        rule.GetHighlights(0, line.AsSpan(), rule.InitialState, highlights);
        int state = rule.GetStateAfterLine(0, line.AsSpan(), rule.InitialState);

        Assert.HasCount(1, highlights);
        Assert.AreEqual("```code```".Length, highlights[0].Length);
        Assert.AreEqual(rule.InitialState, state);
    }

    [UITestMethod]
    public void ViewportStartingInsideRange_HighlightsFromFirstCharacter()
    {
        TestContext context = CreateContext("before /*", "inside", "end */", "after");
        SyntaxHighlightLanguage language = CreateLanguage(context.Rule);

        IReadOnlyList<HighlightSpan> highlights = context.Manager.GetHighlights(
            language,
            startLine: 1,
            context.TextManager.totalLines.Span.Slice(1, 1),
            "\n");

        Assert.HasCount(1, highlights);
        Assert.AreEqual((0, "inside".Length),
            (highlights[0].Start, highlights[0].Length));
    }

    [UITestMethod]
    public void Manager_PreservesSemanticRoleWhenConvertingVisibleOffsets()
    {
        var rule = new DelimitedHighlightRule(
            "/*",
            "*/",
            "#112233",
            "#445566",
            style: null,
            ignoreCase: false,
            role: SyntaxHighlightRole.Comment);
        TestContext context = CreateContext(rule, "/* comment */");
        SyntaxHighlightLanguage language = CreateLanguage(rule);

        IReadOnlyList<HighlightSpan> highlights = context.Manager.GetHighlights(
            language,
            0,
            context.TextManager.totalLines.Span,
            "\n");

        Assert.AreEqual(SyntaxHighlightRole.Comment, highlights[0].Role);
    }

    [UITestMethod]
    public void IdenticalViewportText_UsesDocumentLineState()
    {
        TestContext context = CreateContext("/*", "same", "*/", "same");
        SyntaxHighlightLanguage language = CreateLanguage(context.Rule);

        IReadOnlyList<HighlightSpan> inside = context.Manager.GetHighlights(
            language,
            1,
            context.TextManager.totalLines.Span.Slice(1, 1),
            "\n");
        IReadOnlyList<HighlightSpan> outside = context.Manager.GetHighlights(
            language,
            3,
            context.TextManager.totalLines.Span.Slice(3, 1),
            "\n");

        Assert.HasCount(1, inside);
        Assert.IsEmpty(outside);
    }

    [UITestMethod]
    public void EditingOpeningDelimiter_InvalidatesFollowingLineState()
    {
        TestContext context = CreateContext("/*", "inside", "*/");
        SyntaxHighlightLanguage language = CreateLanguage(context.Rule);
        context.Manager.GetHighlights(
            language,
            1,
            context.TextManager.totalLines.Span.Slice(1, 1),
            "\n");
        long revisionBeforeEdit = context.Manager.Revision;

        context.TextManager.SetLineText(0, "plain");
        IReadOnlyList<HighlightSpan> highlights = context.Manager.GetHighlights(
            language,
            1,
            context.TextManager.totalLines.Span.Slice(1, 1),
            "\n");

        Assert.IsEmpty(highlights);
        Assert.IsGreaterThan(revisionBeforeEdit, context.Manager.Revision);
    }

    [UITestMethod]
    public void InsertAndRemoveLines_RecalculateShiftedState()
    {
        TestContext context = CreateContext("plain", "inside", "*/");
        SyntaxHighlightLanguage language = CreateLanguage(context.Rule);

        context.TextManager.InsertOrAdd(1, "/*");
        IReadOnlyList<HighlightSpan> afterInsert = context.Manager.GetHighlights(
            language,
            2,
            context.TextManager.totalLines.Span.Slice(2, 1),
            "\n");

        context.TextManager.DeleteAt(1);
        IReadOnlyList<HighlightSpan> afterRemove = context.Manager.GetHighlights(
            language,
            1,
            context.TextManager.totalLines.Span.Slice(1, 1),
            "\n");

        Assert.HasCount(1, afterInsert);
        Assert.IsEmpty(afterRemove);
    }

    [UITestMethod]
    public void UnchangedState_StopsIncrementalRecalculationAtConvergence()
    {
        var countingRule = new CountingRule(
            new DelimitedHighlightRule("/*", "*/", "#112233", "#445566"));
        TestContext context = CreateContext(
            countingRule,
            "plain zero",
            "plain one",
            "plain two",
            "plain three",
            "plain four");
        SyntaxHighlightLanguage language = CreateLanguage(countingRule);
        context.Manager.GetHighlights(
            language,
            4,
            context.TextManager.totalLines.Span.Slice(4, 1),
            "\n");
        countingRule.StateEvaluationCount = 0;

        context.TextManager.SetLineText(1, "changed but still plain");
        context.Manager.GetHighlights(
            language,
            4,
            context.TextManager.totalLines.Span.Slice(4, 1),
            "\n");

        Assert.AreEqual(2, countingRule.StateEvaluationCount);
    }

    private static SyntaxHighlightLanguage CreateLanguage(IStatefulHighlightRule rule)
    {
        return new SyntaxHighlightLanguage { StatefulHighlightRules = new[] { rule } };
    }

    private static TestContext CreateContext(params string[] lines)
    {
        var rule = new DelimitedHighlightRule("/*", "*/", "#112233", "#445566");
        return CreateContext(rule, lines);
    }

    private static TestContext CreateContext(IStatefulHighlightRule rule, params string[] lines)
    {
        EditorTestContext editor = EditorTestContext.Create(lines);
        var statefulManager = new StatefulSyntaxHighlightingManager();
        statefulManager.Init(editor.TextManager, editor.Events, new SyntaxHighlightingSession());
        return new TestContext(editor.TextManager, statefulManager, rule);
    }

    private sealed record TestContext(
        TextManager TextManager,
        StatefulSyntaxHighlightingManager Manager,
        IStatefulHighlightRule Rule);

    private sealed class CountingRule(IStatefulHighlightRule inner) : IStatefulHighlightRule
    {
        public int StateEvaluationCount { get; set; }

        public int InitialState => inner.InitialState;

        public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
        {
            StateEvaluationCount++;
            return inner.GetStateAfterLine(lineNumber, line, state);
        }

        public void GetHighlights(
            int lineNumber,
            ReadOnlySpan<char> line,
            int state,
            ICollection<HighlightSpan> highlights)
        {
            inner.GetHighlights(lineNumber, line, state, highlights);
        }
    }
}
