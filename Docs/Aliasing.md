# Aliasing and bulk resolution

Two related features for when a single service needs to be reachable under multiple type keys, or when many registrations share a type.

## `.As<TAlias>()` — multi-type-key registrations

Chain `.As<TAlias>()` on any registration to add an additional type key that resolves to the **same resolver**. For singletons this means one instance shared across every key.

```csharp
builder.Resolvers.AddSingleton<IAuthService, AuthService>()
    .As<IUserService>()
    .As<ISessionProvider>();
```

All four of these return the same `AuthService` instance:
```csharp
container.Get<IAuthService>()
container.Get<IUserService>()
container.Get<ISessionProvider>()
container.Get<AuthService>()   // only if you also registered the concrete
```

### Supertype rule

`TAlias` must be a supertype of (or equal to) the concrete type. The analyzer **BUTTR013** catches this at compile time; runtime throws `ObjectResolverException` if you get past the analyzer.

```csharp
// Won't compile (analyzer error): string is not a supertype of AuthService
builder.Resolvers.AddSingleton<AuthService>().As<string>();
```

### Chained aliases

Chain `.As<>()` as many times as you want:

```csharp
builder.Resolvers.AddSingleton<Foo>()
    .As<IFoo>()
    .As<IBar>()
    .As<IBaz>();
```

### Duplicate aliases

Each alias key must be unique within a container. If two registrations claim the same alias, **BUTTR014** fires at compile time and `DuplicateAliasException` throws at build time.

If you want multiple implementations reachable by the same interface, use `All<T>` instead.

## `All<T>` — bulk resolution

`All<T>()` returns every registration whose concrete type is assignable to `T`. Hidden registrations are excluded. One instance per registration, regardless of how many aliases that registration carries.

```csharp
builder.Resolvers.AddSingleton<StartupCommand>();
builder.Resolvers.AddSingleton<LoggingCommand>();
builder.Resolvers.AddSingleton<ShutdownCommand>();

foreach (var cmd in ((IDIContainer)container).All<ICommand>()) cmd.Run();
```

`All<T>` uses a struct enumerator, so `foreach` iteration is zero-allocation.

### On the Application surface

`Application.All<T>()` does the same for the global container:

```csharp
foreach (var plugin in Application.All<IPlugin>()) plugin.Activate();
```

## Patterns

### Plugin systems

Drop-in plugins register themselves as a shared interface. Startup iterates.

```csharp
// Each plugin registers itself:
builder.Resolvers.AddSingleton<AudioPlugin>();
builder.Resolvers.AddSingleton<NetworkPlugin>();
builder.Resolvers.AddSingleton<UIPlugin>();

// App startup:
foreach (var plugin in container.All<IPlugin>()) plugin.Initialize();
```

### Layered interface exposure

One concrete, multiple consumer-facing abstractions.

```csharp
builder.Resolvers.AddSingleton<UserRepository>()
    .As<IUserReader>()      // read-only consumers
    .As<IUserWriter>()      // write-capable consumers
    .As<IUserQuery>();      // query builders
```

Singleton semantics mean there's still one `UserRepository` — the aliases are purely consumer-facing contracts.

### Named bulk pipelines

When you want a pipeline of services to run in order, each implementing a shared interface. Combine with registration order (Buttr preserves the registration list's order):

```csharp
builder.Resolvers.AddSingleton<ValidateHandler>();    // runs first
builder.Resolvers.AddSingleton<AuthenticateHandler>();
builder.Resolvers.AddSingleton<ExecuteHandler>();     // runs last

foreach (var handler in container.All<IRequestHandler>()) handler.Process(request);
```

## Performance

Both `.As<>()` and `All<T>()` are zero-alloc at resolve time:

- `.As<>()` adds a dict entry at build time; doesn't affect `Get<T>` speed.
- `All<T>()` returns a struct enumerable. `foreach` allocates nothing; `.ToList()` allocates once (for the list).

See [Benchmarks](Benchmarks/) for numbers.

## When aliasing is the wrong answer

- **"I want two implementations of `IThing` resolved separately."** → Register each concrete (`AddSingleton<ThingA>()`), resolve by concrete type, and optionally use `All<IThing>()` for bulk.
- **"I want to swap the registration at runtime."** → Containers are immutable after `Build()`. Rebuild the container, or use a factory override (`WithFactory`).
- **"I want conditional registration based on environment."** → Do the branching in your bootstrapping code; call `AddSingleton` or not.
