using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Buttr.Core.Analyzers.Fixes {
    public sealed class DocumentFixAllProvider : FixAllProvider {
        public static readonly DocumentFixAllProvider Instance = new();

        private DocumentFixAllProvider() { }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => ImmutableArray.Create(FixAllScope.Document);

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext) {
            if (fixAllContext.Scope != FixAllScope.Document) return null;

            var document = fixAllContext.Document;
            if (document is null) return null;

            var diagnostics = await fixAllContext
                .GetDocumentDiagnosticsAsync(document)
                .ConfigureAwait(false);

            if (diagnostics.IsEmpty) return null;

            var codeFixProvider = fixAllContext.CodeFixProvider;

            return CodeAction.Create(
                title: fixAllContext.CodeActionEquivalenceKey ?? "Fix all in document",
                createChangedDocument: async ct => {
                    var currentDoc = document;

                    foreach (var diagnostic in diagnostics) {
                        var root = await currentDoc.GetSyntaxRootAsync(ct).ConfigureAwait(false);
                        if (root is null) continue;

                        var actions = new List<CodeAction>();
                        var context = new CodeFixContext(
                            currentDoc,
                            diagnostic,
                            (action, _) => actions.Add(action),
                            ct);

                        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                        var matchingAction = actions.FirstOrDefault(a =>
                            a.EquivalenceKey == fixAllContext.CodeActionEquivalenceKey);
                        if (matchingAction is null) continue;

                        var operations = await matchingAction.GetOperationsAsync(ct).ConfigureAwait(false);
                        var changedDocOp = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
                        if (changedDocOp is null) continue;

                        currentDoc = changedDocOp.ChangedSolution.GetDocument(currentDoc.Id) ?? currentDoc;
                    }

                    return currentDoc;
                },
                equivalenceKey: fixAllContext.CodeActionEquivalenceKey);
        }
    }
}
