using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Buttr.Core.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DuplicateRegistrationAnalyzer : DiagnosticAnalyzer {
        private static readonly DiagnosticDescriptor Rule = new(
            id: "BUTTR006",
            title: "Duplicate registration in container",
            messageFormat:
                "Type '{0}' is registered multiple times in the {1} container. " +
                "The second registration will overwrite the first, which is likely unintentional.",
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
            CheckForDuplicates(context, model.ApplicationRegistrations, "Application");
            CheckForDuplicates(context, model.DIRegistrations, "DI");

            foreach (var kvp in model.ScopeRegistrations)
                CheckForDuplicates(context, kvp.Value, $"Scope(\"{kvp.Key}\")");
        }

        private static void CheckForDuplicates(
            CompilationAnalysisContext context,
            List<Registration> registrations,
            string containerName
        ) {
            var bySymbol = registrations
                .Where(r => r.BuilderSymbol is not null)
                .GroupBy(r => r.BuilderSymbol!, SymbolEqualityComparer.Default);

            foreach (var group in bySymbol)
                ReportDuplicatesInGroup(context, group, containerName);

            var byTree = registrations
                .Where(r => r.BuilderSymbol is null && r.CallSite?.SourceTree is not null)
                .GroupBy(r => r.CallSite!.SourceTree!);

            foreach (var group in byTree)
                ReportDuplicatesInGroup(context, group, containerName);
        }

        private static void ReportDuplicatesInGroup(
            CompilationAnalysisContext context,
            IEnumerable<Registration> group,
            string containerName
        ) {
            var ordered = group
                .Where(r => r.CallSite is not null)
                .OrderBy(r => r.CallSite!.SourceTree?.FilePath ?? string.Empty)
                .ThenBy(r => r.CallSite!.SourceSpan.Start);

            var seen = new HashSet<string>();
            foreach (var reg in ordered) {
                if (reg.KeyTypeFullName is null) continue;
                if (!seen.Add(reg.KeyTypeFullName)) {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule, reg.CallSite, reg.KeyType, containerName));
                }
            }
        }
    }
}
