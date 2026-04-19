```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 9.0.308
  [Host]     : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX2


```
| Method                        | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------------------------ |----------:|----------:|----------:|-------:|----------:|
| Get_ResolvedSingleton_RefType |  8.828 ns | 0.0216 ns | 0.0192 ns |      - |         - |
| Get_Transient_ZeroDeps        | 17.537 ns | 0.0958 ns | 0.0896 ns | 0.0000 |      24 B |
| Get_Transient_ThreeDeps       | 63.829 ns | 0.3023 ns | 0.2680 ns |      - |      40 B |
| All_FiveMatchingRegistrations | 48.280 ns | 0.0983 ns | 0.0821 ns |      - |         - |
