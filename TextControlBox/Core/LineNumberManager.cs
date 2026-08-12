using System;
using System.Collections.Generic;

namespace TextControlBoxNS.Core;

internal class LineNumberManager
{
    public bool _ShowLineNumbers = true;
    public float _SpaceBetweenLineNumberAndText = 30;

    private string[] customLabels;
    private string widestCustomLabel = "";

    public bool HasCustomLabels => customLabels is not null;

    public void SetCustomLabels(IEnumerable<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        var snapshot = new List<string>();
        string widestLabel = "";
        foreach (string label in labels)
        {
            string normalizedLabel = label ?? "";
            snapshot.Add(normalizedLabel);
            if (normalizedLabel.Length > widestLabel.Length)
                widestLabel = normalizedLabel;
        }

        customLabels = snapshot.ToArray();
        widestCustomLabel = widestLabel;
    }

    public void ClearCustomLabels()
    {
        customLabels = null;
        widestCustomLabel = "";
    }

    public string GetLabel(int lineIndex)
    {
        if (customLabels is null)
            return (lineIndex + 1).ToString();

        return lineIndex >= 0 && lineIndex < customLabels.Length
            ? customLabels[lineIndex]
            : "";
    }

    public string GetWidthReference(int lineCount)
    {
        return customLabels is null
            ? Math.Max(lineCount, 1).ToString()
            : widestCustomLabel;
    }
}
