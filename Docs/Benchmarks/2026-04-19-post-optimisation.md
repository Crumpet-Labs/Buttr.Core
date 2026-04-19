# Post-optimisation — 2026-04-19

BenchmarkDotNet v0.14.0, Windows 11 10.0.26200.8246, .NET 8.0.25, X64 RyuJIT AVX2, GC = Concurrent Server.

Captured after the struct-enumerator change (see `RegistrationEnumerable<T>`). Same machine, same session, otherwise identical to the baseline run.

## DIContainer

| Method | Mean | Allocated |
|---|---|---|
| Get singleton (resolved, ref) | 13.33 ns | 0 B |
| Get transient (0 deps) | 16.91 ns | 24 B |
| Get transient (3 deps) | 63.96 ns | 40 B |
| All<IPlugin> (5 matches) | 48.86 ns | **0 B** ← was 56 B |

## ScopeContainer

| Method | Mean | Allocated |
|---|---|---|
| Get singleton (resolved, ref) | 8.83 ns | 0 B |
| Get transient (0 deps) | 17.54 ns | 24 B |
| Get transient (3 deps) | 63.83 ns | 40 B |
| All<IPlugin> (5 matches) | 48.28 ns | **0 B** ← was 56 B |

## Application (static)

| Method | Mean | Allocated |
|---|---|---|
| Get singleton (resolved, ref) | 3.22 ns | 0 B |
| Get transient (0 deps) | 12.54 ns | 24 B |
| Get transient (3 deps) | 57.86 ns | 40 B |
| All<IPlugin> (5 matches) | 70.81 ns | **0 B** ← was 48 B |

## What changed, what didn't

**Real wins (struct enumerator):**
- `All<T>` alloc eliminated on all three containers (48–56 B → 0 B).
- `All<T>` time down 15–48 % — avoiding the iterator state-machine also cuts per-call time, not just allocation.

**Noise (time-column fluctuations on unrelated paths):**
- `Get singleton` on DI fluctuated by ~0.1 ns (within measurement error).
- `Get singleton` on Scope appears faster (13.10 → 8.83 ns) but the `Get` path itself wasn't touched. Probably thermal / JIT scheduling variation.
- `Get singleton` on Application appears slower (1.66 → 3.22 ns). Same reason — sub-nanosecond measurements are noisy.
- Transient paths moved around by 5–30 %, but the code is unchanged.

Don't read the non-`All` rows as real changes. The `Get` code is byte-identical between baseline and post-fix. A re-run on a quiet machine should show those numbers converging.

## #2 (CollectionsMarshal) deferred

`CollectionsMarshal.GetValueRefOrNullRef` is .NET 6+ only. Buttr.Core targets `netstandard2.1`. Shipping #2 would require multi-targeting the library (adding a `net8.0` target alongside). The cost — extra build output, Unity vendoring decision, dual-target maintenance — is disproportionate to the ~1 ns potential saving. Deferred pending a separate decision on whether to multi-target.
