# Analyzers

Buttr.Core ships Roslyn analyzers bundled in the main NuGet package at `analyzers/dotnet/cs/Buttr.Core.Analyzers.dll`. They activate automatically on any project that references `Buttr.Core` — no extra package to install.

Rule severity can be customised per-project via `.editorconfig` (`dotnet_diagnostic.BUTTR###.severity = warning|suggestion|error|none`).

## v1 rule catalog

| ID | Title | Severity | Fixer |
|---|---|---|---|
| **BUTTR004** | Constructor parameter may not be resolvable | Warning | — |
| **BUTTR006** | Duplicate registration in container | Error | Remove duplicate |
| **BUTTR012** | `IDisposable` type registered as transient | Warning | Change to singleton |
| **BUTTR013** | Alias must be a supertype of the concrete | Error | Replace with valid supertype |
| **BUTTR014** | Duplicate alias key across registrations | Error | Remove duplicate alias |

---

### BUTTR004 — Constructor parameter may not be resolvable

**Category:** `Buttr.Injection` · **Severity:** Warning · **Fixer:** none

Fires when a registered type has a constructor parameter whose type isn't registered in the same container (or its fallback chain).

```csharp
public sealed class Mailer {
    public Mailer(IGreeter greeter, ILogger logger) { } // ← BUTTR004: ILogger not registered
}

var builder = new DIBuilder();
builder.Resolvers.AddSingleton<IGreeter, Greeter>();
builder.Resolvers.AddSingleton<Mailer>();
// ILogger is missing — BUTTR004 fires at compile time, ObjectResolverException at runtime.
```

No fixer because auto-fixing is inherently ambiguous: add the missing registration? Change to a factory override? Refactor the constructor? The diagnostic guides you; the correct fix depends on your intent.

**Suppress when:** the dependency is registered by an external plugin or framework that Buttr can't see.

---

### BUTTR006 — Duplicate registration in container

**Category:** `Buttr.Injection` · **Severity:** Error · **Fixer:** remove duplicate

Fires when the same type key is registered twice in the same container. The second registration would overwrite the first at runtime.

```csharp
builder.Resolvers.AddSingleton<IFoo, FooA>();
builder.Resolvers.AddSingleton<IFoo, FooB>(); // ← BUTTR006
```

**Fixer** removes the second `AddSingleton`/`AddTransient` statement. Invoke via the IDE Quick Fix menu.

**Suppress when:** never — this is always a bug. If you want multiple `IFoo` implementations, make them distinct concrete registrations and use `All<IFoo>()`.

---

### BUTTR012 — `IDisposable` type registered as transient

**Category:** `Buttr.Lifetime` · **Severity:** Warning · **Fixer:** change to singleton

Fires when a type implementing `IDisposable` (or `IAsyncDisposable`) is registered as transient. Buttr doesn't track transient instances — each call to `Get<T>` hands one out and forgets about it — so disposal becomes the consumer's problem.

```csharp
builder.Resolvers.AddTransient<FileWriter>(); // ← BUTTR012 if FileWriter : IDisposable
```

**Fixer** converts `AddTransient` to `AddSingleton` (which Buttr *does* dispose on container dispose).

**Suppress when:** you're deliberately taking manual ownership and have a `using` discipline at every call-site.

---

### BUTTR013 — Alias must be a supertype of the concrete

**Category:** `Buttr.Injection` · **Severity:** Error · **Fixer:** replace with valid supertype

Fires when the type passed to `.As<TAlias>()` isn't assignable from the concrete.

```csharp
public sealed class Foo { } // no base / interfaces

builder.Resolvers.AddSingleton<Foo>().As<IDisposable>(); // ← BUTTR013
```

**Fixer** offers every interface the concrete implements and every base class as alias candidates. If `Foo` implements `IFoo` and `IBar`, the Quick Fix menu shows "Use `IFoo` as the alias" and "Use `IBar` as the alias".

Pre-analyzer fallback: the same constraint is enforced at container build time via `ObjectResolverException`.

---

### BUTTR014 — Duplicate alias key across registrations

**Category:** `Buttr.Injection` · **Severity:** Error · **Fixer:** remove duplicate alias

Fires when two registrations claim the same alias key.

```csharp
builder.Resolvers.AddSingleton<FooA>().As<IFoo>();
builder.Resolvers.AddSingleton<FooB>().As<IFoo>(); // ← BUTTR014
```

**Fixer** removes the second `.As<IFoo>()` call. You can invoke on either registration to choose which one keeps the alias.

Pre-analyzer fallback: `DuplicateAliasException` at build time.

**Suppress when:** never — this is always a bug. Use `All<IFoo>()` if you want both implementations reachable.

---

## Relationship to the Unity source-gen analyzers

Historically, these rules lived in `Buttr.Unity.SourceGeneration` (the Unity-side Roslyn project). They were lifted to Buttr.Core in 1.2.0 — Unity-specific rules (`BUTTR001` partial class, `BUTTR011` `[Inject]` on non-MonoBehaviour) and the `InjectSourceGenerator` stayed in the Unity repo because they depend on Unity types or on the source-generator runtime.

Full ownership principle: *Buttr concepts live in Buttr.Core. Engine concepts live in the engine bridge.*

## Configuring severity

In a project's `.editorconfig`:

```
[*.cs]
dotnet_diagnostic.BUTTR004.severity = suggestion   # downgrade from warning
dotnet_diagnostic.BUTTR012.severity = error         # upgrade from warning
```

## Adding `Buttr.Core.Analyzers` to an existing project

Nothing to do — if you reference `Buttr.Core`, the analyzer is automatically picked up from the NuGet's `analyzers/dotnet/cs/` folder.

## Future rules

See [BACKLOG.md](../BACKLOG.md). Additional engine-agnostic rules from the Unity origin (hidden-type checks, unused registration, magic scope strings, ctor injection resolution for Scope) are planned for later Core releases.
