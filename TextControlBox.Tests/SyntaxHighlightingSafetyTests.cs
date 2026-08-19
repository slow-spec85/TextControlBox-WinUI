using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TextControlBoxNS;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Models;

namespace TextControlBox.Tests;

[TestClass]
public class SyntaxHighlightingSafetyTests
{
    [TestMethod]
    [Timeout(2000, CooperativeCancellation = true)]
    public void ExternalLegacyRegex_HasFiniteMatchTimeout()
    {
        SyntaxHighlights rule = new(
            "\"(?:\\.|[^\"])*\"|'(?:\\.|[^'])*'",
            "#000000",
            "#FFFFFF");
        SyntaxHighlightLanguage language = new()
        {
            Highlights = [rule]
        };

        language.CompileAllRegex();
        Regex compiledRegex = rule.PrecompiledRegex;
        language.CompileAllRegex();

        Assert.IsFalse(language.IsBuiltIn);
        Assert.AreEqual(
            SyntaxHighlights.ExternalRegexMatchTimeout,
            rule.PrecompiledRegex.MatchTimeout);
        Assert.AreSame(compiledRegex, rule.PrecompiledRegex);
        Assert.ThrowsExactly<RegexMatchTimeoutException>(() =>
            rule.PrecompiledRegex.IsMatch("\"" + new string('.', 20000)));
    }

    [TestMethod]
    [DataRow(SyntaxHighlightID.CSharp)]
    [DataRow(SyntaxHighlightID.Cpp)]
    public void BuiltInCStyleRegexes_HaveNoMatchTimeout(SyntaxHighlightID languageId)
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(languageId);

        language.CompileAllRegex();

        Assert.IsTrue(language.IsBuiltIn);
        Assert.IsNotNull(language.Highlights);
        Assert.IsTrue(language.Highlights.All(
            rule => rule.PrecompiledRegex.MatchTimeout == Regex.InfiniteMatchTimeout));
    }

    [TestMethod]
    [DataRow(SyntaxHighlightID.CSharp, "void Create() { return new object(); }")]
    [DataRow(SyntaxHighlightID.Cpp, "void* Create() { return new Widget(); }")]
    public void BuiltInCStyleKeywords_AreMatched(SyntaxHighlightID languageId, string text)
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(languageId);
        SyntaxHighlights keywordRule = language.Highlights.Single(
            rule => rule.Role == SyntaxHighlightRole.Keyword);

        language.CompileAllRegex();

        HashSet<string> matches = keywordRule.PrecompiledRegex
            .Matches(text)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("void", matches);
        Assert.Contains("return", matches);
        Assert.Contains("new", matches);
    }

    [TestMethod]
    [DataRow("#region Name")]
    [DataRow("    #endregion")]
    [DataRow("\t# region Name")]
    public void CSharpPreprocessorDirective_MatchesAtLineStart(string directive)
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(
            SyntaxHighlightID.CSharp);
        SyntaxHighlights directiveRule = language.Highlights.Single(
            rule => rule.Role == SyntaxHighlightRole.Directive);
        string text = $"class Example {{\r\n{directive}\r\n}}";

        language.CompileAllRegex();
        Match match = directiveRule.PrecompiledRegex.Match(text);

        Assert.IsTrue(match.Success);
        Assert.AreEqual(directive, match.Value);
    }

    [TestMethod]
    public void CSharpPreprocessorDirective_DoesNotMatchInsideStringLiteral()
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(
            SyntaxHighlightID.CSharp);
        SyntaxHighlights directiveRule = language.Highlights.Single(
            rule => rule.Role == SyntaxHighlightRole.Directive);
        const string text =
            "new SyntaxHighlights(\"#region.*$\", \"#ff0000\", \"#ff0000\", true),";

        language.CompileAllRegex();

        Assert.IsFalse(directiveRule.PrecompiledRegex.IsMatch(text));
    }

    [TestMethod]
    public void TimedOutHighlightRule_IsQuarantinedOnlyInCurrentSession()
    {
        TimeoutHighlightRule rule = new();
        SyntaxHighlightLanguage language = new() { Name = "External" };
        SyntaxHighlightingSession first = new();
        SyntaxHighlightingSession second = new();
        SyntaxHighlightingRuleQuarantinedEventArgs? received = null;
        object? eventSender = null;
        int eventCount = 0;
        first.RuleQuarantined += (sender, args) =>
        {
            eventSender = sender;
            received = args;
            eventCount++;
        };

        bool completed = first.TryGetHighlights(
            language,
            rule,
            ["input"],
            "input",
            "\n",
            out _);

        Assert.IsFalse(completed);
        Assert.IsTrue(first.IsQuarantined(rule));
        Assert.IsFalse(second.IsQuarantined(rule));
        Assert.AreSame(first, eventSender);
        Assert.IsNotNull(received);
        Assert.AreSame(language, received!.Language);
        Assert.AreSame(rule, received.Rule);
        Assert.AreEqual(rule.GetType(), received.RuleType);
        Assert.AreEqual("pattern", received.Pattern);
        Assert.AreEqual(TimeSpan.FromMilliseconds(50), received.MatchTimeout);
        Assert.AreEqual(5, received.InputLength);
        Assert.IsInstanceOfType<RegexMatchTimeoutException>(received.Exception);

        Assert.IsFalse(first.TryGetHighlights(
            language,
            rule,
            ["input"],
            "input",
            "\n",
            out _));
        Assert.AreEqual(1, eventCount);
    }

    [TestMethod]
    public void QuarantinedRule_IsSkippedUntilRulesAreReset()
    {
        object timedOutRule = new();
        object healthyRule = new();
        SyntaxHighlightLanguage language = new() { Name = "External" };
        SyntaxHighlightingSession session = new();
        int timedOutInvocations = 0;
        int healthyInvocations = 0;

        session.TryExecute(language, timedOutRule, () =>
        {
            timedOutInvocations++;
            throw new RegexMatchTimeoutException(
                "input",
                "pattern",
                TimeSpan.FromMilliseconds(50));
        });
        bool repeated = session.TryExecute(
            language,
            timedOutRule,
            () => timedOutInvocations++);
        bool healthy = session.TryExecute(
            language,
            healthyRule,
            () => healthyInvocations++);

        Assert.IsFalse(repeated);
        Assert.IsTrue(healthy);
        Assert.AreEqual(1, timedOutInvocations);
        Assert.AreEqual(1, healthyInvocations);

        session.ResetRules();

        Assert.IsTrue(session.TryExecute(
            language,
            timedOutRule,
            () => timedOutInvocations++));
        Assert.AreEqual(2, timedOutInvocations);
    }

    [UITestMethod]
    public void StatefulTimeout_QuarantinesOnlyFailingRule_AndContinuesRendering()
    {
        SyntaxHighlightingSession session = new();
        TimeoutStatefulRule timedOutRule = new();
        HealthyStatefulRule healthyRule = new();
        SyntaxHighlightLanguage language = new()
        {
            StatefulHighlightRules = [timedOutRule, healthyRule]
        };
        EditorTestContext editor = EditorTestContext.Create("text");
        StatefulSyntaxHighlightingManager manager = new();
        manager.Init(editor.TextManager, editor.Events, session);

        IReadOnlyList<HighlightSpan> highlights = manager.GetHighlights(
            language,
            0,
            editor.TextManager.totalLines.Span,
            "\n");

        Assert.IsTrue(session.IsQuarantined(timedOutRule));
        Assert.IsFalse(session.IsQuarantined(healthyRule));
        Assert.HasCount(1, highlights);
        Assert.AreEqual(SyntaxHighlightRole.Keyword, highlights[0].Role);
    }

    private sealed class TimeoutStatefulRule : IStatefulHighlightRule
    {
        public int InitialState => 0;

        public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
        {
            throw new RegexMatchTimeoutException(
                line.ToString(),
                "pattern",
                TimeSpan.FromMilliseconds(50));
        }

        public void GetHighlights(
            int lineNumber,
            ReadOnlySpan<char> line,
            int state,
            ICollection<HighlightSpan> highlights)
        {
            throw new RegexMatchTimeoutException(
                line.ToString(),
                "pattern",
                TimeSpan.FromMilliseconds(50));
        }
    }

    private sealed class TimeoutHighlightRule : IHighlightRule
    {
        public List<HighlightSpan> GetHighlights(
            ReadOnlySpan<string> lines,
            string text,
            string newLineCharacter)
        {
            throw new RegexMatchTimeoutException(
                text,
                "pattern",
                TimeSpan.FromMilliseconds(50));
        }
    }

    private sealed class HealthyStatefulRule : IStatefulHighlightRule
    {
        public int InitialState => 0;

        public int GetStateAfterLine(int lineNumber, ReadOnlySpan<char> line, int state)
        {
            return state;
        }

        public void GetHighlights(
            int lineNumber,
            ReadOnlySpan<char> line,
            int state,
            ICollection<HighlightSpan> highlights)
        {
            highlights.Add(new HighlightSpan
            {
                Start = 0,
                Length = line.Length,
                Role = SyntaxHighlightRole.Keyword,
            });
        }
    }
}
