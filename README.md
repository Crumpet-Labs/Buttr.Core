# Buttr

Engine-agnostic dependency-injection container for .NET. Pure C#, no engine
dependencies, targets `netstandard2.1`.

This repository ships two assemblies:

| Assembly          | Purpose                                                                |
|-------------------|------------------------------------------------------------------------|
| `Buttr.Core`      | DI container, builders, resolvers, lifetimes, scopes, `Application<T>` |
| `Buttr.Injection` | `[Inject]` attribute, `IInjectable`, engine-agnostic injection registry |

Unity users should depend on the [Buttr Unity package](https://github.com/Crumpet-Labs/Buttr)
(`com.crumpetlabs.buttr.unity`), which vendors the DLLs from this repository and adds
a MonoBehaviour / scene-walking bridge.

## Build

```
dotnet build -c Release
```

## Test

```
dotnet test
```

## License

MIT — see [LICENSE.md](LICENSE.md).
