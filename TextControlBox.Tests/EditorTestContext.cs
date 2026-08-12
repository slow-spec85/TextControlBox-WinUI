using System;
using TextControlBoxNS.Core;
using TextControlBoxNS.Core.Text;

namespace TextControlBox.Tests;

internal sealed class EditorTestContext
{
    private EditorTestContext(
        EventsManager eventsManager,
        DocumentChangeManager documentChangeManager,
        LineDecorationStore lineDecorations,
        TextDecorationStore textDecorations,
        TextManager textManager)
    {
        Events = eventsManager;
        DocumentChanges = documentChangeManager;
        LineDecorations = lineDecorations;
        TextDecorations = textDecorations;
        TextManager = textManager;
    }

    public EventsManager Events { get; }

    public DocumentChangeManager DocumentChanges { get; }

    public LineDecorationStore LineDecorations { get; }

    public TextDecorationStore TextDecorations { get; }

    public TextManager TextManager { get; }

    public static EditorTestContext Create(params string[] lines)
    {
        EventsManager eventsManager = new();
        DocumentChangeManager documentChangeManager = new();
        documentChangeManager.Init(eventsManager);
        LineDecorationStore lineDecorations = CreateLineDecorationStore();
        TextDecorationStore textDecorations = CreateTextDecorationStore();
        TextManager textManager = new();
        textManager.Init(
            eventsManager,
            lineDecorations,
            textDecorations,
            documentChangeManager);

        if (lines.Length > 0)
            textManager.InsertOrAddRange(lines, 0);

        return new EditorTestContext(
            eventsManager,
            documentChangeManager,
            lineDecorations,
            textDecorations,
            textManager);
    }

    public static LineDecorationStore CreateLineDecorationStore(Action? invalidate = null)
    {
        LineDecorationStore store = new();
        store.Init(invalidate ?? (static () => { }));
        return store;
    }

    public static TextDecorationStore CreateTextDecorationStore(
        Action? invalidateBackground = null,
        Action? invalidateText = null)
    {
        TextDecorationStore store = new();
        store.Init(
            invalidateBackground ?? (static () => { }),
            invalidateText ?? (static () => { }));
        return store;
    }
}
