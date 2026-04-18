using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WT005NoCommentsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.WT005,
        "No comments allowed",
        "Remove comment",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext Context)
    {
        Context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        Context.EnableConcurrentExecution();

        Context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext Context)
    {
        var FilePath = Context.Tree.FilePath ?? string.Empty;

        if (FilePath.Contains("obj") || FilePath.Contains("Generated"))
        {
            return;
        }

        var Root = Context.Tree.GetRoot(Context.CancellationToken);

        foreach (var Trivia in Root.DescendantTrivia())
        {
            if (!Trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                !Trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) &&
                !Trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) &&
                !Trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                continue;
            }

            var TriviaText = Trivia.ToFullString();

            if (TriviaText.Contains("auto-generated"))
            {
                continue;
            }

            var Diagnostic = Microsoft.CodeAnalysis.Diagnostic.Create(
                Rule,
                Trivia.GetLocation());
            Context.ReportDiagnostic(Diagnostic);
        }
    }
}
