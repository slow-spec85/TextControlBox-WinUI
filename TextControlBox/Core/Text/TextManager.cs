using Collections.Pooled;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TextControlBoxNS.Helper;
using TextControlBoxNS.Models;

namespace TextControlBoxNS.Core.Text;

internal class TextManager
{
    public const float DefaultLineSpacing = 2;

    private EventsManager eventsManager;
    private LineDecorationStore lineDecorationStore;
    private TextDecorationStore textDecorationStore;
    private DocumentChangeManager documentChangeManager;

    public PooledList<string> totalLines = new PooledList<string>(0);

    public int _FontSize = 18;
    public float _LineSpacing = DefaultLineSpacing;
    private LineEnding _LineEnding = LineEnding.CRLF;
    public LineEnding LineEnding 
    { 
        get => _LineEnding;
        set 
        {
            _LineEnding = value;
            eventsManager.CallLineEndingChanged(value);
            NewLineCharacter = LineEndings.LineEndingToString(value);
        }
    }

    public void Init(
        EventsManager eventsManager,
        LineDecorationStore decorations,
        TextDecorationStore textDecorations,
        DocumentChangeManager documentChanges)
    {
        this.eventsManager = eventsManager;
        lineDecorationStore = decorations;
        textDecorationStore = textDecorations;
        documentChangeManager = documentChanges;
    }

    public DocumentChangeBatch BeginDocumentChangeBatch(DocumentChangeReason reason)
    {
        return documentChangeManager.BeginBatch(reason);
    }

    public FontFamily _FontFamily = new FontFamily("Consolas");
    public string NewLineCharacter = "\r\n";
    public SyntaxHighlightLanguage _SyntaxHighlighting = null;
    public int MaxFontsize = 125;
    public int MinFontSize = 3;
    public bool _IsReadOnly = false;

    public int GetLineLength(int line)
    {
        return GetLineText(line).Length;
    }

    public int LinesCount => totalLines.Count;

    public string GetLineText(int line)
    {
        if (line == -1)
            return totalLines[^1];

        if (line >= totalLines.Count || line < 0)
            throw new IndexOutOfRangeException("GetLineText provided line index out of range of valid values.");

        return totalLines[line];
    }

    public string GetLinesAsString()
    {
        if (totalLines.Count == 1 && totalLines[0].Length == 0)
            return "";
        
        return string.Join(NewLineCharacter, totalLines);
    }
    public string GetLinesAsString(int start, int count)
    {
        if (start < 0 || count < 0)
            throw new IndexOutOfRangeException("GetLinesAsString start or count less then zero");

        if (start + count == 0)
            return "";

        if (start == 0 && count >= totalLines.Count)
            return GetLinesAsString();

        if (start + count > totalLines.Count)
            throw new IndexOutOfRangeException("GetLinesAsString start + count is out of range of the size of the collection");

        return string.Join(NewLineCharacter, totalLines.Span.Slice(start, count).ToArray());
    }

    public LineSliceResult GetLinesForRendering(int start, int count)
    {
        if (start < 0 || count < 0 || start + count > totalLines.Count)
            return new LineSliceResult(string.Empty, ReadOnlySpan<string>.Empty);

        ReadOnlySpan<string> linesSlice = totalLines.Span.Slice(start, count);
        string joinedText = JoinLines(start, count);

        return new LineSliceResult(joinedText, linesSlice);
    }

    public void SetLineText(int line, string text)
    {
        int actualLine = line;
        // -1 is the last line.
        if (line == -1)
            actualLine = totalLines.Count - 1;

        if (actualLine >= totalLines.Count || actualLine < 0)
            throw new IndexOutOfRangeException("SetLineText provided line index out of range of valid values.");

        if (totalLines.Span[actualLine].Equals(text, StringComparison.Ordinal))
            return;

        totalLines.Span[actualLine] = text;
        textDecorationStore.OnLineTextChanged(actualLine, text.Length);
        documentChangeManager.RecordChange(actualLine, 1, 1);
    }
    public void String_AddToEnd(int line, string add)
    {
        if (add.Length == 0)
            return;

        totalLines.Span[line] += add;
        textDecorationStore.OnLineTextChanged(line, totalLines.Span[line].Length);
        documentChangeManager.RecordChange(line, 1, 1);
    }
    public void String_AddToStart(int line, string add)
    {
        if (add.Length == 0)
            return;

        totalLines[line] = add + totalLines[line];
        textDecorationStore.OnLineTextChanged(line, totalLines.Span[line].Length);
        documentChangeManager.RecordChange(line, 1, 1);
    }

    public void DeleteAt(int index)
    {
        if (index >= totalLines.Count || index < 0)
            throw new IndexOutOfRangeException("DeleteAt: provided index is out of range");
        totalLines.RemoveAt(index);
        lineDecorationStore.OnLinesRemoved(index, 1);
        textDecorationStore.OnLinesRemoved(index, 1);
        documentChangeManager.RecordChange(index, 1, 0);
    }

    public void InsertOrAddRange(IEnumerable<string> lines, int index)
    {
        var lineList = lines as IList<string> ?? lines.ToList();
        if (lineList.Count == 0)
            return;

        int insertionIndex;
        if (index >= totalLines.Count)
        {
            insertionIndex = totalLines.Count;
            totalLines.AddRange(lineList);
        }
        else
        {
            insertionIndex = index < 0 ? 0 : index;
            totalLines.Capacity = Math.Max(totalLines.Count + lineList.Count, totalLines.Capacity);
            totalLines.InsertRange(insertionIndex, lineList);
        }

        lineDecorationStore.OnLinesInserted(insertionIndex, lineList.Count);
        textDecorationStore.OnLinesInserted(insertionIndex, lineList.Count);
        documentChangeManager.RecordChange(insertionIndex, 0, lineList.Count);
    }
    public void InsertOrAdd(int index, string lineText)
    {
        int insertionIndex;
        if (index >= totalLines.Count || index == -1)
        {
            insertionIndex = totalLines.Count;
            totalLines.Add(lineText);
        }
        else
        {
            insertionIndex = index;
            totalLines.Insert(index, lineText);
        }

        lineDecorationStore.OnLinesInserted(insertionIndex, 1);
        textDecorationStore.OnLinesInserted(insertionIndex, 1);
        documentChangeManager.RecordChange(insertionIndex, 0, 1);
    }

    public void ClearText(bool addNewLine = false)
    {
        int removedLineCount = totalLines.Count;
        totalLines.Clear();
        ListHelper.GCList(totalLines);
        lineDecorationStore.Clear();
        textDecorationStore.Clear();

        if (addNewLine)
            totalLines.Add("");

        documentChangeManager.RecordChange(0, removedLineCount, addNewLine ? 1 : 0);
    }
    public void CleanUp()
    {
        Debug.WriteLine("Collect GC");
        ListHelper.GCList(totalLines);
    }
    public void RemoveRange(int index, int count)
    {
        if (index + count > totalLines.Count)
            throw new IndexOutOfRangeException("RemoveRange index + count out of range");

        totalLines.RemoveRange(index, count);
        totalLines.TrimExcess();
        lineDecorationStore.OnLinesRemoved(index, count);
        textDecorationStore.OnLinesRemoved(index, count);
        documentChangeManager.RecordChange(index, count, 0);

        //clear up the memory of the list if more than 1_000_000 items are removed
        if (count > 1_000_000)
            ListHelper.GCList(totalLines);
    }

    public void AddLine(string content = "")
    {
        int insertionIndex = totalLines.Count;
        totalLines.Add(content);
        lineDecorationStore.OnLinesInserted(insertionIndex, 1);
        textDecorationStore.OnLinesInserted(insertionIndex, 1);
        documentChangeManager.RecordChange(insertionIndex, 0, 1);
    }
    public bool SwapLines(int originalIndex, int newIndex)
    {
        if (originalIndex < 0 || originalIndex >= totalLines.Count ||
            newIndex < 0 || newIndex >= totalLines.Count)
            return false;

        if (originalIndex == newIndex)
            return true;

        (totalLines[originalIndex], totalLines[newIndex]) = (totalLines[newIndex], totalLines[originalIndex]);
        textDecorationStore.OnLinesSwapped(originalIndex, newIndex);
        int firstChangedLine = Math.Min(originalIndex, newIndex);
        int secondChangedLine = Math.Max(originalIndex, newIndex);
        documentChangeManager.RecordChange(firstChangedLine, 1, 1);
        documentChangeManager.RecordChange(secondChangedLine, 1, 1);
        return true;
    }

    public int CountCharacters()
    {
        int count = 0;
        int lineEndingLength = LineEndings.LineEndingToString(this.LineEnding).Length;

        for (int i = 0; i < totalLines.Count; i++)
        {
            count += totalLines.Span[i].Length;

            //add line ending for all lines except the last
            if (i < totalLines.Count - 1)
                count += lineEndingLength;
        }

        return count;
    }

    public int CountWords()
    {
        int wordCount = 0;

        foreach (var line in totalLines)
        {
            var span = line.AsSpan();
            int index = 0;

            while (index < span.Length)
            {
                while (index < span.Length && char.IsWhiteSpace(span[index]))
                {
                    index++;
                }

                if (index < span.Length)
                {
                    wordCount++;
                }

                while (index < span.Length && !char.IsWhiteSpace(span[index]))
                {
                    index++;
                }
            }
        }

        return wordCount;
    }

    private string JoinLines(int start, int count)
    {
        if (count == 0)
            return string.Empty;

        int resultLength = checked(NewLineCharacter.Length * (count - 1));
        ReadOnlySpan<string> lines = totalLines.Span.Slice(start, count);
        foreach (string line in lines)
            resultLength = checked(resultLength + line.Length);

        var state = (Lines: totalLines, Start: start, Count: count, Separator: NewLineCharacter);
        return string.Create(resultLength, state, static (destination, joinState) =>
        {
            int destinationIndex = 0;
            ReadOnlySpan<string> sourceLines = joinState.Lines.Span.Slice(joinState.Start, joinState.Count);
            for (int lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++)
            {
                if (lineIndex > 0)
                {
                    joinState.Separator.AsSpan().CopyTo(destination[destinationIndex..]);
                    destinationIndex += joinState.Separator.Length;
                }

                sourceLines[lineIndex].AsSpan().CopyTo(destination[destinationIndex..]);
                destinationIndex += sourceLines[lineIndex].Length;
            }
        });
    }
}
