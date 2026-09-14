using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TextControlBoxNS;
using TextControlBoxNS.Core;

namespace TextControlBox.Tests;

[TestClass]
public sealed class PopularLanguageSyntaxTests
{
    [TestMethod]
    [DataRow(SyntaxHighlightID.Go, ".go")]
    [DataRow(SyntaxHighlightID.VisualBasic, ".vb")]
    [DataRow(SyntaxHighlightID.VBA, ".bas")]
    [DataRow(SyntaxHighlightID.Bash, ".sh")]
    [DataRow(SyntaxHighlightID.PowerShell, ".ps1")]
    [DataRow(SyntaxHighlightID.Rust, ".rs")]
    [DataRow(SyntaxHighlightID.YAML, ".yaml")]
    [DataRow(SyntaxHighlightID.Dockerfile, "Dockerfile")]
    [DataRow(SyntaxHighlightID.HCL, ".tf")]
    public void BuiltInLanguage_IsRegisteredWithExpectedFilter(
        SyntaxHighlightID languageId,
        string expectedFilter)
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(languageId);

        Assert.IsNotNull(language);
        Assert.Contains(expectedFilter, language.Filter);
        Assert.IsNotNull(language.Highlights);
        Assert.IsNotEmpty(language.Highlights);
    }

    [TestMethod]
    [DataRow(SyntaxHighlightID.Go, "package main", SyntaxHighlightRole.Keyword, "package")]
    [DataRow(SyntaxHighlightID.VisualBasic, "Public Class Example", SyntaxHighlightRole.Keyword, "Class")]
    [DataRow(SyntaxHighlightID.VBA, "Private Sub Workbook_Open()", SyntaxHighlightRole.Keyword, "Sub")]
    [DataRow(SyntaxHighlightID.Bash, "if test -f file; then", SyntaxHighlightRole.ControlFlow, "if")]
    [DataRow(SyntaxHighlightID.PowerShell, "function Get-Value { param() }", SyntaxHighlightRole.ControlFlow, "function")]
    [DataRow(SyntaxHighlightID.Rust, "pub fn main()", SyntaxHighlightRole.Keyword, "fn")]
    [DataRow(SyntaxHighlightID.YAML, "enabled: true", SyntaxHighlightRole.Key, "enabled")]
    [DataRow(SyntaxHighlightID.Dockerfile, "FROM mcr.microsoft.com/dotnet", SyntaxHighlightRole.Directive, "FROM")]
    [DataRow(SyntaxHighlightID.HCL, "resource \"example\" \"main\" {", SyntaxHighlightRole.Keyword, "resource")]
    public void RepresentativeToken_IsMatchedWithSemanticRole(
        SyntaxHighlightID languageId,
        string source,
        SyntaxHighlightRole role,
        string expectedToken)
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(languageId);
        language.CompileAllRegex();

        bool matched = language.Highlights
            .Where(rule => rule.Role == role)
            .SelectMany(rule => rule.PrecompiledRegex.Matches(source).Cast<System.Text.RegularExpressions.Match>())
            .Any(match => match.Value.Contains(expectedToken, StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(matched);
    }

    [TestMethod]
    public void VisualBasicCommentRule_IgnoresApostropheInsideString()
    {
        BasicCommentRule rule = new();
        const string line = "Dim message = \"it's text\" ' actual comment";
        List<HighlightSpan> highlights = [];

        rule.GetHighlights(0, line.AsSpan(), rule.InitialState, highlights);

        Assert.HasCount(1, highlights);
        Assert.AreEqual(line.LastIndexOf('\''), highlights[0].Start);
        Assert.AreEqual(SyntaxHighlightRole.Comment, highlights[0].Role);
    }

    [TestMethod]
    public void GoRawString_ContinuesAcrossLines()
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(SyntaxHighlightID.Go);
        IStatefulHighlightRule rawStringRule = language.StatefulHighlightRules.Single(
            rule => rule is DelimitedHighlightRule);
        List<HighlightSpan> highlights = [];

        int state = rawStringRule.GetStateAfterLine(0, "value := `first".AsSpan(), rawStringRule.InitialState);
        rawStringRule.GetHighlights(1, "second`".AsSpan(), state, highlights);

        Assert.AreNotEqual(rawStringRule.InitialState, state);
        Assert.HasCount(1, highlights);
        Assert.AreEqual(SyntaxHighlightRole.String, highlights[0].Role);
        Assert.AreEqual(rawStringRule.InitialState,
            rawStringRule.GetStateAfterLine(1, "second`".AsSpan(), state));
    }

    [TestMethod]
    public void GoCommentRule_TreatsBackslashAsLiteralInsideRawString()
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(SyntaxHighlightID.Go);
        IStatefulHighlightRule commentRule = language.StatefulHighlightRules.Single(
            rule => rule is CStyleCommentRule);
        const string line = "value := `text\\` // comment";
        List<HighlightSpan> highlights = [];

        commentRule.GetHighlights(0, line.AsSpan(), commentRule.InitialState, highlights);

        Assert.HasCount(1, highlights);
        Assert.AreEqual(line.IndexOf("//", StringComparison.Ordinal), highlights[0].Start);
        Assert.AreEqual(SyntaxHighlightRole.Comment, highlights[0].Role);
    }

    [TestMethod]
    public void BashComment_OverridesEveryTokenInsideComment()
    {
        const string line = "# if test \"$value\" && 42";

        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.Bash, line, line.IndexOf("test", StringComparison.Ordinal)));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.Bash, line, line.IndexOf("$value", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void BashHashInsideStringOrWord_DoesNotStartComment()
    {
        const string quotedLine = "echo \"# if test\" # while";
        const string wordLine = "echo value#suffix";

        Assert.AreEqual(
            SyntaxHighlightRole.String,
            GetFinalRoleAt(SyntaxHighlightID.Bash, quotedLine, quotedLine.IndexOf('#')));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.Bash, quotedLine, quotedLine.LastIndexOf('#')));
        Assert.AreNotEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.Bash, wordLine, wordLine.IndexOf('#')));
    }

    [TestMethod]
    public void PowerShellLineComment_RespectsStringsAndOverridesTokens()
    {
        const string line = "Write-Host \"# $false\" # foreach $true";

        Assert.AreEqual(
            SyntaxHighlightRole.String,
            GetFinalRoleAt(SyntaxHighlightID.PowerShell, line, line.IndexOf('#')));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.PowerShell, line, line.LastIndexOf('#')));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.PowerShell, line, line.IndexOf("foreach", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PowerShellHereString_DoesNotTreatHashAsComment()
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(
            SyntaxHighlightID.PowerShell);
        PowerShellLineCommentRule rule = language.StatefulHighlightRules
            .OfType<PowerShellLineCommentRule>()
            .Single();
        List<HighlightSpan> highlights = [];

        int state = rule.GetStateAfterLine(0, "$text = @\"".AsSpan(), rule.InitialState);
        rule.GetHighlights(1, "# foreach $true".AsSpan(), state, highlights);

        Assert.AreNotEqual(rule.InitialState, state);
        Assert.IsEmpty(highlights);
        Assert.AreEqual(
            rule.InitialState,
            rule.GetStateAfterLine(2, "\"@".AsSpan(), state));
    }

    [TestMethod]
    public void YamlComment_RespectsQuotedScalarsAndOverridesTokens()
    {
        const string line = "value: \"# true\" # false: 42";

        Assert.AreEqual(
            SyntaxHighlightRole.String,
            GetFinalRoleAt(SyntaxHighlightID.YAML, line, line.IndexOf('#')));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.YAML, line, line.LastIndexOf('#')));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.YAML, line, line.IndexOf("false", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void YamlBlockScalar_DoesNotTreatHashAsComment()
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(
            SyntaxHighlightID.YAML);
        YamlCommentRule rule = language.StatefulHighlightRules
            .OfType<YamlCommentRule>()
            .Single();
        List<HighlightSpan> highlights = [];

        int state = rule.GetStateAfterLine(0, "script: |".AsSpan(), rule.InitialState);
        rule.GetHighlights(1, "  echo \"# true\"".AsSpan(), state, highlights);

        Assert.AreNotEqual(rule.InitialState, state);
        Assert.IsEmpty(highlights);
        Assert.AreEqual(
            rule.InitialState,
            rule.GetStateAfterLine(2, "enabled: true".AsSpan(), state));
    }

    [TestMethod]
    public void DockerfileComment_IsAppliedAfterEmbeddedTokens()
    {
        const string comment = "# RUN echo ${HOME} \"text\" 42";
        const string command = "RUN echo \"# ${HOME}\"";

        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.Dockerfile, comment, comment.IndexOf("RUN", StringComparison.Ordinal)));
        Assert.AreEqual(
            SyntaxHighlightRole.Comment,
            GetFinalRoleAt(SyntaxHighlightID.Dockerfile, comment, comment.IndexOf("${HOME}", StringComparison.Ordinal)));
        Assert.AreEqual(
            SyntaxHighlightRole.String,
            GetFinalRoleAt(SyntaxHighlightID.Dockerfile, command, command.IndexOf('#')));
    }

    private static SyntaxHighlightRole GetFinalRoleAt(
        SyntaxHighlightID languageId,
        string line,
        int characterIndex)
    {
        SyntaxHighlightLanguage language = CoreTextControlBox.GetSyntaxHighlightingFromID(languageId);
        language.CompileAllRegex();
        SyntaxHighlightRole role = SyntaxHighlightRole.Custom;

        foreach (SyntaxHighlights highlight in language.Highlights)
        {
            foreach (System.Text.RegularExpressions.Match match in highlight.PrecompiledRegex.Matches(line))
            {
                if (characterIndex >= match.Index && characterIndex < match.Index + match.Length)
                    role = highlight.Role;
            }
        }

        foreach (IStatefulHighlightRule rule in language.StatefulHighlightRules ?? [])
        {
            List<HighlightSpan> spans = [];
            rule.GetHighlights(0, line.AsSpan(), rule.InitialState, spans);
            foreach (HighlightSpan span in spans)
            {
                if (characterIndex >= span.Start && characterIndex < span.Start + span.Length)
                    role = span.Role;
            }
        }

        return role;
    }
}
