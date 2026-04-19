```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 9.0.308
  [Host]     : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX2


```
| Method                        | Mean      | Error     | StdDev    | Median    | Gen0   | Allocated |
|------------------------------ |----------:|----------:|----------:|----------:|-------:|----------:|
| Get_ResolvedSingleton_RefType |  3.215 ns | 0.1946 ns | 0.5615 ns |  3.416 ns |      - |         - |
| Get_Transient_ZeroDeps        | 12.542 ns | 0.2012 ns | 0.1783 ns | 12.463 ns | 0.0000 |      24 B |
| Get_Transient_ThreeDeps       | 57.855 ns | 0.2190 ns | 0.1941 ns | 57.853 ns |      - |      40 B |
| All_FiveMatchingRegistrations | 70.810 ns | 0.5320 ns | 0.4716 ns | 70.807 ns |      - |         - |
