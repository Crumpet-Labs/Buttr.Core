# Containers

Buttr ships three container shapes. They share the same registration model but differ in visibility, lifetime, and fallback behaviour.

| Container | Builder | Shape | Resolution surface |
|---|---|---|---|
| DIContainer | `DIBuilder` | Type-keyed, locally scoped | `container.Get<T>()` |
| ScopeContainer | `ScopeBuilder("key")` | Type-keyed, named scope | `container.Get<T>()` |
| Application | `ApplicationBuilder` | Type-keyed, process-global | `Application<T>.Get()` |

All three implement `IDIContainer` and share the same `Get<T>` / `TryGet<T>` / `All<T>` surface.

## DIContainer

A self-contained type-keyed container. Created via `DIBuilder`.

```csharp
var builder = new DIBuilder();
builder.Resolvers.AddSingleton<IGreeter, Greeter>();

var container = builder.Build();
var greeter = container.Get<IGreeter>();

container.Dispose();
```

Use this when:
- You want a bounded container for a specific feature, test, or subsystem.
- Registrations shouldn't leak to global `Application<T>` accessors.
- You want explicit control over disposal timing.

**Fallback:** if a constructor dependency isn't registered in this container, Buttr falls back to `Application<T>` for that type. This lets you layer a local container over a global one — local registrations win, unresolved deps fall back.

## ScopeContainer

Same as `DIContainer` but named. Created via `ScopeBuilder(string key)`. The key is registered in a process-global scope registry so scopes can be looked up later.

```csharp
var scope = new ScopeBuilder("inventory-scope");
scope.Resolvers.AddSingleton<IInventoryService, InventoryService>();

var container = scope.Build();
var service = container.Get<IInventoryService>();

container.Dispose();
```

Use this when:
- A subsystem owns its own services but needs a stable lookup key.
- You want scope-specific registrations alongside application-wide ones.

**Fallback:** identical to `DIContainer` — unresolved dependencies fall back to `Application<T>`.

## Application (global static)

Created via `ApplicationBuilder`. Registrations populate the process-global `Application<T>` accessor.

```csharp
var app = new ApplicationBuilder();
app.Resolvers.AddSingleton<IConfig, Config>();
var container = app.Build();

// Accessible from anywhere, no container reference needed:
var cfg = Application<IConfig>.Get();

// When the application shuts down:
container.Dispose();
```

Use this when:
- A service is genuinely process-global (config, logging facade, scene broadcasters).
- Engine-side source generators need to resolve dependencies from a well-known location (the `[Inject]` story in Unity relies on this).

**Caveat:** like any static, `Application<T>` makes testing harder. Prefer `DIContainer`/`ScopeContainer` for services you might want to swap in tests.

### `Application.All<T>()`

For bulk access over the global container:

```csharp
foreach (var handler in Application.All<IStartupHandler>()) handler.Run();
```

Mirrors `IDIContainer.All<T>()` for the static surface. Zero-alloc `foreach`.

## Hidden registrations

Every builder exposes a parallel `.Hidden` collection for registrations that shouldn't be externally resolvable:

```csharp
builder.Resolvers.AddSingleton<IPublicService, PublicService>();
builder.Hidden.AddSingleton<IInternalHelper, InternalHelper>();
```

`Get<IInternalHelper>()` throws. But `PublicService`'s constructor can take `IInternalHelper` as a dependency — internal wiring still works. See [Hidden registrations](Hidden.md).

## Disposal semantics

- `DIContainer.Dispose()` and `ScopeContainer.Dispose()` walk their registrations and dispose any cached singletons that implement `IDisposable`. Scope additionally unregisters from the scope registry.
- `ApplicationContainer.Dispose()` disposes all `ApplicationBuilder`-added singletons and clears their `Application<T>.s_Resolver` slots.
- **Transient instances are not tracked.** The consumer owns their lifetime.

## Which one should I use?

- **Default answer: `DIContainer`.** Bounded, explicit, testable.
- **Use `ScopeContainer`** when you have a subsystem with a natural key and want scope lookup semantics.
- **Use `ApplicationBuilder`** for services that truly need global reach, or when you're writing an engine bridge that depends on the static accessors.

Mixing works: an app can have one `ApplicationBuilder` for global config plus several `DIContainer`s for feature-local state. Local wins on overlapping registrations; the fallback chain handles the rest.
