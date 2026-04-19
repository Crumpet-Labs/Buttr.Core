# Benchmarks

Dated benchmark reports for Buttr.Core. Each entry captures a run of the full `tests/Buttr.Core.Benchmarks/` suite against a specific build, so future perf work has a paper trail rather than a rolling top-of-master table.

## Reports

- [2026-04-19 baseline](2026-04-19-baseline.md) — first full capture, pre-optimisation.
- [2026-04-19 post-optimisation](2026-04-19-post-optimisation.md) — after the struct-enumerator swap for `All<T>`. Context in [CHANGELOG 1.3.0](../../CHANGELOG.md).

## Reproducing

All benchmarks live in `tests/Buttr.Core.Benchmarks/` (BenchmarkDotNet 0.14.0, net8.0, `MemoryDiagnoser` enabled).

```
cd tests/Buttr.Core.Benchmarks
dotnet run -c Release -- --filter "*"
```

Individual benchmark classes can be targeted with a filter:

```
dotnet run -c Release -- --filter "*DIContainerBenchmarks*"
dotnet run -c Release -- --filter "*All*"
```

A single run takes ~6–10 minutes across all three container classes × four methods each.

## Interpreting the numbers

- **Mean** — average time per call. Fractional-nanosecond differences are noise; focus on changes >1 ns or ≥5 % relative.
- **Allocated** — bytes per call. This is the value that matters for a DI container on the hot path. `Get<T>` on a resolved singleton should be 0 B; transient should equal the instance size.
- **Gen0** — the number of Gen-0 GCs per 1000 calls. `-` or near-zero is the target.

BenchmarkDotNet default job uses a separate process per class, JIT warmup, statistical sampling (N≥15 iterations), and outlier removal. Individual sub-nanosecond measurements still have meaningful variance; re-run to confirm anything close to noise.

## Machine notes

Reports include a header block with CPU, Windows build, .NET SDK, and runtime. A number captured on a laptop doing other work is less useful than one captured on an idle machine. For ongoing perf tracking, prefer the same host over time and mention any known load in the report.

## Scenarios covered

Every container class runs these four benchmarks so we can compare surfaces apples-to-apples:

1. **`Get<T>` resolved singleton (reference type)** — the hottest real-world path. Should be 0 B.
2. **`Get<T>` transient (0 deps)** — baseline transient cost.
3. **`Get<T>` transient (3 deps)** — exercises the `TryResolve` dependency-resolution path.
4. **`All<T>` bulk resolve (5 matches)** — bulk enumeration.

New scenarios land alongside features. If a future feature has a non-obvious cost model, add a scenario here and capture before/after numbers in a dated report.

## When to capture a new report

- After a change expected to affect any hot path.
- Before a release, to confirm no regression crept in.
- When triaging a perf complaint — capture against the exact reported build, not latest master.

## Files touched

Keep each report as a single markdown file in `Docs/Benchmarks/YYYY-MM-DD-label.md`. Link from this README. Don't edit historical reports — add a new one.
