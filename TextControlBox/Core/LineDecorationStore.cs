using System;
using System.Collections.Generic;
using System.Linq;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBoxNS.Core;

internal readonly record struct ResolvedLineDecoration(
    int StartLine,
    int EndLine,
    Color BackgroundColor,
    int Priority,
    long Sequence) : IPaintOrderedDecoration;

internal sealed class LineDecorationStore
{
    private readonly DecorationGroups<ResolvedLineDecoration> groups = new();
    private ResolvedLineDecoration[] decorationsByStartLine = [];
    private int[] maximumEndLinePrefixes = [];
    private Action invalidate = static () => { };

    public void Init(Action invalidateAction)
    {
        invalidate = invalidateAction ?? throw new ArgumentNullException(nameof(invalidateAction));
    }

    public void SetGroup(
        string groupKey,
        IEnumerable<LineDecoration> decorations,
        int documentLineCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKey);
        ArgumentNullException.ThrowIfNull(decorations);

        var replacement = new List<ResolvedLineDecoration>();
        foreach (LineDecoration decoration in decorations)
        {
            if (decoration is null)
                throw new ArgumentException("A decoration group cannot contain null values.", nameof(decorations));

            if (decoration.EndLine >= documentLineCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decorations),
                    $"Decoration line {decoration.EndLine} is outside the document line range.");
            }

            replacement.Add(new ResolvedLineDecoration(
                decoration.StartLine,
                decoration.EndLine,
                decoration.BackgroundColor,
                decoration.Priority,
                groups.TakeSequence()));
        }

        groups.Replace(groupKey, replacement);
        RebuildIndex();
        invalidate();
    }

    public bool RemoveGroup(string groupKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKey);

        if (!groups.Remove(groupKey))
            return false;

        RebuildIndex();
        invalidate();
        return true;
    }

    public void Clear()
    {
        if (!groups.Clear())
            return;

        RebuildIndex();
        invalidate();
    }

    public void AppendVisibleDecorations(
        int firstVisibleLine,
        int lastVisibleLine,
        List<ResolvedLineDecoration> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();

        if (firstVisibleLine < 0 || lastVisibleLine < firstVisibleLine || decorationsByStartLine.Length == 0)
            return;

        int firstCandidate = LowerBound(maximumEndLinePrefixes, firstVisibleLine);
        int candidateEnd = UpperBoundByStartLine(lastVisibleLine);

        for (int index = firstCandidate; index < candidateEnd; index++)
        {
            ResolvedLineDecoration decoration = decorationsByStartLine[index];
            if (decoration.EndLine < firstVisibleLine)
                continue;

            destination.Add(decoration with
            {
                StartLine = Math.Max(decoration.StartLine, firstVisibleLine),
                EndLine = Math.Min(decoration.EndLine, lastVisibleLine),
            });
        }

        destination.Sort(DecorationPaintOrderComparer<ResolvedLineDecoration>.Instance);
    }

    public void OnLinesInserted(int index, int count)
    {
        if (count <= 0 || groups.IsEmpty)
            return;

        TransformGroups(decoration =>
        {
            if (index <= decoration.StartLine)
            {
                return decoration with
                {
                    StartLine = decoration.StartLine + count,
                    EndLine = decoration.EndLine + count,
                };
            }

            if (index <= decoration.EndLine)
                return decoration with { EndLine = decoration.EndLine + count };

            return decoration;
        });
    }

    public void OnLinesRemoved(int index, int count)
    {
        if (count <= 0 || groups.IsEmpty)
            return;

        int removedEnd = checked(index + count - 1);
        foreach (List<ResolvedLineDecoration> group in groups.Values)
        {
            for (int decorationIndex = group.Count - 1; decorationIndex >= 0; decorationIndex--)
            {
                ResolvedLineDecoration decoration = group[decorationIndex];

                if (decoration.EndLine < index)
                    continue;

                if (decoration.StartLine > removedEnd)
                {
                    group[decorationIndex] = decoration with
                    {
                        StartLine = decoration.StartLine - count,
                        EndLine = decoration.EndLine - count,
                    };
                    continue;
                }

                bool hasLinesBeforeRemoval = decoration.StartLine < index;
                bool hasLinesAfterRemoval = decoration.EndLine > removedEnd;
                if (!hasLinesBeforeRemoval && !hasLinesAfterRemoval)
                {
                    group.RemoveAt(decorationIndex);
                    continue;
                }

                int newStartLine = hasLinesBeforeRemoval ? decoration.StartLine : index;
                int newEndLine = hasLinesAfterRemoval
                    ? decoration.EndLine - count
                    : index - 1;
                group[decorationIndex] = decoration with
                {
                    StartLine = newStartLine,
                    EndLine = newEndLine,
                };
            }
        }

        RemoveEmptyGroupsAndRebuild();
        invalidate();
    }

    private void TransformGroups(
        Func<ResolvedLineDecoration, ResolvedLineDecoration> transform)
    {
        foreach (List<ResolvedLineDecoration> group in groups.Values)
        {
            for (int index = 0; index < group.Count; index++)
                group[index] = transform(group[index]);
        }

        RebuildIndex();
        invalidate();
    }

    private void RemoveEmptyGroupsAndRebuild()
    {
        groups.RemoveEmpty();
        RebuildIndex();
    }

    private void RebuildIndex()
    {
        decorationsByStartLine = groups.Values
            .SelectMany(static group => group)
            .OrderBy(static decoration => decoration.StartLine)
            .ThenBy(static decoration => decoration.EndLine)
            .ThenBy(static decoration => decoration.Sequence)
            .ToArray();

        maximumEndLinePrefixes = new int[decorationsByStartLine.Length];
        int maximumEndLine = -1;
        for (int index = 0; index < decorationsByStartLine.Length; index++)
        {
            maximumEndLine = Math.Max(maximumEndLine, decorationsByStartLine[index].EndLine);
            maximumEndLinePrefixes[index] = maximumEndLine;
        }
    }

    private int UpperBoundByStartLine(int line)
    {
        int low = 0;
        int high = decorationsByStartLine.Length;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (decorationsByStartLine[middle].StartLine <= line)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    private static int LowerBound(int[] values, int target)
    {
        int low = 0;
        int high = values.Length;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (values[middle] < target)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
