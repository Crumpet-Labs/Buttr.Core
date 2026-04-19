<p align="center">
  <img src="Docs/Images/buttr-wordmark.svg" alt="Buttr" width="320" />
</p>

<p align="center">
  Engine-agnostic dependency-injection container for .NET. Pure C#, zero runtime dependencies.
</p>

<p align="center">
  <a href="#quick-start">Quick start</a> ·
  <a href="Docs/GettingStarted.md">Getting started</a> ·
  <a href="Docs/Containers.md">Containers</a> ·
  <a href="Docs/Aliasing.md">Aliasing</a> ·
  <a href="Docs/Analyzers.md">Analyzers</a> ·
  <a href="Docs/Benchmarks/">Benchmarks</a>
</p>

---

## About

Buttr is a lightweight dependency-injection framework. The Core library is **engine-agnostic** — it targets `netstandard2.1` and has no dependency on Unity, Godot, or any other engine. Game-engine integrations live in sibling repos that vendor these DLLs and add engine-specific glue on top.

Design priorities, in order:

1. **Zero-allocation on the hot path.** Resolved singleton `Get<T>` is 0 B allocated, measured.
2. **API first.** The builder chain is what Buttr was designed around — short, fluent, obvious.
3. **Low barrier to entry.** If you've used any DI container, you already know the shape.

## Assemblies

| Assembly | Purpose |
|---|---|
| `Buttr.Core` | DI container, builders, resolvers, lifetimes, scopes, `Application<T>` |
| `Buttr.Injection` | `[Inject]` marker attribute + `InjectionProcessor` registry — consumed by engine-side source generators |
| `Buttr.Core.Analyzers` | Roslyn analyzers + fixers, bundled in the Buttr.Core NuGet at `analyzers/dotnet/cs/` |

## Quick start

```csharp
using Buttr.Core;

// 1. Build a container
var builder = new DIBuilder();
builder.Resolvers.AddSingleton<IAuthService, AuthService>();
builder.Resolvers.AddTransient<RequestHandler>();

using var container = (IDisposable)builder.Build();

// 2. Resolve
var auth = ((IDIContainer)container).Get<IAuthService>();

// 3. Bulk-resolve every implementation of an interface
builder.Resolvers.AddSingleton<StartupCommand>().As<ICommand>();
builder.Resolvers.AddSingleton<ShutdownCommand>().As<ICommand>();
foreach (var cmd in ((IDIContainer)container).All<ICommand>()) cmd.Run();
```

See [Getting Started](Docs/GettingStarted.md) for a full walk-through, [Containers](Docs/Containers.md) for the `DIContainer` / `ScopeContainer` / `Application` distinction, and [Aliasing](Docs/Aliasing.md) for `.As<>()` and `All<T>()` patterns.

## Benchmarks (2026-04-19, .NET 8)

| Container | `Get<T>` singleton (resolved) | `Get<T>` transient (3 deps) | `All<T>` (5 matches) |
|---|---|---|---|
| DI | 13 ns, **0 B** | 64 ns, 40 B | 49 ns, **0 B** |
| Scope | 9 ns, **0 B** | 64 ns, 40 B | 48 ns, **0 B** |
| Application (static) | 3 ns, **0 B** | 58 ns, 40 B | 71 ns, **0 B** |

Transient allocation equals the new instance — `TryResolve` adds no visible overhead. `All<T>` uses a struct enumerator so `foreach` is zero-allocation.

Reproducing and full reports in [Docs/Benchmarks/](Docs/Benchmarks/).

## Unity / Godot / Stride consumers

Engine-specific bridges live in their own repos and vendor the Buttr.Core DLLs. The Unity bridge is the first and most mature:

- **[Buttr Unity package](https://github.com/Crumpet-Labs/Buttr)** (`com.crumpetlabs.buttr.unity`) — MonoBehaviour wiring, scene walker, `[Inject]` source generator.

If you're writing a new engine bridge, the pattern is: vendor the Core DLLs, add an engine-specific source generator that wires `[Inject]`-decorated fields at the engine's appropriate lifecycle hook.

## Build & test

```
dotnet build -c Release
dotnet test
```

## Versioning

Semantic — minor bumps for additive features (aliasing in 1.1, analyzers in 1.2, zero-alloc `All<T>` in 1.3), majors reserved for binary-breaking changes.

## Contributing

See [Docs/Contributing.md](Docs/Contributing.md). Short version: keep comments out of code, discuss architectural changes first, and tests must stay green.

## License

MIT — see [LICENSE.md](LICENSE.md).
