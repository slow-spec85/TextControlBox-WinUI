using System;
using System.Text.RegularExpressions;

namespace TextControlBoxNS.Models;

/// <summary>
/// Provides diagnostic information about a syntax highlighting rule that exceeded its
/// regular expression match timeout and was disabled for the current control session.
/// </summary>
public sealed class SyntaxHighlightingRuleQuarantinedEventArgs : EventArgs
{
    internal SyntaxHighlightingRuleQuarantinedEventArgs(
        SyntaxHighlightLanguage language,
        object rule,
        RegexMatchTimeoutException exception)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(exception);

        Language = language;
        Rule = rule;
        Exception = exception;
        Pattern = exception.Pattern ?? string.Empty;
        MatchTimeout = exception.MatchTimeout;
        InputLength = exception.Input?.Length ?? 0;
    }

    /// <summary>Gets the active language definition.</summary>
    public SyntaxHighlightLanguage Language { get; }

    /// <summary>Gets the rule instance that was placed in quarantine.</summary>
    public object Rule { get; }

    /// <summary>Gets the concrete runtime type of the quarantined rule.</summary>
    public Type RuleType => Rule.GetType();

    /// <summary>Gets the regular expression pattern reported by the timeout.</summary>
    public string Pattern { get; }

    /// <summary>Gets the configured match timeout.</summary>
    public TimeSpan MatchTimeout { get; }

    /// <summary>
    /// Gets the length of the input that triggered the timeout. The input itself is not copied
    /// into this property because it may contain sensitive document text.
    /// </summary>
    public int InputLength { get; }

    /// <summary>
    /// Gets the original exception. Its <see cref="RegexMatchTimeoutException.Input"/> property
    /// can contain document text and should not be logged without an explicit privacy decision.
    /// </summary>
    public RegexMatchTimeoutException Exception { get; }
}
