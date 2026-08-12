using System.Collections.Generic;
using TextControlBoxNS.Core;

namespace TextControlBox.Tests;

internal static class DecorationStoreTestExtensions
{
    public static ResolvedLineDecoration[] GetVisibleDecorations(
        this LineDecorationStore store,
        int firstVisibleLine,
        int lastVisibleLine)
    {
        List<ResolvedLineDecoration> result = [];
        store.AppendVisibleDecorations(firstVisibleLine, lastVisibleLine, result);
        return [.. result];
    }

    public static ResolvedLineGutterDecoration[] GetVisibleDecorations(
        this LineGutterDecorationStore store,
        int firstVisibleLine,
        int lastVisibleLine)
    {
        List<ResolvedLineGutterDecoration> result = [];
        store.AppendVisibleDecorations(firstVisibleLine, lastVisibleLine, result);
        return [.. result];
    }

    public static ResolvedTextRangeDecoration[] GetVisibleDecorations(
        this TextDecorationStore store,
        int firstVisibleLine,
        int lastVisibleLine)
    {
        List<ResolvedTextRangeDecoration> result = [];
        store.AppendVisibleDecorations(firstVisibleLine, lastVisibleLine, result);
        return [.. result];
    }
}
