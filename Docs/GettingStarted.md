# Getting started

A five-minute walk-through from an empty project to resolving your first service.

## Install

### .NET (any platform)

```
dotnet add package Buttr.Core
```

That single package brings `Buttr.Core`, `Buttr.Injection`, and the bundled `Buttr.Core.Analyzers`. No other setup.

### Unity

Use the [Buttr Unity package](https://github.com/Crumpet-Labs/Buttr) (`com.crumpetlabs.buttr.unity`) — it vendors the Core DLLs and adds the MonoBehaviour integration.

## Your first container

Buttr containers follow a builder pattern: register everything through a builder, call `Build()`, then resolve through the container.

```csharp
using Buttr.Core;

public interface IGreeter { string Greet(string name); }
public sealed class Greeter : IGreeter {
    public string Greet(string name) => $"Hello, {name}!";
}

var builder = new DIBuilder();
builder.Resolvers.AddSingleton<IGreeter, Greeter>();

var container = builder.Build();
var greeter = container.Get<IGreeter>();
System.Console.WriteLine(greeter.Greet("world"));

container.Dispose();
```

Two keys: `IGreeter` (the abstract consumers ask for) and `Greeter` (the concrete Buttr instantiates). One instance is created and shared.

## Lifetimes

Buttr ships two lifetimes:

- **Singleton** — one instance per container, built lazily on first resolve.
- **Transient** — a new instance every `Get`.

```csharp
builder.Resolvers.AddSingleton<IGreeter, Greeter>();    // one instance
builder.Resolvers.AddTransient<Request>();              // fresh instance per call
```

There is deliberately no "scoped" lifetime — use a `ScopeBuilder` instead (see [Containers](Containers.md)).

## Constructor injection

Dependencies are injected through constructors. Buttr picks the public constructor with the most parameters.

```csharp
public sealed class Mailer {
    public Mailer(IGreeter greeter, ILogger logger) { ... }
}

builder.Resolvers.AddSingleton<IGreeter, Greeter>();
builder.Resolvers.AddSingleton<ILogger, ConsoleLogger>();
builder.Resolvers.AddTransient<Mailer>();

var container = builder.Build();
var mailer = container.Get<Mailer>();

container.Dispose();
```

If a constructor parameter isn't registered, `Get<T>` throws `ObjectResolverException` at resolve time — or the `BUTTR004` analyzer catches it at compile time.

## Overriding construction with a factory

When you need to customise how an instance is built — calling a non-default constructor, wiring up engine objects, etc. — use `.WithFactory()`:

```csharp
builder.Resolvers.AddSingleton<ExpensiveService>()
    .WithFactory(() => new ExpensiveService(config.ApiKey, retries: 3));
```

The factory runs once for singletons, every resolve for transients.

## Post-construction configuration

For cases where you want Buttr to build the instance but then tweak it before returning, use `.WithConfiguration()`:

```csharp
builder.Resolvers.AddSingleton<Mailer>()
    .WithConfiguration(m => { m.From = "noreply@example.com"; return m; });
```

## Aliasing — one resolver, many type keys

Chain `.As<TAlias>()` to expose the same registration under additional interfaces. Singletons share one instance across all keys.

```csharp
builder.Resolvers.AddSingleton<IAuthService, AuthService>()
    .As<IUserService>()
    .As<ISessionProvider>();
```

`Get<IAuthService>()`, `Get<IUserService>()`, and `Get<ISessionProvider>()` all return the same `AuthService`. See [Aliasing](Aliasing.md).

## Bulk resolution

When many registrations implement the same interface, `All<T>` walks them:

```csharp
builder.Resolvers.AddSingleton<StartupPipeline>();
builder.Resolvers.AddSingleton<LoggingPipeline>();
builder.Resolvers.AddSingleton<ShutdownPipeline>();

var container = builder.Build();

foreach (var p in container.All<IPipeline>()) p.Run();

container.Dispose();
```

`All<T>` returns every registration whose concrete type is assignable to `T`. Hidden registrations are excluded. Zero allocations on `foreach`.

## Global access with `Application`

For services that should be resolvable from anywhere without passing a container reference around, use `ApplicationBuilder` — they become accessible statically via `Application<T>.Get()`.

```csharp
var app = new ApplicationBuilder();
app.Resolvers.AddSingleton<IConfig, Config>();
var container = app.Build();

// Later, anywhere in the code:
var cfg = Application<IConfig>.Get();
```

See [Containers](Containers.md) for when `Application` is the right choice.

## Disposal

Containers implement `IDisposable`. Dispose to run cleanup — singletons that implement `IDisposable` get disposed, resolvers are torn down, scopes deregister.

```csharp
var container = builder.Build();
// ... use container ...
container.Dispose();
```

Transient instances you receive via `Get<T>()` are **not** tracked for disposal — the consumer owns their lifetime.

## Where next

- [Containers](Containers.md) — DIContainer vs ScopeContainer vs Application, and the fallback chain.
- [Aliasing](Aliasing.md) — patterns for plugin systems, bulk pipelines, multi-interface services.
- [Hidden registrations](Hidden.md) — services used internally but not exposed publicly.
- [Analyzers](Analyzers.md) — the compile-time diagnostics that ship bundled with Core.
