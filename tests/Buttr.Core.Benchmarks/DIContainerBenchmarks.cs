using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Buttr.Core;

namespace Buttr.Core.Benchmarks;

[MemoryDiagnoser]
public class DIContainerBenchmarks {
    private IDIContainer m_Container = null!;

    [GlobalSetup]
    public void Setup() {
        var builder = new DIBuilder();
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

        m_Container = builder.Build();
        _ = m_Container.Get<Ping>();
        _ = m_Container.Get<TriadConsumer>();
    }

    [GlobalCleanup]
    public void Cleanup() {
        if (m_Container is IDisposable d) d.Dispose();
    }

    [Benchmark]
    public Ping Get_ResolvedSingleton_RefType() => m_Container.Get<Ping>();

    [Benchmark]
    public Zero Get_Transient_ZeroDeps() => m_Container.Get<Zero>();

    [Benchmark]
    public TriadConsumer Get_Transient_ThreeDeps() => m_Container.Get<TriadConsumer>();

    [Benchmark]
    public int All_FiveMatchingRegistrations() {
        var count = 0;
        foreach (var _ in m_Container.All<IPlugin>()) count++;
        return count;
    }
}
