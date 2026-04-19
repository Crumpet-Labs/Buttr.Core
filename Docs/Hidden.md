# Hidden registrations

Every container builder exposes two parallel collections:

- `.Resolvers` — normal public registrations.
- `.Hidden` — registrations that are available as **constructor dependencies** but not via `Get<T>()`.

```csharp
var builder = new DIBuilder();
builder.Resolvers.AddSingleton<IPublicApi, PublicApi>();
builder.Hidden.AddSingleton<IInternalCache, InternalCache>();
```

The runtime behaviour:

```csharp
container.Get<IPublicApi>();    // OK
container.Get<IInternalCache>(); // throws ObjectResolverException
```

But `PublicApi`'s constructor can declare `IInternalCache cache` as a parameter and Buttr will inject it normally. The hidden flag only gates the consumer-facing lookup; the internal dependency graph still sees everything.

## Why

A Buttr container is often used to assemble a subsystem that exposes a small public surface. Hidden lets you wire the internals — helpers, caches, factories, engine-specific bridges — without polluting the consumer-facing API.

### Before Hidden (everything public)

```csharp
builder.Resolvers.AddSingleton<IAuthService, AuthService>();
builder.Resolvers.AddSingleton<ITokenStore, TokenStore>();     // implementation detail
builder.Resolvers.AddSingleton<IHashingAlgorithm, SHA256Hash>(); // implementation detail

container.Get<ITokenStore>();   // consumer reaches into internals — bad
```

### With Hidden

```csharp
builder.Resolvers.AddSingleton<IAuthService, AuthService>();
builder.Hidden.AddSingleton<ITokenStore, TokenStore>();
builder.Hidden.AddSingleton<IHashingAlgorithm, SHA256Hash>();

container.Get<ITokenStore>();   // throws — consumer forced to go through IAuthService
```

`AuthService`'s constructor still receives `ITokenStore` and `IHashingAlgorithm`. The consumer API stays single-responsibility.

## Scope of the hide

- `.Resolvers.AddSingleton<TAbstract, TConcrete>()` — `TAbstract` is the public key.
- `.Hidden.AddSingleton<TAbstract, TConcrete>()` — `TAbstract` is blocked from `Get<T>()`; still injectable as a dependency.
- `.Hidden.AddSingleton<TConcrete>()` (single type-arg form) — `TConcrete` is blocked, injectable.

## Aliasing and Hidden interact

Aliases on a hidden registration inherit the hidden flag:

```csharp
builder.Hidden.AddSingleton<ISecret, Secret>().As<ISecretAlias>();

container.Get<ISecret>();       // throws
container.Get<ISecretAlias>();  // also throws
container.All<ISecret>();       // excludes this registration
```

Resolving a hidden type throws `ObjectResolverException` at runtime. A future Core analyzer release is planned to catch the direct-resolve case at compile time (tracked in `BACKLOG.md` as BUTTR002/003 lifted from the Unity source-gen project).

## Every builder has Hidden

- `DIBuilder.Hidden`
- `ScopeBuilder.Hidden`
- `ApplicationBuilder.Hidden` — for Application-scope services that aren't exposed via `Application<T>.Get()` but ARE injectable as dependencies.

## Typical uses

- **Engine bridges** — scene-walkers, lifecycle adapters, engine-specific loggers. Consumers shouldn't see them; engine code needs them.
- **Factories** — helper types whose job is to build other registered services. Expose the result, hide the factory.
- **Internal caches** — memoisation stores that are implementation details of a service.

## When not to use Hidden

- **"I want to prevent misuse."** Hidden isn't a security boundary — internal access within the same assembly can still get at these types. It's an API hygiene tool.
- **"I want different implementations in test vs prod."** Use factory overrides (`.WithFactory()`) or register conditionally at bootstrap.
- **"I want to expose this through an abstract interface only."** Normal registration with `.AddSingleton<TAbstract, TConcrete>()` does that — consumers ask for `TAbstract`, the concrete stays uncallable via its concrete type key. Hidden is only needed when you want to *block* even the abstract key from external consumers.
