using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Buttr.Core.Analyzers.Fixes {
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposableTransientFix)), Shared]
    public sealed class DisposableTransientFix : CodeFixProvider {
        private const string Title = "Change to AddSingleton";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("BUTTR012");

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

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: ct => ChangeToSingleton(context.Document, invocation, ct),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> ChangeToSingleton(
            Document document, InvocationExpressionSyntax invocation, CancellationToken ct
        ) {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return document;

            SimpleNameSyntax newName;

            if (memberAccess.Name is GenericNameSyntax genericName) {
                newName = SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("AddSingleton"),
                        genericName.TypeArgumentList)
                    .WithTriviaFrom(memberAccess.Name);
            }
            else {
                newName = SyntaxFactory.IdentifierName("AddSingleton")
                    .WithTriviaFrom(memberAccess.Name);
            }

            var newMemberAccess = memberAccess.WithName(newName);
            var newInvocation = invocation.WithExpression(newMemberAccess);
            var newRoot = root!.ReplaceNode(invocation, newInvocation);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
