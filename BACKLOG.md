# Backlog

Scope items to work through after the initial test-coverage + bug-fix pass.
Entries are loosely grouped by theme.

## Container capabilities

### Tracked transient handouts

Containers currently cannot dispose transient instances they hand out — the
consumer owns the lifetime. Some DI frameworks (e.g. Autofac) track instances
produced by a container and dispose owned transients when the container is
disposed. Worth considering if an opt-in "owned" flag makes sense for Buttr.

## Performance

### Multi-target for `CollectionsMarshal.GetValueRefOrNullRef`

`Get<T>` on `DIContainer` / `ScopeContainer` currently uses
`Dictionary.TryGetValue`. `CollectionsMarshal.GetValueRefOrNullRef` (.NET 6+)
would shave ~1 ns per call on the typical hit path, but requires multi-targeting
the library (netstandard2.1 → +net8.0). Audited 2026-04-19 and deferred —
worth revisiting when there's an independent reason to multi-target (e.g. AOT
build variants or consumer demand for a net8-specific build).

### Source-gen resolver factories

Replace runtime expression-tree compilation with compile-time source-gen for
resolver factories. Required for NativeAOT / IL2CPP without JIT; opens
Buttr.Core to Godot AOT builds too. Big piece of work — needs its own plan.

## Async surface

### Ticket replacement for `UnityEngine.Awaitable`

Core shipped v1.0.0 without an async surface. Follow-up decides how to model
async in the engine-agnostic library — candidates include `Task`, `ValueTask`,
or the in-house `Clabs.Tickets.Ticket` primitive.

## Engines

### Godot bridge

Port the Unity-side bootstrap / logger adapter / node injection pattern to
Godot 4. Deferred from the split plan.
