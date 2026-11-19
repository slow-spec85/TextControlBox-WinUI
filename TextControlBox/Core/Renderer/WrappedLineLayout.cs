using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;

namespace TextControlBoxNS.Core.Renderer;

/// <summary>
/// Indexes the first visual row of each document line, plus the end of the document.
/// The editor still renders and selects text using its original line and column indices.
/// </summary>
internal sealed class WrappedLineLayout
{
    private readonly int[] lineStarts;

    public WrappedLineLayout(ICanvasResourceCreator resourceCreator, CanvasTextFormat format, float width, ReadOnlySpan<string> lines)
    {
        lineStarts = new int[lines.Length + 1];
        for (int line = 0; line < lines.Length; line++)
        {
            int rows = 1;
            if (lines[line].Length > 0)
            {
                using CanvasTextLayout layout = new(resourceCreator, lines[line], format, width, 0);
                rows = Math.Max(1, layout.LineCount);
            }

            lineStarts[line + 1] = checked(lineStarts[line] + rows);
        }
    }

    public int RowCount => lineStarts[^1];

    public int GetStartRow(int line) => lineStarts[line];

    public int GetRowCount(int line) => lineStarts[line + 1] - lineStarts[line];

    public int GetLineAtRow(int row)
    {
        if (RowCount == 0)
            return -1;

        int index = Array.BinarySearch(lineStarts, Math.Clamp(row, 0, RowCount - 1));
        return index >= 0 ? index : ~index - 1;
    }
}
