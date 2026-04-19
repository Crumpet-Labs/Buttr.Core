using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Buttr.Core.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ConstructorResolutionAnalyzer : DiagnosticAnalyzer {
        private static readonly DiagnosticDescriptor Rule = new(
            id: "BUTTR004",
            title: "Constructor parameter may not be resolvable",
            messageFormat:
                "Constructor of '{0}' requires '{1}' but it does not appear to be registered in the {2} container. " +
                "If this type is registered by an external plugin, this warning may be safely suppressed.",
            category: "Buttr.Injection",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext => {
                var collector = new RegistrationCollector();
                collector.RegisterCallbacks(compilationContext);

                compilationContext.RegisterCompilationEndAction(
                    endContext => RunChecks(endContext, collector.FinalizeModel()));
            });
        }

        private static void RunChecks(CompilationAnalysisContext context, RegistrationModel model) {
            var checkedTypes = new HashSet<string>();

            var allRegistrations = model.ApplicationRegistrations
                .Concat(model.DIRegistrations)
                .Concat(model.ScopeRegistrations.SelectMany(kvp => kvp.Value));

            foreach (var registration in allRegistrations) {
                if (registration.ConcreteTypeFullName is null) continue;
                if (!checkedTypes.Add(registration.ConcreteTypeFullName)) continue;

                var concreteSymbol = context.Compilation.GetTypeByMetadataName(registration.ConcreteTypeFullName);
                if (concreteSymbol is null) continue;

                var constructor = concreteSymbol.Constructors
                    .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
                    .OrderByDescending(c => c.Parameters.Length)
                    .FirstOrDefault();

                if (constructor is null || constructor.Parameters.Length == 0) continue;

                var containers = model.ContainersFor(registration.ConcreteTypeFullName);

                foreach (var (containerKind, scopeKey) in containers) {
                    foreach (var param in constructor.Parameters) {
                        var paramType = param.Type.ToDisplayString();

                        if (model.IsResolvableFrom(paramType, containerKind, scopeKey)) continue;
                        if (IsFrameworkType(param.Type)) continue;

                        var containerName = containerKind switch {
                            ContainerKind.Scope => $"Scope(\"{scopeKey}\")",
                            ContainerKind.DI => "DI",
                            _ => "Application"
                        };

                        var location = constructor.DeclaringSyntaxReferences
                            .FirstOrDefault()?.GetSyntax().GetLocation()
                            ?? registration.CallSite;

                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule, location, registration.ConcreteType, param.Type.Name, containerName));
                    }
                }
            }
        }

        private static bool IsFrameworkType(ITypeSymbol type) {
            switch (type.SpecialType) {
                case SpecialType.System_String:
                case SpecialType.System_Boolean:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
            }

            if (type.IsValueType) return true;

            var ns = type.ContainingNamespace?.ToDisplayString();
            return ns != null && ns.StartsWith("System");
        }
    }
}
