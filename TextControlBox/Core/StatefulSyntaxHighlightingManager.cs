using System;
using System.Collections.Generic;
using TextControlBoxNS.Core.Text;
using TextControlBoxNS.Models;

namespace TextControlBoxNS.Core;

internal sealed class StatefulSyntaxHighlightingManager
{
    private static readonly IStatefulHighlightRule[] EmptyRules = [];
    private readonly List<string> cachedLineTexts = [];
    private readonly List<HighlightSpan> lineHighlights = [];
    private TextManager textManager;
    private SyntaxHighlightingSession session;
    private SyntaxHighlightLanguage language;
    private IStatefulHighlightRule[] rules = EmptyRules;
    private List<int>[] statesAfterByRule = [];
    private int invalidFromLine = -1;

    public long Revision { get; private set; }

    public void Init(
        TextManager manager,
        EventsManager eventsManager,
        SyntaxHighlightingSession highlightingSession)
    {
        textManager = manager;
        session = highlightingSession;
        eventsManager.DocumentChanged += OnDocumentChanged;
    }

    public IReadOnlyList<HighlightSpan> GetHighlights(
        SyntaxHighlightLanguage syntaxLanguage,
        int startLine,
        ReadOnlySpan<string> lines,
        string newLineCharacter)
    {
        UseLanguage(syntaxLanguage);
        if (rules.Length == 0 || lines.Length == 0)
            return Array.Empty<HighlightSpan>();

        var result = new List<HighlightSpan>();

        int lastLine = checked(startLine + lines.Length - 1);
        EnsureStatesThrough(lastLine);

        int textOffset = 0;
        for (int relativeLine = 0; relativeLine < lines.Length; relativeLine++)
        {
            int documentLine = startLine + relativeLine;
            string line = lines[relativeLine];

            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                lineHighlights.Clear();
                IStatefulHighlightRule rule = rules[ruleIndex];
                bool completed = session.TryExecute(rule, () => rule.GetHighlights(
                    documentLine,
                    line.AsSpan(),
                    GetStateBefore(documentLine, ruleIndex),
                    lineHighlights));
                if (!completed)
                {
                    lineHighlights.Clear();
                    continue;
                }

                foreach (HighlightSpan highlight in lineHighlights)
                {
                    if (highlight.Start < 0
                        || highlight.Length <= 0
                        || highlight.Start > line.Length - highlight.Length)
                    {
                        continue;
                    }

                    result.Add(new HighlightSpan
                    {
                        Start = checked(textOffset + highlight.Start),
                        Length = highlight.Length,
                        ColorLight = highlight.ColorLight,
                        ColorDark = highlight.ColorDark,
                        Style = highlight.Style,
                        Role = highlight.Role,
                    });
                }
            }

            textOffset = checked(textOffset + line.Length);
            if (relativeLine < lines.Length - 1)
                textOffset = checked(textOffset + newLineCharacter.Length);
        }

        return result;
    }

    public void Reset(SyntaxHighlightLanguage syntaxLanguage)
    {
        language = syntaxLanguage;
        rules = syntaxLanguage?.StatefulHighlightRules ?? EmptyRules;
        cachedLineTexts.Clear();
        statesAfterByRule = new List<int>[rules.Length];
        for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            statesAfterByRule[ruleIndex] = [];
        invalidFromLine = -1;
        Revision = checked(Revision + 1);
    }

    private void UseLanguage(SyntaxHighlightLanguage syntaxLanguage)
    {
        IStatefulHighlightRule[] requestedRules = syntaxLanguage?.StatefulHighlightRules ?? EmptyRules;
        if (!ReferenceEquals(language, syntaxLanguage) || !ReferenceEquals(rules, requestedRules))
            Reset(syntaxLanguage);
    }

    private void OnDocumentChanged(DocumentChangedEventArgs args)
    {
        bool cachedStateAffected = false;
        foreach (DocumentChange change in args.Changes)
            cachedStateAffected |= ApplyChange(change);

        if (cachedStateAffected)
            Revision = checked(Revision + 1);
    }

    private bool ApplyChange(DocumentChange change)
    {
        if (change.StartLine >= cachedLineTexts.Count)
            return false;

        invalidFromLine = invalidFromLine < 0
            ? change.StartLine
            : Math.Min(invalidFromLine, change.StartLine);

        if (change.RemovedLineCount != change.InsertedLineCount)
        {
            int invalidCachedLineCount = cachedLineTexts.Count - change.StartLine;
            cachedLineTexts.RemoveRange(change.StartLine, invalidCachedLineCount);
            foreach (List<int> statesAfter in statesAfterByRule)
                statesAfter.RemoveRange(change.StartLine, invalidCachedLineCount);
            return true;
        }

        int changedCachedLines = Math.Min(
            change.RemovedLineCount,
            cachedLineTexts.Count - change.StartLine);
        for (int index = 0; index < changedCachedLines; index++)
            cachedLineTexts[change.StartLine + index] = null;

        return true;
    }

    private void EnsureStatesThrough(int targetLine)
    {
        if (targetLine < 0 || rules.Length == 0)
            return;

        if (invalidFromLine >= 0)
            RecalculateInvalidStates(targetLine);

        while (cachedLineTexts.Count <= targetLine)
        {
            int lineNumber = cachedLineTexts.Count;
            AppendLineState(lineNumber);
        }
    }

    private void RecalculateInvalidStates(int targetLine)
    {
        int lineNumber = invalidFromLine;
        while (lineNumber < cachedLineTexts.Count && lineNumber <= targetLine)
        {
            string oldText = cachedLineTexts[lineNumber];
            bool statesConverged = UpdateLineState(lineNumber);
            lineNumber++;

            if (oldText is not null
                && ReferenceEquals(oldText, cachedLineTexts[lineNumber - 1])
                && statesConverged)
            {
                invalidFromLine = -1;
                return;
            }
        }

        invalidFromLine = lineNumber < cachedLineTexts.Count ? lineNumber : -1;
    }

    private void AppendLineState(int lineNumber)
    {
        string text = textManager.GetLineText(lineNumber);
        for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            IStatefulHighlightRule rule = rules[ruleIndex];
            int stateAfter = rule.InitialState;
            session.TryExecute(rule, () => stateAfter = rule.GetStateAfterLine(
                lineNumber,
                text.AsSpan(),
                GetStateBefore(lineNumber, ruleIndex)));
            statesAfterByRule[ruleIndex].Add(stateAfter);
        }

        cachedLineTexts.Add(text);
    }

    private bool UpdateLineState(int lineNumber)
    {
        string text = textManager.GetLineText(lineNumber);
        bool statesConverged = true;
        for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            IStatefulHighlightRule rule = rules[ruleIndex];
            int stateAfter = rule.InitialState;
            session.TryExecute(rule, () => stateAfter = rule.GetStateAfterLine(
                lineNumber,
                text.AsSpan(),
                GetStateBefore(lineNumber, ruleIndex)));
            statesConverged &= statesAfterByRule[ruleIndex][lineNumber] == stateAfter;
            statesAfterByRule[ruleIndex][lineNumber] = stateAfter;
        }

        cachedLineTexts[lineNumber] = text;
        return statesConverged;
    }

    private int GetStateBefore(int lineNumber, int ruleIndex)
    {
        if (session.IsQuarantined(rules[ruleIndex]))
            return rules[ruleIndex].InitialState;

        return lineNumber == 0
            ? rules[ruleIndex].InitialState
            : statesAfterByRule[ruleIndex][lineNumber - 1];
    }
}
