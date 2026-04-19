# Session 1 Retrospective — Test coverage + KNOWN_ISSUES resolution
*2026-04-19*

## What was accomplished

- Grew Buttr.Core.Tests from **17 → 179 tests**, all passing, ~**92% line / 85% branch coverage**
- New test files across 13 topics: scope, ID-keyed, hidden resolvers, configurable chaining, injection processor, disposal collection, factories, exceptions, object-resolver base, `Application<T>`, configurable collection, abstract/concrete pair variants, mixed-sourcing scenarios, resolver lifecycle, disposal semantics, factory-only types
- Added `coverlet.collector` + `coverlet.msbuild` to the test project for coverage reporting
- Resolved **four discovered bugs / architectural issues**, each with a pinning test:
  1. `WithFactory` didn't bypass ctor-dep validation — fixed across 8 resolvers so factory override short-circuits dep lookup
  2. `ConfigurableCollection` error messages missing `$` interpolation — literal `{typeof(TConcrete)}` was being printed
  3. Mixed-sourcing + interface-keyed deps broke the reorder step in `ServiceResolverUtilities.TryResolve` — replaced with an index-preserving pipeline that removes the reorder entirely
  4. Unreachable `Dispose()` paths on outer resolver types + transient-disposal bug in `DIContainer.Dispose()` — added `IObjectResolver.IsCached`, guarded container dispose on it, cleaned up interface hierarchy so `IResolver` no longer extends `IDisposable`
- Resolved the follow-up from #1: `ObjectResolverBase` no longer requires a public ctor when a factory override is supplied — factory-only types (private/internal ctors, static factory methods) now work
- Deleted the dead `Disposable` class
- Improved the nested-dependency-failure error message: preserves the inner exception, enumerates found vs. unresolved deps, drops the misleading "cyclic dependency" label
- Emptied `KNOWN_ISSUES.md`; populated `BACKLOG.md` with future-work items

## Deviations from plan

- **Initial target was 95% coverage** — settled at ~92%. Most of the remaining gap was unreachable code (outer resolver `Dispose()` methods that no path actually called). Those were deleted in the fix pass for known issue #4, and what's left is either legitimately unreachable or trivial accessors that don't move the needle.
- **Source-level changes weren't in the original scope** — the session started as test-writing only, then the tests surfaced behavioural bugs that were worth fixing immediately. Each bug got the same treatment: discuss, agree, implement, pin.
- **Mid-refactor interruption** on the `ObjectResolverBase` ctor-bypass fix — jumped into implementation before presenting options. Corrected by reverting and presenting A/B/C/D tradeoffs; Jamie picked Option B (bool flag) instead of the initial Option A (pass factory reference). Memory captured: discuss before diving in for non-trivial source changes.

## Decisions made

- **Test framework: NUnit 4.2.2** — chosen to match the Unity Test Framework's idiom, so assertions/attributes transfer directly between Buttr.Core and Buttr.Unity tests. Considered xUnit briefly (more common in Godot .NET); kept NUnit for cross-test-suite consistency.
- **Interface hierarchy cleanup: `IResolver` no longer extends `IDisposable`** — only implementers that do actual dispose work (8 static/hidden resolvers) now carry the interface. Collection `Dispose()` methods pattern-match `is IDisposable` when iterating.
- **Mixed-sourcing rewrite (Option B over A)** — instead of fixing the reorder step's type-equality check, eliminated the reorder step entirely by making `CollectResolvedDependencies` / `CollectUnresolvedTypes` write to `output[i]` instead of `output[count++]`. Cleaner code, no latent ambiguity bug for multi-interface types.
- **`WithFactory` bypass: Option B (bool flag)** — `ObjectResolverBase` accepts `bool skipCtorScan = false`; subclasses pass `skipCtorScan: factoryOverride != null`. Cleaner than passing the factory reference to the base class (Option A).
- **Transient disposal: containers don't dispose them** — guarded `DIContainer` / `ScopeContainer` / `DIContainer<TID>` Dispose on `IsCached`. Consumer owns transient lifetime. Tracked-transient-handouts left as a future opt-in feature in BACKLOG.
- **Comment style: lean** — stripped narrative/historical comments from all recently-touched files per Jamie's feedback. Code stands on its own; test method names describe intent.

## Open items

- `KNOWN_ISSUES.md` is empty. No blockers, no unresolved debt from this pass.
- `BACKLOG.md` has 6 items ranked roughly by likely order: aliasing, hot-path zero-alloc, Ticket async surface, source-gen resolver factories, Godot bridge, tracked transient handouts.
- One design question lurking: `WithFactory` registration with an abstract/interface `TConcrete` (e.g. `AddSingleton<IFoo>().WithFactory(...)`) is currently blocked at `DIBuilder`'s interface guard — separately from the resolver layer. Not a bug; just worth noting as a potential future ergonomic.

## Next session

Per the backlog ordering, **aliasing** is the likely next target (multi-type-key registrations + tag-based filtering for bulk resolution, deferred from the original split plan as v1.1 of Buttr.Core). That's a larger design piece — worth starting with brainstorming and a proper design doc before touching code.

Alternative candidates if aliasing isn't the mood: a **hot-path allocation audit** (smaller, contained win) or the **Ticket async surface discussion** (we flagged it as "let's discuss when we get to it" during the split). Both are bounded and won't bleed across sessions the way aliasing probably will.
