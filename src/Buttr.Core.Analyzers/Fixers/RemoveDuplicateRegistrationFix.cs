using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Buttr.Core.Analyzers.Fixes {
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveDuplicateRegistrationFix)), Shared]
    public sealed class RemoveDuplicateRegistrationFix : CodeFixProvider {
        private const string Title = "Remove duplicate registration";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("BUTTR006");

        public override FixAllProvider GetFixAllProvider() => DocumentFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var span = diagnostic.Location.SourceSpan;

            var token = root?.FindToken(span.Start);
            if (token is null) return;

            var statement = token.Value.Parent?
                .AncestorsAndSelf()
                .OfType<ExpressionStatementSyntax>()
                .FirstOrDefault();

            if (statement is null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: ct => RemoveStatement(context.Document, statement, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> RemoveStatement(
            Document document, StatementSyntax statement, CancellationToken ct
        ) {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var newRoot = root!.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot!);
        }
    }
}
