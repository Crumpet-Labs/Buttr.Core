# Changelog

## 1.3.0 — Performance audit + `All<T>` zero-alloc

Full BenchmarkDotNet pass over the resolve hot paths (baseline + post-fix
reports in `Docs/Benchmarks/`). Findings:

- **Singleton `Get<T>` was already zero-alloc on every container** — the Stage 1
  Registration refactor didn't regress the hottest path.
- **Transient allocation equals the new instance** — `TryResolve` / `ArrayPool`
  path is effectively free in steady state.
- **`All<T>` allocated 48–56 B per call** — the `yield return` state machine
  was creating a fresh ref-type enumerator every call.

### Changes

- New `RegistrationEnumerable<T>` struct (with a nested struct `Enumerator`)
  replaces the compiler-synthesised iterator for bulk resolution.
- `IDIContainer.All<T>()` return type changes from `IEnumerable<T>` to
  `RegistrationEnumerable<T>`. The struct still implements `IEnumerable<T>`
  so LINQ and legacy call sites keep working; `foreach` now hits the struct
  `GetEnumerator()` and allocates zero.
- Same treatment for `Application.All<T>()`.

### Measured result

`All<T>` drops from 48–56 B to **0 B** per call across `DIContainer`,
`ScopeContainer`, and `Application`. Per-call time also improves 15–48 %
purely from avoiding the state-machine allocation.

### Deferred

`CollectionsMarshal.GetValueRefOrNullRef` micro-optimisation on `Get<T>`
would require multi-targeting the library (netstandard2.1 → +net8.0) for a
~1 ns saving. Disproportionate trade; revisit as a separate call on
multi-targeting.

## 1.2.0 — Analyzers

Five compile-time diagnostics ship alongside Buttr.Core via a new
`Buttr.Core.Analyzers` project bundled into the Buttr.Core NuGet at
`analyzers/dotnet/cs/`. Consumers referencing Buttr.Core pick the analyzers up
automatically; no extra package reference is needed.

### Rules

- **BUTTR004** (Warning) — constructor parameter of a registered type may not
  be resolvable from its container; diagnostic-only (no fixer, auto-fix is
  inherently ambiguous).
- **BUTTR006** (Error) — same type registered twice in the same container; the
  second registration overwrites the first. Fixer removes the duplicate.
- **BUTTR012** (Warning) — `IDisposable` type registered as transient; the
  container doesn't track or dispose transients. Fixer converts to singleton.
- **BUTTR013** (Error) — alias type passed to `.As<TAlias>()` is not a
  supertype of the concrete type. Replaces the runtime
  `ObjectResolverException` thrown in 1.1. Fixer suggests any interface or
  base class the concrete does implement.
- **BUTTR014** (Error) — two registrations claim the same alias key. Replaces
  the runtime `DuplicateAliasException`. Fixer removes the second `.As<>()`
  call.

### Unity coordination

`Buttr.Unity.SourceGeneration` retains Unity-specific rules (BUTTR001 partial
class requirement, BUTTR011 non-MonoBehaviour `[Inject]`) and the
`InjectSourceGenerator`. Unity's copies of BUTTR004, BUTTR006, and BUTTR012
should be deleted in the matching Buttr Unity 2.3.0 release — Core's analyzer
now owns those rule IDs. IDs and messages are stable; no user-facing change
apart from the rules firing from a different assembly.

## 1.1.0 — Aliasing

Multi-type-key registrations plus bulk resolution. One resolver can now be
reached under multiple consumer-facing type keys, and a new `All<T>()` surface
returns every registration whose concrete type is assignable to `T`.

### Additions

- `IConfigurable<TConcrete>.As<TAlias>()` — adds an additional type key that
  resolves to the same underlying resolver. Singletons share one instance
  across all keys. The alias must be a supertype of the concrete; a runtime
  `ObjectResolverException` is thrown otherwise. A compile-time diagnostic
  from the forthcoming `Buttr.Core.Analyzers` project will take over this
  check later.
- `IConfigurableCollection.As<TConcrete, TAlias>()` — surfaces the same
  capability for package authors assembling configurable bundles.
- `IDIContainer.All<T>()` — bulk-resolves every registration whose concrete
  type is assignable to `T`. Hidden registrations are excluded. One instance
  per registration regardless of alias count.
- `Application.All<T>()` — mirrors `Application<T>.Get()` for bulk access on
  the global container.
- `DuplicateAliasException` — thrown when two registrations claim the same
  alias key. For many implementations of a shared interface, use `All<T>()`
  instead.

### Internals

- Registrations are now first-class: a new internal `Registration` type
  carries the resolver, primary key, concrete type, alias set, and hidden
  flag. Containers keep a `List<Registration>` as the source of truth and a
  `Dictionary<Type, Registration>` as a pure key index. The dict no longer
  doubles as storage, and resolvers no longer write themselves into a
  registry on `.Resolve()` — the builder wires the registration up.
- `DIBuilder<TID>` is explicitly untouched: aliasing is a type-keyed feature.
  Calling `.As<>()` on an ID-keyed configurable throws
  `NotSupportedException`.

## 1.0.0 — Initial engine-agnostic release

Extracted the engine-agnostic pieces of `com.crumpetlabs.buttr` 2.1.4 into a
standalone .NET library. See `README.md` for the repository layout.

### Highlights

- Pure C# — zero Unity dependencies.
- Targets `netstandard2.1`; compatible with Unity 6000 and Godot 4 (.NET).
- New `ButtrLog` facade replaces direct `UnityEngine.Debug` calls.
- `CMDArgs.Initialize(IEnumerable<string>)` replaces the Unity runtime-init hook.
- `InjectionProcessor` is now a pure registry + single-instance inject — Unity
  scene-walking helpers live in `Buttr.Unity`.
