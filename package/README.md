# Buttr.Core (UPM)

Engine-agnostic dependency-injection container for .NET, delivered as a Unity UPM package that vendors the pre-built `Buttr.Core` and `Buttr.Injection` assemblies.

Source, benchmarks, and the canonical documentation live in the [Buttr.Core repo](https://github.com/Crumpet-Labs/Buttr.Core). This UPM package exists solely so Unity projects can consume the engine-agnostic core as a git dependency; for non-Unity .NET projects, prefer the `Buttr.Core` NuGet package.

## What's here

```
Runtime/
└── Lib/
    ├── Buttr.Core.dll       — container, builders, resolvers, scopes, Application<T>
    └── Buttr.Injection.dll  — [Inject] attribute + InjectionProcessor registry
```

## Paired with

For Unity MonoBehaviour/ScriptableObject wiring and the `[Inject]` source generator, install [Buttr.Unity](https://github.com/Crumpet-Labs/Buttr.Unity) alongside this package. Buttr.Unity references these same assemblies.

## Install

Add to your `Packages/manifest.json`:

```json
"com.crumpetlabs.buttr": "https://github.com/Crumpet-Labs/Buttr.Core.git?path=package"
```

Pin a specific version by appending a tag, e.g. `#1.3.2`.

## License

MIT — see [LICENSE.md](LICENSE.md).
