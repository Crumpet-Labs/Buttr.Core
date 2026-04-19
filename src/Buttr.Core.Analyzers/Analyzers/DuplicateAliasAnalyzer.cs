using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Buttr.Core.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DuplicateAliasAnalyzer : DiagnosticAnalyzer {
        private static readonly DiagnosticDescriptor Rule = new(
            id: "BUTTR014",
            title: "Duplicate alias key across registrations",
            messageFormat:
                "Alias '{0}' is already claimed by another registration in the {1} container. " +
                "An alias key must be unique; use All<{0}>() for bulk resolution across multiple implementations.",
            category: "Buttr.Injection",
            defaultSeverity: DiagnosticSeverity.Error,
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
            CheckContainer(context, model.ApplicationRegistrations, "Application");
            CheckContainer(context, model.DIRegistrations, "DI");

            foreach (var kvp in model.ScopeRegistrations)
                CheckContainer(context, kvp.Value, $"Scope(\"{kvp.Key}\")");
        }

        private static void CheckContainer(
            CompilationAnalysisContext context,
            List<Registration> registrations,
            string containerName
        ) {
            var ownersByKey = new Dictionary<string, Registration>();

            foreach (var registration in registrations) {
                if (registration.KeyTypeFullName is not null &&
                    !ownersByKey.ContainsKey(registration.KeyTypeFullName)) {
                    ownersByKey[registration.KeyTypeFullName] = registration;
                }

                foreach (var alias in registration.Aliases) {
                    if (alias.CallSite is null) continue;

                    if (ownersByKey.ContainsKey(alias.AliasTypeFullName)) {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule, alias.CallSite, ShortNameOf(alias.AliasTypeFullName), containerName));
                        continue;
                    }

                    ownersByKey[alias.AliasTypeFullName] = registration;
                }
            }
        }

        private static string ShortNameOf(string fullName) {
            var lastDot = fullName.LastIndexOf('.');
            return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
        }
    }
}
