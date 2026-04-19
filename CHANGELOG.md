# Changelog

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
