namespace TextControlBoxNS;

/// <summary>
/// Describes the semantic purpose of a syntax-highlighted text range.
/// </summary>
public enum SyntaxHighlightRole
{
    /// <summary>Uses only the colors stored by the highlighting rule.</summary>
    Custom = 0,

    /// <summary>Comments and documentation text.</summary>
    Comment,

    /// <summary>Language keywords and modifiers.</summary>
    Keyword,

    /// <summary>Branching, looping, and other control-flow keywords.</summary>
    ControlFlow,

    /// <summary>Type names and type declarations.</summary>
    Type,

    /// <summary>Function, method, and callable names.</summary>
    Function,

    /// <summary>String and character literals.</summary>
    String,

    /// <summary>Numeric literals.</summary>
    Number,

    /// <summary>Named and built-in constant values.</summary>
    Constant,

    /// <summary>Markup element and tag names.</summary>
    MarkupName,

    /// <summary>Markup attribute names.</summary>
    AttributeName,

    /// <summary>Language operators.</summary>
    Operator,

    /// <summary>Brackets, separators, and other punctuation.</summary>
    Punctuation,

    /// <summary>Compiler, preprocessor, and document directives.</summary>
    Directive,

    /// <summary>Variable and parameter identifiers.</summary>
    Variable,

    /// <summary>Labels and jump targets.</summary>
    Label,

    /// <summary>Configuration and structured-data keys.</summary>
    Key,

    /// <summary>Configuration and structured-data values.</summary>
    Value,
}
