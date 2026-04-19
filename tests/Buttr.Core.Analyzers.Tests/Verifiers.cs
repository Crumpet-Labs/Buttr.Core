using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Buttr.Core.Analyzers.Tests;

internal static class Verifiers {
    public static Task Analyze<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new() {
        var test = new CSharpAnalyzerTest<TAnalyzer, NUnitVerifier> {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        };

        AddButtrReferences(test.TestState);

        foreach (var diag in expected) test.ExpectedDiagnostics.Add(diag);

        return test.RunAsync();
    }

    public static Task AnalyzeAndFix<TAnalyzer, TCodeFix>(
        string source, string fixedSource, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new() {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, NUnitVerifier> {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CodeFixTestBehaviors =
                CodeFixTestBehaviors.SkipFixAllInDocumentCheck |
                CodeFixTestBehaviors.SkipFixAllInProjectCheck |
                CodeFixTestBehaviors.SkipFixAllInSolutionCheck
        };

        AddButtrReferences(test.TestState);
        AddButtrReferences(test.FixedState);

        foreach (var diag in expected) test.ExpectedDiagnostics.Add(diag);

        return test.RunAsync();
    }

    private static void AddButtrReferences(SolutionState state) {
        state.AdditionalReferences.Add(typeof(Buttr.Core.DIBuilder).Assembly);
        state.AdditionalReferences.Add(typeof(Buttr.Injection.InjectAttribute).Assembly);
    }
}
