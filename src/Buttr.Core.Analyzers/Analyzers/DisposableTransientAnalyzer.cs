using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Buttr.Core.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DisposableTransientAnalyzer : DiagnosticAnalyzer {
        private static readonly DiagnosticDescriptor Rule = new(
            id: "BUTTR012",
            title: "IDisposable type registered as transient",
            messageFormat:
                "Type '{0}' implements IDisposable but is registered as a transient in the {1} container. " +
                "Transient instances are not tracked or disposed by the container — this may cause resource leaks. " +
                "Consider registering as a singleton, or managing disposal manually.",
            category: "Buttr.Lifetime",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol) return;
            if (methodSymbol.Name != "AddTransient") return;

            var containingType = methodSymbol.ContainingType?.ToDisplayString();
            if (!IsButtrBuilderType(containingType)) return;

            if (methodSymbol.TypeArguments.Length == 0) return;

            var concreteType = methodSymbol.TypeArguments.Last();
            if (!ImplementsDisposable(concreteType)) return;

            var containerName = InferContainerName(invocation, containingType);

            context.ReportDiagnostic(Diagnostic.Create(
                Rule, invocation.GetLocation(), concreteType.Name, containerName));
        }

        private static bool IsButtrBuilderType(string? fullName) {
            if (fullName is null) return false;

            return fullName.StartsWith("Buttr.Core.") && (
                fullName.Contains("ResolverCollection") ||
                fullName.Contains("ScopeBuilder") ||
                fullName.Contains("DIBuilder") ||
                fullName.Contains("HiddenCollection") ||
                fullName.Contains("ObjectResolverCollection") ||
                fullName.Contains("HiddenObjectResolverCollection"));
        }

        private static bool ImplementsDisposable(ITypeSymbol type) {
            if (IsDisposable(type)) return true;
            return type.AllInterfaces.Any(IsDisposable);
        }

        private static bool IsDisposable(ITypeSymbol type) {
            var fullName = type.ToDisplayString();
            return fullName == "System.IDisposable" || fullName == "System.IAsyncDisposable";
        }

        private static string InferContainerName(InvocationExpressionSyntax invocation, string? containingType) {
            if (containingType is not null) {
                if (containingType.Contains("Scope")) return "Scope";
                if (containingType.Contains("Hidden")) return "Application (Hidden)";
                if (containingType.Contains("DIBuilder") || containingType.Contains("ObjectResolverCollection"))
                    return "DI";
            }

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess) {
                var receiverText = memberAccess.Expression.ToString();

                if (receiverText.Contains("Resolvers")) return "Application";
                if (receiverText.Contains("Hidden")) return "Application (Hidden)";
            }

            return "container";
        }
    }
}
