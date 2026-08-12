using System;
using System.Collections.Generic;
using Windows.UI;

namespace TextControlBoxNS;

/// <summary>
/// Defines light and dark colors for a semantic syntax highlighting role.
/// </summary>
public readonly record struct SyntaxHighlightPaletteEntry(
    SyntaxHighlightRole Role,
    Color Light,
    Color Dark);

/// <summary>
/// Provides immutable semantic color overrides for one text control.
/// </summary>
public sealed class SyntaxHighlightPalette
{
    private readonly IReadOnlyDictionary<SyntaxHighlightRole, SyntaxHighlightPaletteEntry> entries;

    /// <summary>
    /// Creates a palette from semantic color entries.
    /// </summary>
    /// <param name="entries">The color entries to copy into the palette.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is null.</exception>
    /// <exception cref="ArgumentException">A semantic role occurs more than once.</exception>
    public SyntaxHighlightPalette(params SyntaxHighlightPaletteEntry[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Dictionary<SyntaxHighlightRole, SyntaxHighlightPaletteEntry> copy = [];
        foreach (SyntaxHighlightPaletteEntry entry in entries)
        {
            if (!copy.TryAdd(entry.Role, entry))
            {
                throw new ArgumentException(
                    $"A color pair for role '{entry.Role}' is already defined.",
                    nameof(entries));
            }
        }

        this.entries = copy;
    }

    /// <summary>
    /// Gets the color entry registered for a semantic role.
    /// </summary>
    public bool TryGetColors(
        SyntaxHighlightRole role,
        out SyntaxHighlightPaletteEntry colors)
    {
        return entries.TryGetValue(role, out colors);
    }

    internal Color Resolve(
        SyntaxHighlightRole role,
        bool isLightTheme,
        Color fallback)
    {
        return TryGetColors(role, out SyntaxHighlightPaletteEntry colors)
            ? isLightTheme ? colors.Light : colors.Dark
            : fallback;
    }
}
