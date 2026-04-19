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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DuplicateAliasFix)), Shared]
    public sealed class DuplicateAliasFix : CodeFixProvider {
        private const string Title = "Remove duplicate alias";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("BUTTR014");

        public override FixAllProvider GetFixAllProvider() => DocumentFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var span = diagnostic.Location.SourceSpan;

            var invocation = root?.FindToken(span.Start).Parent?
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocation is null) return;
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: ct =>
                        RemoveAliasCall(context.Document, invocation, memberAccess, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> RemoveAliasCall(
            Document document,
            InvocationExpressionSyntax invocation,
            MemberAccessExpressionSyntax memberAccess,
            CancellationToken ct
        ) {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null) return document;

            var replacement = memberAccess.Expression.WithTriviaFrom(invocation);
            var newRoot = root.ReplaceNode(invocation, replacement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
