using System;
using System.Collections.Generic;

namespace TextControlBoxNS.Core;

internal interface IPaintOrderedDecoration
{
    int Priority { get; }

    long Sequence { get; }
}

internal interface ILineIndexedDecoration
{
    int Line { get; }
}

internal sealed class DecorationPaintOrderComparer<T> : IComparer<T>
    where T : IPaintOrderedDecoration
{
    public static DecorationPaintOrderComparer<T> Instance { get; } = new();

    public int Compare(T left, T right)
    {
        int priorityComparison = left.Priority.CompareTo(right.Priority);
        return priorityComparison != 0
            ? priorityComparison
            : left.Sequence.CompareTo(right.Sequence);
    }
}

internal static class LineDecorationIndex
{
    public static int LowerBound<T>(T[] decorations, int line)
        where T : ILineIndexedDecoration
    {
        int low = 0;
        int high = decorations.Length;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (decorations[middle].Line < line)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    public static int UpperBound<T>(T[] decorations, int line)
        where T : ILineIndexedDecoration
    {
        int low = 0;
        int high = decorations.Length;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (decorations[middle].Line <= line)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}

internal sealed class DecorationGroups<T>
{
    private readonly Dictionary<string, List<T>> groups = new(StringComparer.Ordinal);
    private long nextSequence;

    public IEnumerable<List<T>> Values => groups.Values;

    public bool IsEmpty => groups.Count == 0;

    public long TakeSequence()
    {
        return nextSequence++;
    }

    public void Replace(string groupKey, List<T> replacement)
    {
        if (replacement.Count == 0)
            groups.Remove(groupKey);
        else
            groups[groupKey] = replacement;
    }

    public bool Remove(string groupKey)
    {
        return groups.Remove(groupKey);
    }

    public bool Clear()
    {
        if (groups.Count == 0)
            return false;

        groups.Clear();
        return true;
    }

    public void RemoveEmpty()
    {
        List<string> emptyKeys = [];
        foreach (KeyValuePair<string, List<T>> group in groups)
        {
            if (group.Value.Count == 0)
                emptyKeys.Add(group.Key);
        }

        foreach (string key in emptyKeys)
            groups.Remove(key);
    }
}
