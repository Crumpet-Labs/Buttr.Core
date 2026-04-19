using System;
using BenchmarkDotNet.Attributes;
using Buttr.Core;

namespace Buttr.Core.Benchmarks;

[MemoryDiagnoser]
public class ApplicationBenchmarks {
    private ApplicationContainer m_App = null!;

    [GlobalSetup]
    public void Setup() {
        var builder = new ApplicationBuilder();
        builder.Resolvers.AddSingleton<Ping>();
        builder.Resolvers.AddSingleton<Pong>();
        builder.Resolvers.AddSingleton<Ding>();
        builder.Resolvers.AddTransient<TriadConsumer>();
        builder.Resolvers.AddTransient<Zero>();
        builder.Resolvers.AddSingleton<PluginA>().As<IPlugin>();
        builder.Resolvers.AddSingleton<PluginB>();
        builder.Resolvers.AddSingleton<PluginC>();
        builder.Resolvers.AddSingleton<PluginD>();
        builder.Resolvers.AddSingleton<PluginE>();

        m_App = builder.Build();
        _ = Application<Ping>.Get();
        _ = Application<TriadConsumer>.Get();
    }

    [GlobalCleanup]
    public void Cleanup() => m_App.Dispose();

    [Benchmark]
    public Ping Get_ResolvedSingleton_RefType() => Application<Ping>.Get();

    [Benchmark]
    public Zero Get_Transient_ZeroDeps() => Application<Zero>.Get();

    [Benchmark]
    public TriadConsumer Get_Transient_ThreeDeps() => Application<TriadConsumer>.Get();

    [Benchmark]
    public int All_FiveMatchingRegistrations() {
        var count = 0;
        foreach (var _ in Application.All<IPlugin>()) count++;
        return count;
    }
}
