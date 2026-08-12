using System;
using System.Collections.Generic;
using System.Linq;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBoxNS.Core;

internal readonly record struct ResolvedLineGutterDecoration(
    int Line,
    string Text,
    Color ForegroundColor,
    Color? BackgroundColor,
    int Priority,
    long Sequence) : IPaintOrderedDecoration, ILineIndexedDecoration;

internal sealed class LineGutterDecorationStore
{
    private readonly DecorationGroups<ResolvedLineGutterDecoration> groups = new();
    private ResolvedLineGutterDecoration[] decorationsByLine = [];
    private Action invalidate = static () => { };

    public bool HasDecorations => decorationsByLine.Length > 0;

    public void Init(Action invalidateAction)
    {
        invalidate = invalidateAction ?? throw new ArgumentNullException(nameof(invalidateAction));
    }

    public void SetGroup(
        string groupKey,
        IEnumerable<LineGutterDecoration> decorations,
        int documentLineCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKey);
        ArgumentNullException.ThrowIfNull(decorations);

        var replacement = new List<ResolvedLineGutterDecoration>();
        foreach (LineGutterDecoration decoration in decorations)
        {
            if (decoration is null)
                throw new ArgumentException("A decoration group cannot contain null values.", nameof(decorations));

            if (decoration.Line >= documentLineCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decorations),
                    $"Decoration line {decoration.Line} is outside the document line range.");
            }

            replacement.Add(new ResolvedLineGutterDecoration(
                decoration.Line,
                decoration.Text,
                decoration.ForegroundColor,
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
        List<ResolvedLineGutterDecoration> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();

        if (firstVisibleLine < 0 || lastVisibleLine < firstVisibleLine || decorationsByLine.Length == 0)
            return;

        int start = LineDecorationIndex.LowerBound(decorationsByLine, firstVisibleLine);
        int end = LineDecorationIndex.UpperBound(decorationsByLine, lastVisibleLine);
        for (int index = start; index < end; index++)
            destination.Add(decorationsByLine[index]);

        destination.Sort(DecorationPaintOrderComparer<ResolvedLineGutterDecoration>.Instance);
    }

    private void RebuildIndex()
    {
        decorationsByLine = groups.Values
            .SelectMany(static group => group)
            .OrderBy(static decoration => decoration.Line)
            .ThenBy(static decoration => decoration.Sequence)
            .ToArray();
    }

}
