# Contributing

Buttr.Core is primarily maintained by Crumpet Labs. Contributions and issues are welcome, but the library has opinions on how changes land.

## Before you open a PR

### Discuss architectural changes first

Anything that touches multiple files — a refactor, a new public API, a change to registration or resolution semantics — starts with a conversation, not a branch. Open an issue, describe the problem and at least two candidate approaches with trade-offs, and let the direction settle before writing code.

Reason: the library is used in production by 20+ businesses. The shape has been earned over several design passes and a split from the Unity repo. "Just tried this, what do you think?" PRs rarely land.

### Keep the code comment-free

Buttr.Core prefers self-documenting code over narrative comments. Names carry the intent; tests carry the contract; this `Docs/` folder carries the conceptual material. Inline comments get stripped during review unless they document a non-obvious constraint (hidden invariant, specific workaround for a framework bug, etc.).

If you feel the urge to write a paragraph explaining what a method does, split the method instead.

### Tests must stay green

The test project under `tests/Buttr.Core.Tests/` is the behavioural contract. Any change — including refactors that don't touch semantics — runs `dotnet test` and passes before the PR is ready. Coverage currently sits around 92 %; new code should land at that level or higher.

For analyzer changes, `tests/Buttr.Core.Analyzers.Tests/` must also be green.

## Style

- **Naming** — PascalCase for public/internal members, `m_` prefix for private instance fields, `s_` for private statics.
- **File layout** — one type per file (barring tightly-coupled types like a generic pair `Foo<T>` + `Foo<TA, TB>` that legitimately share concerns).
- **Braces** — Allman (opening brace on new line) where shown in existing code, K&R otherwise — match the surrounding file.
- **Nullable** — enabled in analyzers/tests, disabled in runtime projects (matches the existing Directory.Build.props). Don't flip it locally.
- **Target framework** — Buttr.Core and Buttr.Injection target `netstandard2.1`. Don't add APIs that require `net6.0+` without a multi-target decision (see BACKLOG).

## Commit messages

Short, present-tense, scope-prefixed:

```
aliasing: detect duplicate alias keys at compile time
perf: swap All<T> to struct enumerator
docs: add getting started guide
```

Review checkpoints are at natural boundaries (a stage completing, a feature landing) rather than per-commit. Prefer clean commits over squash-at-merge.

## Running benchmarks

Before proposing a perf change, capture a baseline and post-fix report. See [Benchmarks](Benchmarks/). Dated reports in `Docs/Benchmarks/` build the paper trail; don't replace old ones, add new ones.

## Adding a new analyzer rule

1. Raise an issue first with a clear "what this catches, what the fixer does" summary.
2. New rule goes in `src/Buttr.Core.Analyzers/Analyzers/` with a `BUTTR###` ID incrementing the highest existing.
3. Paired fixer in `src/Buttr.Core.Analyzers/Fixers/` where auto-fixing is unambiguous; skip otherwise.
4. Test file in `tests/Buttr.Core.Analyzers.Tests/` — at minimum a positive and negative case, plus a round-trip for the fixer if present.
5. Update `src/Buttr.Core.Analyzers/AnalyzerReleases.Unshipped.md` with the new rule row.
6. Document the rule in `Docs/Analyzers.md`.

## Engine-specific contributions

Buttr.Core is strictly engine-agnostic. Unity / Godot / Stride glue lives in sibling repos — PRs against this repo that reference engine types will be redirected. See [repo topology](../README.md#unity--godot--stride-consumers).

## Reporting bugs

- Repro steps, expected vs actual behaviour.
- The Buttr.Core version you're on.
- A minimal reproduction as a standalone `.csproj` when the bug involves resolution / registration semantics — it's much faster to iterate on a self-contained project than to triage from a description.

## License

MIT. By contributing you agree the contribution ships under the same license.
