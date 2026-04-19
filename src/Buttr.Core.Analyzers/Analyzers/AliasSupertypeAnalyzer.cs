using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Buttr.Core.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AliasSupertypeAnalyzer : DiagnosticAnalyzer {
        private static readonly DiagnosticDescriptor Rule = new(
            id: "BUTTR013",
            title: "Alias must be a supertype of the concrete registration",
            messageFormat:
                "Cannot alias '{0}' as '{1}'. The alias type must be a supertype of (or identical to) the concrete type.",
            category: "Buttr.Injection",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context) {
            var operation = (IInvocationOperation)context.Operation;
            var method = operation.TargetMethod;

            if (method.Name != "As") return;
            if (method.ContainingType is not { } containingType) return;

            ITypeSymbol? concreteType;
            ITypeSymbol? aliasType;

            var definitionName = containingType.ConstructedFrom.ToDisplayString();
            if (definitionName == "Buttr.Core.IConfigurable<TConcrete>"
                && method.TypeArguments.Length == 1
                && containingType.TypeArguments.Length == 1) {
                concreteType = containingType.TypeArguments[0];
                aliasType = method.TypeArguments[0];
            }
            else if (definitionName == "Buttr.Core.IConfigurableCollection"
                     && method.TypeArguments.Length == 2) {
                concreteType = method.TypeArguments[0];
                aliasType = method.TypeArguments[1];
            }
            else {
                return;
            }

            if (concreteType is null || aliasType is null) return;
            if (concreteType is ITypeParameterSymbol || aliasType is ITypeParameterSymbol) return;
            if (SymbolEqualityComparer.Default.Equals(concreteType, aliasType)) return;
            if (IsAssignable(concreteType, aliasType)) return;

            var location = ExtractAliasLocation(operation) ?? operation.Syntax.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                location,
                concreteType.ToDisplayString(),
                aliasType.ToDisplayString()));
        }

        private static Location? ExtractAliasLocation(IInvocationOperation operation) {
            if (operation.Syntax is not InvocationExpressionSyntax invocation) return null;
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return null;
            if (memberAccess.Name is not GenericNameSyntax generic) return null;
            return generic.GetLocation();
        }

        private static bool IsAssignable(ITypeSymbol concreteType, ITypeSymbol aliasType) {
            if (concreteType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, aliasType)))
                return true;

            var baseType = concreteType.BaseType;
            while (baseType is not null) {
                if (SymbolEqualityComparer.Default.Equals(baseType, aliasType)) return true;
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
