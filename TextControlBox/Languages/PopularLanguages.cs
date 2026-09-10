using TextControlBoxNS.Models;

namespace TextControlBoxNS.Languages;

internal sealed class Go : SyntaxHighlightLanguage
{
    public Go()
    {
        Name = "Go";
        Filter = [".go"];
        Description = "Syntax highlighting for Go source files";
        AutoPairingPair = LanguageDefinitionHelpers.CStylePairs(includeBackticks: true);
        StatefulHighlightRules =
        [
            new CStyleCommentRule(
                LanguageDefinitionHelpers.CommentLight,
                LanguageDefinitionHelpers.CommentDark,
                supportsRawBacktickStrings: true),
            new DelimitedHighlightRule(
                "`",
                "`",
                LanguageDefinitionHelpers.StringLight,
                LanguageDefinitionHelpers.StringDark,
                role: SyntaxHighlightRole.String),
        ];
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(
                @"\b(?:0[bB][01](?:_?[01])*|0[oO][0-7](?:_?[0-7])*|0[xX][0-9A-Fa-f](?:_?[0-9A-Fa-f])*|(?:\d(?:_?\d)*)?\.\d(?:_?\d)*(?:[eE][+-]?\d(?:_?\d)*)?|\d(?:_?\d)*(?:[eE][+-]?\d(?:_?\d)*)?)(?:i)?\b",
                SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:break|case|continue|default|defer|else|fallthrough|for|go|goto|if|range|return|select|switch)\b",
                SyntaxHighlightRole.ControlFlow),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:chan|const|func|import|interface|map|package|struct|type|var)\b",
                SyntaxHighlightRole.Keyword),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:any|bool|byte|comparable|complex64|complex128|error|float32|float64|int|int8|int16|int32|int64|rune|string|uint|uint8|uint16|uint32|uint64|uintptr)\b",
                SyntaxHighlightRole.Type),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:false|iota|nil|true)\b",
                SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(
                @"\b[A-Za-z_]\w*(?=\s*\()",
                SyntaxHighlightRole.Function),
            LanguageDefinitionHelpers.Rule(
                "\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'",
                SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(
                @"<<|>>|&\^|:=|\.\.\.|==|!=|<=|>=|&&|\|\||\+\+|--|[+\-*/%&|^<>=!:]",
                SyntaxHighlightRole.Operator),
        ];
    }
}

internal sealed class VisualBasic : SyntaxHighlightLanguage
{
    public VisualBasic()
    {
        Name = "Visual Basic .NET";
        Filter = [".vb"];
        Description = "Syntax highlighting for Visual Basic .NET source files";
        AutoPairingPair = LanguageDefinitionHelpers.BasicPairs();
        StatefulHighlightRules = [new BasicCommentRule()];
        Highlights = BasicLanguageDefinition.CreateHighlights(includeVbaKeywords: false);
    }
}

internal sealed class Vba : SyntaxHighlightLanguage
{
    public Vba()
    {
        Name = "Visual Basic for Applications";
        Filter = [".bas", ".cls", ".frm", ".vba"];
        Description = "Syntax highlighting for exported VBA modules";
        AutoPairingPair = LanguageDefinitionHelpers.BasicPairs();
        StatefulHighlightRules = [new BasicCommentRule()];
        Highlights = BasicLanguageDefinition.CreateHighlights(includeVbaKeywords: true);
    }
}

internal sealed class Bash : SyntaxHighlightLanguage
{
    public Bash()
    {
        Name = "Shell/Bash";
        Filter = [".sh", ".bash", ".zsh"];
        Description = "Syntax highlighting for Bash and compatible shell scripts";
        AutoPairingPair = LanguageDefinitionHelpers.CStylePairs(includeBackticks: true);
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(@"(?m)(?<![$\\])#.*$", SyntaxHighlightRole.Comment),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:case|coproc|do|done|elif|else|esac|fi|for|function|if|in|select|then|time|until|while)\b",
                SyntaxHighlightRole.ControlFlow),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:alias|bg|bind|break|builtin|caller|cd|command|compgen|complete|continue|declare|dirs|disown|echo|enable|eval|exec|exit|export|false|fc|fg|getopts|hash|help|history|jobs|kill|let|local|logout|mapfile|popd|printf|pushd|pwd|read|readonly|return|set|shift|shopt|source|suspend|test|times|trap|true|type|typeset|ulimit|umask|unalias|unset|wait)\b",
                SyntaxHighlightRole.Keyword),
            LanguageDefinitionHelpers.Rule(
                @"\$\{[^}\r\n]+\}|\$[A-Za-z_][A-Za-z0-9_]*|\$[0-9@*#?$!_-]",
                SyntaxHighlightRole.Variable),
            LanguageDefinitionHelpers.Rule(
                "\"(?:\\\\.|[^\"\\\\])*\"|'[^']*'|`(?:\\\\.|[^`\\\\])*`",
                SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(@"\b\d+\b", SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(@"&&|\|\||<<|>>|;;|;&|;;&|[|&;<>]", SyntaxHighlightRole.Operator),
        ];
    }
}

internal sealed class PowerShell : SyntaxHighlightLanguage
{
    public PowerShell()
    {
        Name = "PowerShell";
        Filter = [".ps1", ".psm1", ".psd1"];
        Description = "Syntax highlighting for PowerShell scripts and data files";
        AutoPairingPair = LanguageDefinitionHelpers.CStylePairs();
        StatefulHighlightRules =
        [
            new DelimitedHighlightRule(
                "<#",
                "#>",
                LanguageDefinitionHelpers.CommentLight,
                LanguageDefinitionHelpers.CommentDark,
                role: SyntaxHighlightRole.Comment),
        ];
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(@"(?m)#(?!>).*?$", SyntaxHighlightRole.Comment),
            LanguageDefinitionHelpers.Rule(
                @"(?i)\b(?:begin|break|catch|class|continue|data|define|do|dynamicparam|else|elseif|end|enum|exit|filter|finally|for|foreach|from|function|if|in|param|process|return|switch|throw|trap|try|until|using|while)\b",
                SyntaxHighlightRole.ControlFlow),
            LanguageDefinitionHelpers.Rule(
                @"(?i)\b(?:workflow|parallel|sequence|inlinescript|configuration|hidden|static)\b",
                SyntaxHighlightRole.Keyword),
            LanguageDefinitionHelpers.Rule(@"\$\{[^}\r\n]+\}|\$[A-Za-z_][\w:]*|\$[?^_$]", SyntaxHighlightRole.Variable),
            LanguageDefinitionHelpers.Rule(@"(?<!\w)-[A-Za-z][\w-]*", SyntaxHighlightRole.Directive),
            LanguageDefinitionHelpers.Rule(@"\b[A-Za-z]+-[A-Za-z][\w-]*\b", SyntaxHighlightRole.Function),
            LanguageDefinitionHelpers.Rule(
                "\"(?:`.|[^\"])*\"|'(?:''|[^'])*'",
                SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(@"(?i)\$(?:false|null|true)\b", SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(@"\b(?:0[xX][0-9A-Fa-f]+|\d+(?:\.\d+)?)\b", SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(@"(?i)-(?:and|as|band|bor|bxor|contains|eq|ge|gt|in|is|isnot|le|like|lt|match|ne|not|notcontains|notin|notlike|notmatch|or|replace|shl|shr|split|xor)\b", SyntaxHighlightRole.Operator),
        ];
    }
}

internal sealed class Rust : SyntaxHighlightLanguage
{
    public Rust()
    {
        Name = "Rust";
        Filter = [".rs"];
        Description = "Syntax highlighting for Rust source files";
        AutoPairingPair = LanguageDefinitionHelpers.CStylePairs();
        StatefulHighlightRules =
        [
            new CStyleCommentRule(
                LanguageDefinitionHelpers.CommentLight,
                LanguageDefinitionHelpers.CommentDark),
        ];
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(
                @"\b(?:as|async|await|break|const|continue|crate|dyn|else|enum|extern|false|fn|for|if|impl|in|let|loop|match|mod|move|mut|pub|ref|return|self|Self|static|struct|super|trait|true|type|union|unsafe|use|where|while|yield)\b",
                SyntaxHighlightRole.Keyword),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:bool|char|f32|f64|i8|i16|i32|i64|i128|isize|str|u8|u16|u32|u64|u128|usize)\b",
                SyntaxHighlightRole.Type),
            LanguageDefinitionHelpers.Rule(@"\b(?:false|None|Some|true)\b", SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(@"\b[A-Za-z_]\w*(?=\s*!?\s*\()", SyntaxHighlightRole.Function),
            LanguageDefinitionHelpers.Rule(
                "b?r(?<hash>#{0,255})\"[\\s\\S]*?\"\\k<hash>|b?\"(?:\\\\.|[^\"\\\\])*\"|b?'(?:\\\\.|[^'\\\\])+'",
                SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(
                @"\b(?:0[bB][01_]+|0[oO][0-7_]+|0[xX][0-9A-Fa-f_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?[\d_]+)?)(?:[iu](?:8|16|32|64|128|size)|f(?:32|64))?\b",
                SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(@"#\!?\s*\[", SyntaxHighlightRole.Directive),
            LanguageDefinitionHelpers.Rule(@"=>|->|::|\.\.=|\.\.|==|!=|<=|>=|&&|\|\||<<|>>|[+\-*/%&|^<>=!?:]", SyntaxHighlightRole.Operator),
        ];
    }
}

internal sealed class Yaml : SyntaxHighlightLanguage
{
    public Yaml()
    {
        Name = "YAML";
        Filter = [".yaml", ".yml"];
        Description = "Syntax highlighting for YAML documents";
        AutoPairingPair = LanguageDefinitionHelpers.BasicPairs();
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(@"(?m)(?<![\w])#.*$", SyntaxHighlightRole.Comment),
            LanguageDefinitionHelpers.Rule("(?m)^\\s*(?:-\\s+)?(?:[^\\s#][^:#\\r\\n]*|\"(?:\\\\.|[^\"])*\"|'(?:''|[^'])*')(?=\\s*:)", SyntaxHighlightRole.Key),
            LanguageDefinitionHelpers.Rule(@"(?:^|\s)[&*][A-Za-z0-9_.-]+", SyntaxHighlightRole.Variable),
            LanguageDefinitionHelpers.Rule(@"(?:^|\s)![A-Za-z0-9_./:-]+", SyntaxHighlightRole.Directive),
            LanguageDefinitionHelpers.Rule("\"(?:\\\\.|[^\"\\\\])*\"|'(?:''|[^'])*'", SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(@"(?i)\b(?:false|null|true|yes|no|on|off|~)\b", SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(@"(?<![\w.-])[-+]?(?:0[xX][0-9A-Fa-f_]+|0[oO][0-7_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][-+]?\d+)?)(?![\w.-])", SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(@"(?m)^\s*(?:---|\.\.\.)\s*$|[|>]([-+]?\d*)?\s*$", SyntaxHighlightRole.Directive),
            LanguageDefinitionHelpers.Rule(@"[\[\]{},?:-]", SyntaxHighlightRole.Punctuation),
        ];
    }
}

internal sealed class Dockerfile : SyntaxHighlightLanguage
{
    public Dockerfile()
    {
        Name = "Dockerfile";
        Filter = ["Dockerfile", ".dockerfile"];
        Description = "Syntax highlighting for Dockerfiles";
        AutoPairingPair = LanguageDefinitionHelpers.CStylePairs();
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(@"(?m)^\s*#.*$", SyntaxHighlightRole.Comment),
            LanguageDefinitionHelpers.Rule(
                @"(?im)^\s*(?:ADD|ARG|CMD|COPY|ENTRYPOINT|ENV|EXPOSE|FROM|HEALTHCHECK|LABEL|MAINTAINER|ONBUILD|RUN|SHELL|STOPSIGNAL|USER|VOLUME|WORKDIR)\b",
                SyntaxHighlightRole.Directive),
            LanguageDefinitionHelpers.Rule(@"\$\{[^}\r\n]+\}|\$[A-Za-z_][A-Za-z0-9_]*", SyntaxHighlightRole.Variable),
            LanguageDefinitionHelpers.Rule(@"(?<!\w)--[a-z][a-z-]*(?:=[^\s]+)?", SyntaxHighlightRole.AttributeName),
            LanguageDefinitionHelpers.Rule("\"(?:\\\\.|[^\"\\\\])*\"|'[^']*'", SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(@"\b\d+(?::\d+)?(?:/(?:tcp|udp))?\b", SyntaxHighlightRole.Number),
        ];
    }
}

internal sealed class Hcl : SyntaxHighlightLanguage
{
    public Hcl()
    {
        Name = "HCL";
        Filter = [".hcl", ".tf", ".tfvars"];
        Description = "Syntax highlighting for HCL and Terraform files";
        AutoPairingPair = LanguageDefinitionHelpers.CStylePairs();
        StatefulHighlightRules =
        [
            new CStyleCommentRule(
                LanguageDefinitionHelpers.CommentLight,
                LanguageDefinitionHelpers.CommentDark,
                supportsHashLineComments: true),
        ];
        Highlights =
        [
            LanguageDefinitionHelpers.Rule(
                @"\b(?:data|dynamic|for|if|in|locals|module|output|provider|resource|terraform|variable)\b",
                SyntaxHighlightRole.Keyword),
            LanguageDefinitionHelpers.Rule(@"(?m)^\s*[A-Za-z_][\w-]*(?=\s*=)", SyntaxHighlightRole.Key),
            LanguageDefinitionHelpers.Rule(@"\b[A-Za-z_][\w-]*(?=\s*\()", SyntaxHighlightRole.Function),
            LanguageDefinitionHelpers.Rule(@"\$\{[^}\r\n]+\}", SyntaxHighlightRole.Variable),
            LanguageDefinitionHelpers.Rule("\"(?:\\\\.|[^\"\\\\])*\"", SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(@"\b(?:false|null|true)\b", SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(@"(?<![\w.])[-+]?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?(?![\w.])", SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(@"=>|\.\.\.|==|!=|<=|>=|&&|\|\||[+\-*/%<>=!?:]", SyntaxHighlightRole.Operator),
        ];
    }
}

internal static class BasicLanguageDefinition
{
    public static SyntaxHighlights[] CreateHighlights(bool includeVbaKeywords)
    {
        string declarationKeywords = includeVbaKeywords
            ? "Alias|As|ByRef|ByVal|Call|Const|Declare|Dim|Enum|Event|Function|Get|Global|Implements|Let|Lib|New|Optional|ParamArray|Private|Property|Public|ReDim|Set|Static|Sub|Type"
            : "As|Async|ByRef|ByVal|Class|Const|Custom|Delegate|Dim|Enum|Event|Function|Get|Implements|Imports|Inherits|Interface|Iterator|Module|Namespace|New|Of|Operator|Optional|ParamArray|Private|Property|Protected|Public|ReadOnly|Set|Shared|Static|Structure|Sub|WriteOnly";
        string controlFlowKeywords = includeVbaKeywords
            ? "Case|Do|Each|Else|ElseIf|End|Error|Exit|For|GoSub|GoTo|If|Loop|Next|On|Resume|Return|Select|Step|Then|To|Wend|While|With"
            : "Await|Case|Catch|Continue|Do|Each|Else|ElseIf|End|Exit|Finally|For|If|Loop|Next|Return|Select|Step|Then|Throw|To|Try|Until|When|While|With|Yield";
        string types = includeVbaKeywords
            ? "Boolean|Byte|Collection|Currency|Date|Decimal|Double|Integer|Long|LongLong|LongPtr|Object|Single|String|Variant"
            : "Boolean|Byte|Char|Date|Decimal|Double|Integer|Long|Object|SByte|Short|Single|String|UInteger|ULong|UShort";

        return
        [
            LanguageDefinitionHelpers.Rule($@"(?i)\b(?:{declarationKeywords})\b", SyntaxHighlightRole.Keyword),
            LanguageDefinitionHelpers.Rule($@"(?i)\b(?:{controlFlowKeywords})\b", SyntaxHighlightRole.ControlFlow),
            LanguageDefinitionHelpers.Rule($@"(?i)\b(?:{types})\b", SyntaxHighlightRole.Type),
            LanguageDefinitionHelpers.Rule(@"(?i)\b(?:False|Me|MyBase|MyClass|Nothing|True)\b", SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(@"\b[A-Za-z_]\w*(?=\s*\()", SyntaxHighlightRole.Function),
            LanguageDefinitionHelpers.Rule("\"(?:\"\"|[^\"])*\"", SyntaxHighlightRole.String),
            LanguageDefinitionHelpers.Rule(@"#[^#\r\n]+#", SyntaxHighlightRole.Constant),
            LanguageDefinitionHelpers.Rule(@"(?im)^\s*#(?:Const|Else|ElseIf|End\s+If|End\s+Region|ExternalSource|If|Region)\b.*$", SyntaxHighlightRole.Directive),
            LanguageDefinitionHelpers.Rule(@"(?i)(?<![\w.])(?:&H[0-9A-F]+|&O[0-7]+|\d+(?:\.\d+)?(?:E[-+]?\d+)?)(?:D|F|I|L|R|S|UI|UL|US|@|!|#|%|&|\^)?\b", SyntaxHighlightRole.Number),
            LanguageDefinitionHelpers.Rule(@"<>|<=|>=|<<|>>|\+=|-=|\*=|/=|\\=|\^=|&=|[+\-*/\\^&=<>]", SyntaxHighlightRole.Operator),
        ];
    }
}

internal static class LanguageDefinitionHelpers
{
    public const string CommentLight = "#6B6A6A";
    public const string CommentDark = "#646464";
    public const string StringLight = "#A31515";
    public const string StringDark = "#CE9178";

    public static AutoPairingPair[] CStylePairs(bool includeBackticks = false)
    {
        return includeBackticks
            ?
            [
                new AutoPairingPair("{", "}"),
                new AutoPairingPair("[", "]"),
                new AutoPairingPair("(", ")"),
                new AutoPairingPair("\""),
                new AutoPairingPair("'"),
                new AutoPairingPair("`"),
            ]
            :
            [
                new AutoPairingPair("{", "}"),
                new AutoPairingPair("[", "]"),
                new AutoPairingPair("(", ")"),
                new AutoPairingPair("\""),
                new AutoPairingPair("'"),
            ];
    }

    public static AutoPairingPair[] BasicPairs()
    {
        return
        [
            new AutoPairingPair("(", ")"),
            new AutoPairingPair("[", "]"),
            new AutoPairingPair("{", "}"),
            new AutoPairingPair("\""),
        ];
    }

    public static SyntaxHighlights Rule(string pattern, SyntaxHighlightRole role)
    {
        (string light, string dark) = role switch
        {
            SyntaxHighlightRole.Comment => (CommentLight, CommentDark),
            SyntaxHighlightRole.String => (StringLight, StringDark),
            SyntaxHighlightRole.Number => ("#098658", "#B5CEA8"),
            SyntaxHighlightRole.Type => ("#267F99", "#4EC9B0"),
            SyntaxHighlightRole.Function => ("#795E26", "#DCDCAA"),
            SyntaxHighlightRole.Constant => ("#0000FF", "#569CD6"),
            SyntaxHighlightRole.Operator => ("#7A3E9D", "#D4D4D4"),
            SyntaxHighlightRole.Punctuation => ("#555555", "#D4D4D4"),
            SyntaxHighlightRole.Variable => ("#001080", "#9CDCFE"),
            SyntaxHighlightRole.Key => ("#0451A5", "#9CDCFE"),
            SyntaxHighlightRole.AttributeName => ("#795E26", "#D7BA7D"),
            SyntaxHighlightRole.Directive => ("#AF00DB", "#C586C0"),
            SyntaxHighlightRole.ControlFlow => ("#AF00DB", "#C586C0"),
            _ => ("#0000FF", "#569CD6"),
        };

        return new SyntaxHighlights(pattern, light, dark, role: role);
    }
}
