using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBoxNS.Core;

internal sealed class SyntaxHighlightingSession
{
    private readonly HashSet<object> quarantinedRules = new(ReferenceEqualityComparer.Instance);

    public event EventHandler<SyntaxHighlightingRuleQuarantinedEventArgs> RuleQuarantined;

    public SyntaxHighlightPalette Palette { get; set; }

    public bool IsQuarantined(object rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return quarantinedRules.Contains(rule);
    }

    public bool TryExecute(
        SyntaxHighlightLanguage language,
        object rule,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return TryExecute<object>(language, rule, () =>
        {
            action();
            return null;
        }, out _);
    }

    public bool TryExecute<T>(
        SyntaxHighlightLanguage language,
        object rule,
        Func<T> action,
        out T result)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(action);

        if (quarantinedRules.Contains(rule))
        {
            result = default;
            return false;
        }

        try
        {
            result = action();
            return true;
        }
        catch (RegexMatchTimeoutException exception)
        {
            result = default;
            Quarantine(language, rule, exception);
            return false;
        }
    }

    public bool TryGetHighlights(
        SyntaxHighlightLanguage language,
        IHighlightRule rule,
        ReadOnlySpan<string> lines,
        string text,
        string newLineCharacter,
        out List<HighlightSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(rule);

        if (quarantinedRules.Contains(rule))
        {
            highlights = default;
            return false;
        }

        try
        {
            highlights = rule.GetHighlights(lines, text, newLineCharacter);
            return true;
        }
        catch (RegexMatchTimeoutException exception)
        {
            highlights = default;
            Quarantine(language, rule, exception);
            return false;
        }
    }

    public bool TryInferInitialState(
        SyntaxHighlightLanguage language,
        IFragmentAwareStatefulHighlightRule rule,
        ReadOnlySpan<string> lines,
        out int state)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(rule);

        if (quarantinedRules.Contains(rule))
        {
            state = rule.InitialState;
            return false;
        }

        try
        {
            state = rule.InferInitialState(lines);
            return true;
        }
        catch (RegexMatchTimeoutException exception)
        {
            state = rule.InitialState;
            Quarantine(language, rule, exception);
            return false;
        }
    }

    public void ResetRules()
    {
        quarantinedRules.Clear();
    }

    public Color ResolveColor(
        SyntaxHighlightRole role,
        bool isLightTheme,
        Color fallback)
    {
        return Palette?.Resolve(role, isLightTheme, fallback) ?? fallback;
    }

    private void Quarantine(
        SyntaxHighlightLanguage language,
        object rule,
        RegexMatchTimeoutException exception)
    {
        if (!quarantinedRules.Add(rule))
            return;

        RuleQuarantined?.Invoke(
            this,
            new SyntaxHighlightingRuleQuarantinedEventArgs(
                language,
                rule,
                exception));
    }
}
