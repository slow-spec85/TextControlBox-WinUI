using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TextControlBoxNS;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Text;

namespace TextControlBox.Tests;

[TestClass]
public class SyntaxHighlightingSafetyTests
{
    [TestMethod]
    [Timeout(2000, CooperativeCancellation = true)]
    public void CompiledLegacyRegex_HasFiniteMatchTimeout()
    {
        SyntaxHighlights rule = new(
            "\"(?:\\.|[^\"])*\"|'(?:\\.|[^'])*'",
            "#000000",
            "#FFFFFF");
        rule.CompileRegex();

        Assert.AreEqual(TimeSpan.FromMilliseconds(50), rule.PrecompiledRegex.MatchTimeout);
        Assert.ThrowsExactly<RegexMatchTimeoutException>(() =>
            rule.PrecompiledRegex.IsMatch("\"" + new string('.', 20000)));
    }

    [TestMethod]
    public void TimedOutRule_IsQuarantinedOnlyInCurrentSession()
    {
        object rule = new();
        SyntaxHighlightingSession first = new();
        SyntaxHighlightingSession second = new();

        bool completed = first.TryExecute(
            rule,
            () => throw new RegexMatchTimeoutException(
                "input",
                "pattern",
                TimeSpan.FromMilliseconds(50)));

        Assert.IsFalse(completed);
        Assert.IsTrue(first.IsQuarantined(rule));
        Assert.IsFalse(second.IsQuarantined(rule));
    }

    [TestMethod]
    public void QuarantinedRule_IsSkippedUntilRulesAreReset()
    {
        object timedOutRule = new();
        object healthyRule = new();
        SyntaxHighlightingSession session = new();
        int timedOutInvocations = 0;
        int healthyInvocations = 0;

        session.TryExecute(timedOutRule, () =>
        {
            timedOutInvocations++;
            throw new RegexMatchTimeoutException(
                "input",
                "pattern",
                TimeSpan.FromMilliseconds(50));
        });
        bool repeated = session.TryExecute(
            timedOutRule,
            () => timedOutInvocations++);
        bool healthy = session.TryExecute(
            healthyRule,
            () => healthyInvocations++);

        Assert.IsFalse(repeated);
        Assert.IsTrue(healthy);
        Assert.AreEqual(1, timedOutInvocations);
        Assert.AreEqual(1, healthyInvocations);

        session.ResetRules();

        Assert.IsTrue(session.TryExecute(
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
