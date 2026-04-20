; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BUTTR004 | Buttr.Injection | Warning | ConstructorResolutionAnalyzer
BUTTR006 | Buttr.Injection | Error | DuplicateRegistrationAnalyzer
BUTTR012 | Buttr.Lifetime | Warning | DisposableTransientAnalyzer
BUTTR013 | Buttr.Injection | Error | AliasSupertypeAnalyzer
BUTTR014 | Buttr.Injection | Error | DuplicateAliasAnalyzer

## Release 1.3.1

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
BUTTR006 | Buttr.Injection | Warning | Buttr.Injection | Error | DuplicateRegistrationAnalyzer. Lowered because last-wins override is a legitimate pattern (test doubles, late-binding overrides). Paired with cross-builder false-positive fix.
