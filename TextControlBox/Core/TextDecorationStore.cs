using System;
using System.Collections.Generic;
using System.Linq;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Models;
using Windows.UI;

namespace TextControlBoxNS.Core;

internal readonly record struct ResolvedTextRangeDecoration(
    int Line,
    int StartColumn,
    int Length,
    Color? ForegroundColor,
    Color? BackgroundColor,
    Color? BorderColor,
    float BorderThickness,
    float CornerRadius,
    float HorizontalPadding,
    int Priority,
    long Sequence) : IPaintOrderedDecoration, ILineIndexedDecoration;

internal sealed class TextDecorationStore
{
    private readonly DecorationGroups<ResolvedTextRangeDecoration> groups = new();
    private ResolvedTextRangeDecoration[] decorationsByLine = [];
    private Action invalidateBackground = static () => { };
    private Action invalidateText = static () => { };

    public long Revision { get; private set; }

    public void Init(Action backgroundInvalidation, Action textInvalidation)
    {
        invalidateBackground = backgroundInvalidation
            ?? throw new ArgumentNullException(nameof(backgroundInvalidation));
        invalidateText = textInvalidation
            ?? throw new ArgumentNullException(nameof(textInvalidation));
    }

    public void SetGroup(
        string groupKey,
        IEnumerable<TextRangeDecoration> decorations,
        TextManager textManager)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKey);
        ArgumentNullException.ThrowIfNull(decorations);
        ArgumentNullException.ThrowIfNull(textManager);

        var replacement = new List<ResolvedTextRangeDecoration>();
        foreach (TextRangeDecoration decoration in decorations)
        {
            if (decoration is null)
                throw new ArgumentException("A decoration group cannot contain null values.", nameof(decorations));

            if (decoration.Line >= textManager.LinesCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decorations),
                    $"Decoration line {decoration.Line} is outside the document line range.");
            }

            int lineLength = textManager.GetLineLength(decoration.Line);
            if (decoration.StartColumn > lineLength - decoration.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decorations),
                    $"Decoration range at line {decoration.Line} is outside the line text.");
            }

            replacement.Add(new ResolvedTextRangeDecoration(
                decoration.Line,
                decoration.StartColumn,
                decoration.Length,
                decoration.ForegroundColor,
                decoration.BackgroundColor,
                decoration.BorderColor,
                decoration.BorderThickness,
                decoration.CornerRadius,
                decoration.HorizontalPadding,
                decoration.Priority,
                groups.TakeSequence()));
        }

        groups.Replace(groupKey, replacement);
        RebuildAndInvalidate();
    }

    public bool RemoveGroup(string groupKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKey);
        if (!groups.Remove(groupKey))
            return false;

        RebuildAndInvalidate();
        return true;
    }

    public void Clear()
    {
        if (!groups.Clear())
            return;

        RebuildAndInvalidate();
    }

    public void AppendVisibleDecorations(
        int firstVisibleLine,
        int lastVisibleLine,
        List<ResolvedTextRangeDecoration> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();

        if (firstVisibleLine < 0 || lastVisibleLine < firstVisibleLine || decorationsByLine.Length == 0)
            return;

        int start = LineDecorationIndex.LowerBound(decorationsByLine, firstVisibleLine);
        int end = LineDecorationIndex.UpperBound(decorationsByLine, lastVisibleLine);
        for (int index = start; index < end; index++)
            destination.Add(decorationsByLine[index]);

        destination.Sort(DecorationPaintOrderComparer<ResolvedTextRangeDecoration>.Instance);
    }

    public void OnLinesInserted(int index, int count)
    {
        if (count <= 0 || groups.IsEmpty)
            return;

        TransformGroups(decoration => index <= decoration.Line
            ? decoration with { Line = checked(decoration.Line + count) }
            : decoration);
    }

    public void OnLinesRemoved(int index, int count)
    {
        if (count <= 0 || groups.IsEmpty)
            return;

        int removedEnd = checked(index + count - 1);
        foreach (List<ResolvedTextRangeDecoration> group in groups.Values)
        {
            for (int decorationIndex = group.Count - 1; decorationIndex >= 0; decorationIndex--)
            {
                ResolvedTextRangeDecoration decoration = group[decorationIndex];
                if (decoration.Line >= index && decoration.Line <= removedEnd)
                    group.RemoveAt(decorationIndex);
                else if (decoration.Line > removedEnd)
                    group[decorationIndex] = decoration with { Line = decoration.Line - count };
            }
        }

        RemoveEmptyGroups();
        RebuildAndInvalidate();
    }

    public void OnLineTextChanged(int line, int newLength)
    {
        if (groups.IsEmpty)
            return;

        bool indexChanged = false;
        foreach (List<ResolvedTextRangeDecoration> group in groups.Values)
        {
            for (int decorationIndex = group.Count - 1; decorationIndex >= 0; decorationIndex--)
            {
                ResolvedTextRangeDecoration decoration = group[decorationIndex];
                if (decoration.Line != line)
                    continue;

                if (decoration.StartColumn >= newLength)
                {
                    group.RemoveAt(decorationIndex);
                    indexChanged = true;
                    continue;
                }

                int availableLength = newLength - decoration.StartColumn;
                if (decoration.Length > availableLength)
                {
                    group[decorationIndex] = decoration with { Length = availableLength };
                    indexChanged = true;
                }
            }
        }

        if (indexChanged)
        {
            RemoveEmptyGroups();
            RebuildAndInvalidate();
        }
        else
        {
            Invalidate();
        }
    }

    public void OnLinesSwapped(int firstLine, int secondLine)
    {
        if (firstLine == secondLine || groups.IsEmpty)
            return;

        TransformGroups(decoration => decoration.Line switch
        {
            var line when line == firstLine => decoration with { Line = secondLine },
            var line when line == secondLine => decoration with { Line = firstLine },
            _ => decoration,
        });
    }

    private void TransformGroups(
        Func<ResolvedTextRangeDecoration, ResolvedTextRangeDecoration> transform)
    {
        foreach (List<ResolvedTextRangeDecoration> group in groups.Values)
        {
            for (int index = 0; index < group.Count; index++)
                group[index] = transform(group[index]);
        }

        RebuildAndInvalidate();
    }

    private void RemoveEmptyGroups()
    {
        groups.RemoveEmpty();
    }

    private void RebuildAndInvalidate()
    {
        decorationsByLine = groups.Values
            .SelectMany(static group => group)
            .OrderBy(static decoration => decoration.Line)
            .ThenBy(static decoration => decoration.StartColumn)
            .ThenBy(static decoration => decoration.Sequence)
            .ToArray();

        Invalidate();
    }

    private void Invalidate()
    {
        Revision++;
        invalidateBackground();
        invalidateText();
    }

}
