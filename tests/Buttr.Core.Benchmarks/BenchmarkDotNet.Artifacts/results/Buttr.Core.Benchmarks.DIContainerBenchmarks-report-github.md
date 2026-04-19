```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 9.0.308
  [Host]     : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX2


```
| Method                        | Mean     | Error    | StdDev   | Allocated |
|------------------------------ |---------:|---------:|---------:|----------:|
| Get_ResolvedSingleton_RefType | 13.33 ns | 0.180 ns | 0.159 ns |         - |
| Get_Transient_ZeroDeps        | 16.91 ns | 0.274 ns | 0.487 ns |      24 B |
| Get_Transient_ThreeDeps       | 63.96 ns | 0.117 ns | 0.098 ns |      40 B |
| All_FiveMatchingRegistrations | 48.86 ns | 0.102 ns | 0.085 ns |         - |
