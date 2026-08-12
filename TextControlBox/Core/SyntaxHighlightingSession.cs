using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.UI;

namespace TextControlBoxNS.Core;

internal sealed class SyntaxHighlightingSession
{
    private readonly HashSet<object> quarantinedRules = new(ReferenceEqualityComparer.Instance);

    public SyntaxHighlightPalette Palette { get; set; }

    public bool IsQuarantined(object rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return quarantinedRules.Contains(rule);
    }

    public void Quarantine(object rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        quarantinedRules.Add(rule);
    }

    public bool TryExecute(object rule, Action action)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(action);

        if (quarantinedRules.Contains(rule))
            return false;

        try
        {
            action();
            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            Quarantine(rule);
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
}
