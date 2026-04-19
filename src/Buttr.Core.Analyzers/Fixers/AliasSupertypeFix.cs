using System.Collections.Generic;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AliasSupertypeFix)), Shared]
    public sealed class AliasSupertypeFix : CodeFixProvider {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("BUTTR013");

        public override FixAllProvider GetFixAllProvider() => DocumentFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (root is null) return;

            var diagnostic = context.Diagnostics.First();
            var span = diagnostic.Location.SourceSpan;

            var genericName = root.FindNode(span).FirstAncestorOrSelf<GenericNameSyntax>();
            if (genericName is null) return;

            var invocation = genericName.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation is null) return;

            var semanticModel = await context.Document
                .GetSemanticModelAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (semanticModel is null) return;

            var method = semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (method is null) return;

            var concreteType = ExtractConcreteType(method);
            if (concreteType is null) return;

            var aliasIndex = method.TypeArguments.Length == 1 ? 0 : 1;
            if (genericName.TypeArgumentList.Arguments.Count <= aliasIndex) return;

            var supertypes = CollectSupertypes(concreteType);
            if (supertypes.Count == 0) return;

            foreach (var supertype in supertypes) {
                var displayName = supertype.ToDisplayString();
                var title = $"Use '{displayName}' as the alias";

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: ct =>
                            ReplaceAliasType(context.Document, genericName, aliasIndex, displayName, ct),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private static ITypeSymbol? ExtractConcreteType(IMethodSymbol method) {
            if (method.ContainingType is not { } containingType) return null;

            var definitionName = containingType.ConstructedFrom.ToDisplayString();
            if (definitionName == "Buttr.Core.IConfigurable<TConcrete>"
                && containingType.TypeArguments.Length == 1) {
                return containingType.TypeArguments[0];
            }

            if (definitionName == "Buttr.Core.IConfigurableCollection"
                && method.TypeArguments.Length == 2) {
                return method.TypeArguments[0];
            }

            return null;
        }

        private static List<ITypeSymbol> CollectSupertypes(ITypeSymbol concreteType) {
            var results = new List<ITypeSymbol>();

            foreach (var iface in concreteType.AllInterfaces) {
                results.Add(iface);
            }

            var baseType = concreteType.BaseType;
            while (baseType is not null && baseType.SpecialType != SpecialType.System_Object) {
                results.Add(baseType);
                baseType = baseType.BaseType;
            }

            return results;
        }

        private static async Task<Document> ReplaceAliasType(
            Document document,
            GenericNameSyntax genericName,
            int aliasIndex,
            string newTypeDisplayName,
            CancellationToken ct
        ) {
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root is null) return document;

            var newArgument = SyntaxFactory.ParseTypeName(newTypeDisplayName);
            var newArguments = genericName.TypeArgumentList.Arguments.Replace(
                genericName.TypeArgumentList.Arguments[aliasIndex],
                newArgument);
            var newGenericName = genericName.WithTypeArgumentList(
                genericName.TypeArgumentList.WithArguments(newArguments));

            var newRoot = root.ReplaceNode(genericName, newGenericName);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
