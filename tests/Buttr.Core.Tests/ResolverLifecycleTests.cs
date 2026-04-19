using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // Closes coverage gaps on the `<TAbstract, TConcrete>` pair variants and
    // single-generic resolvers that weren't exercised through Dispose /
    // WithConfiguration / WithFactory in the first pass.
    //
    // Every resolver type in the codebase implements IConfigurable and Dispose;
    // both must be hit at least once per variant.

    public class ResolverLifecycleTests {
        public interface IThing { int V { get; set; } }
        public sealed class Thing : IThing { public int V { get; set; } }
        public sealed class DisposableThing : IThing, IDisposable {
            public int V { get; set; }
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        // ── SingletonObjectResolver<TAbstract, TConcrete> ────────────
        public class ContainerSingletonPair {
            [Test]
            public void Pair_WithFactory_Overrides() {
                var builder = new DIBuilder();
                builder.AddSingleton<IThing, Thing>()
                    .WithFactory(() => new Thing { V = 7 });
                using var container = builder.Build();
                Assert.That(container.Get<IThing>().V, Is.EqualTo(7));
            }

            [Test]
            public void Pair_WithConfiguration_Transforms() {
                var builder = new DIBuilder();
                builder.AddSingleton<IThing, Thing>()
                    .WithConfiguration(t => { t.V = 13; return t; });
                using var container = builder.Build();
                Assert.That(container.Get<IThing>().V, Is.EqualTo(13));
            }

            [Test]
            public void Pair_Dispose_DisposesResolvedIDisposable() {
                var builder = new DIBuilder();
                builder.AddSingleton<IThing, DisposableThing>();
                var container = builder.Build();
                var instance = (DisposableThing)container.Get<IThing>();

                container.Dispose();
                Assert.That(instance.Disposed, Is.True);
            }
        }

        // ── TransientObjectResolver<TAbstract, TConcrete> ────────────
        public class ContainerTransientPair {
            [Test]
            public void Pair_WithFactory_Overrides() {
                var counter = 0;
                var builder = new DIBuilder();
                builder.AddTransient<IThing, Thing>()
                    .WithFactory(() => new Thing { V = ++counter });
                using var container = builder.Build();

                Assert.That(container.Get<IThing>().V, Is.EqualTo(1));
                Assert.That(container.Get<IThing>().V, Is.EqualTo(2));
            }

            [Test]
            public void Pair_WithConfiguration_Transforms() {
                var builder = new DIBuilder();
                builder.AddTransient<IThing, Thing>()
                    .WithConfiguration(t => { t.V = 21; return t; });
                using var container = builder.Build();

                Assert.That(container.Get<IThing>().V, Is.EqualTo(21));
            }
        }

        // ── TransientObjectResolver<TConcrete> ───────────────────────
        public class ContainerTransientSingle {
            public sealed class Single { public int V; }

            [Test]
            public void Single_WithFactory_Overrides() {
                var counter = 0;
                var builder = new DIBuilder();
                builder.AddTransient<Single>()
                    .WithFactory(() => new Single { V = ++counter });
                using var container = builder.Build();

                Assert.That(container.Get<Single>().V, Is.EqualTo(1));
                Assert.That(container.Get<Single>().V, Is.EqualTo(2));
            }

            [Test]
            public void Single_WithConfiguration_Transforms() {
                var builder = new DIBuilder();
                builder.AddTransient<Single>()
                    .WithConfiguration(s => { s.V = 33; return s; });
                using var container = builder.Build();

                Assert.That(container.Get<Single>().V, Is.EqualTo(33));
            }
        }

        // ── SingletonObjectResolver<TConcrete> Dispose IDisposable ───
        public class ContainerSingletonSingleDispose {
            public sealed class D : IDisposable {
                public bool Disposed;
                public void Dispose() => Disposed = true;
            }

            [Test]
            public void Single_Dispose_DisposesResolvedIDisposable() {
                var builder = new DIBuilder();
                builder.AddSingleton<D>();
                var container = builder.Build();
                var instance = container.Get<D>();

                container.Dispose();
                Assert.That(instance.Disposed, Is.True);
            }
        }

        // ── IDSingletonObjectResolver / IDTransientObjectResolver pair
        //    forms (the generic itself takes <TID, TConcrete>; these
        //    tests verify WithConfiguration + WithFactory on both) ────
        public class IDResolverConfigurablePaths {
            public sealed class Thing { public int V; }

            [Test]
            public void IDSingleton_WithFactory_Overrides() {
                var builder = new DIBuilder<int>();
                builder.AddSingleton<Thing>(1)
                    .WithFactory(() => new Thing { V = 50 });
                using var container = builder.Build();

                Assert.That(container.Get<Thing>(1).V, Is.EqualTo(50));
            }

            [Test]
            public void IDSingleton_WithConfiguration_Transforms() {
                var builder = new DIBuilder<int>();
                builder.AddSingleton<Thing>(2)
                    .WithConfiguration(t => { t.V = 60; return t; });
                using var container = builder.Build();

                Assert.That(container.Get<Thing>(2).V, Is.EqualTo(60));
            }

            [Test]
            public void IDTransient_WithFactory_Overrides() {
                var seed = 0;
                var builder = new DIBuilder<int>();
                builder.AddTransient<Thing>(3)
                    .WithFactory(() => new Thing { V = ++seed });
                using var container = builder.Build();

                Assert.That(container.Get<Thing>(3).V, Is.EqualTo(1));
                Assert.That(container.Get<Thing>(3).V, Is.EqualTo(2));
            }

            [Test]
            public void IDTransient_WithConfiguration_Transforms() {
                var builder = new DIBuilder<int>();
                builder.AddTransient<Thing>(4)
                    .WithConfiguration(t => { t.V = 77; return t; });
                using var container = builder.Build();

                Assert.That(container.Get<Thing>(4).V, Is.EqualTo(77));
            }
        }

        // ── HiddenStaticSingletonResolver<T> + <T,T> Configurable ────
        public class HiddenSingletonConfigurablePaths {
            public interface IDep { int V { get; set; } }
            public sealed class Dep : IDep { public int V { get; set; } }
            public sealed class ConcreteDep { public int V; }

            public sealed class Consumer {
                public IDep I;
                public Consumer(IDep i) { I = i; }
            }
            public sealed class ConsumesConcrete {
                public ConcreteDep C;
                public ConsumesConcrete(ConcreteDep c) { C = c; }
            }

            [Test]
            public void HiddenSingleton_Pair_WithFactory_AppliesInDepInjection() {
                var builder = new ApplicationBuilder();
                builder.Hidden.AddSingleton<IDep, Dep>()
                    .WithFactory(() => new Dep { V = 101 });
                builder.Resolvers.AddSingleton<Consumer>();
                using var app = builder.Build();

                Assert.That(Application<Consumer>.Get().I.V, Is.EqualTo(101));
            }

            [Test]
            public void HiddenSingleton_Pair_WithConfiguration_AppliesInDepInjection() {
                var builder = new ApplicationBuilder();
                builder.Hidden.AddSingleton<IDep, Dep>()
                    .WithConfiguration(d => { d.V = 202; return d; });
                builder.Resolvers.AddSingleton<Consumer>();
                using var app = builder.Build();

                Assert.That(Application<Consumer>.Get().I.V, Is.EqualTo(202));
            }

            [Test]
            public void HiddenSingleton_Single_WithFactory_AppliesInDepInjection() {
                var builder = new ApplicationBuilder();
                builder.Hidden.AddSingleton<ConcreteDep>()
                    .WithFactory(() => new ConcreteDep { V = 303 });
                builder.Resolvers.AddSingleton<ConsumesConcrete>();
                using var app = builder.Build();

                Assert.That(Application<ConsumesConcrete>.Get().C.V, Is.EqualTo(303));
            }

            [Test]
            public void HiddenSingleton_Single_WithConfiguration_AppliesInDepInjection() {
                var builder = new ApplicationBuilder();
                builder.Hidden.AddSingleton<ConcreteDep>()
                    .WithConfiguration(c => { c.V = 404; return c; });
                builder.Resolvers.AddSingleton<ConsumesConcrete>();
                using var app = builder.Build();

                Assert.That(Application<ConsumesConcrete>.Get().C.V, Is.EqualTo(404));
            }
        }

        // ── HiddenStaticTransientResolver<T> + <T,T> Configurable ────
        public class HiddenTransientConfigurablePaths {
            public interface IDep { int V { get; set; } }
            public sealed class Dep : IDep { public int V { get; set; } }
            public sealed class ConcreteDep { public int V; }

            public sealed class Consumer {
                public IDep I;
                public Consumer(IDep i) { I = i; }
            }
            public sealed class ConsumesConcrete {
                public ConcreteDep C;
                public ConsumesConcrete(ConcreteDep c) { C = c; }
            }

            [Test]
            public void HiddenTransient_Pair_WithFactory_AppliesInDepInjection() {
                var seed = 0;
                var builder = new ApplicationBuilder();
                builder.Hidden.AddTransient<IDep, Dep>()
                    .WithFactory(() => new Dep { V = ++seed });
                builder.Resolvers.AddTransient<Consumer>();
                using var app = builder.Build();

                Assert.That(Application<Consumer>.Get().I.V, Is.EqualTo(1));
                Assert.That(Application<Consumer>.Get().I.V, Is.EqualTo(2));
            }

            [Test]
            public void HiddenTransient_Pair_WithConfiguration_AppliesInDepInjection() {
                var builder = new ApplicationBuilder();
                builder.Hidden.AddTransient<IDep, Dep>()
                    .WithConfiguration(d => { d.V = 500; return d; });
                builder.Resolvers.AddTransient<Consumer>();
                using var app = builder.Build();

                Assert.That(Application<Consumer>.Get().I.V, Is.EqualTo(500));
            }

            [Test]
            public void HiddenTransient_Single_WithFactory_AppliesInDepInjection() {
                var seed = 0;
                var builder = new ApplicationBuilder();
                builder.Hidden.AddTransient<ConcreteDep>()
                    .WithFactory(() => new ConcreteDep { V = ++seed });
                builder.Resolvers.AddTransient<ConsumesConcrete>();
                using var app = builder.Build();

                Assert.That(Application<ConsumesConcrete>.Get().C.V, Is.EqualTo(1));
                Assert.That(Application<ConsumesConcrete>.Get().C.V, Is.EqualTo(2));
            }

            [Test]
            public void HiddenTransient_Single_WithConfiguration_AppliesInDepInjection() {
                var builder = new ApplicationBuilder();
                builder.Hidden.AddTransient<ConcreteDep>()
                    .WithConfiguration(c => { c.V = 600; return c; });
                builder.Resolvers.AddTransient<ConsumesConcrete>();
                using var app = builder.Build();

                Assert.That(Application<ConsumesConcrete>.Get().C.V, Is.EqualTo(600));
            }
        }

        // ── StaticTransientResolver<T, T> pair — WithConfiguration / WithFactory
        public class StaticTransientPairConfigurable {
            public interface IT { int V { get; set; } }
            public sealed class T : IT { public int V { get; set; } }

            [Test]
            public void Pair_WithFactory_Overrides() {
                var builder = new ApplicationBuilder();
                builder.Resolvers.AddTransient<IT, T>().WithFactory(() => new T { V = 800 });
                using var app = builder.Build();

                Assert.That(Application<IT>.Get().V, Is.EqualTo(800));
            }

            [Test]
            public void Pair_WithConfiguration_Transforms() {
                var builder = new ApplicationBuilder();
                builder.Resolvers.AddTransient<IT, T>().WithConfiguration(t => { t.V = 900; return t; });
                using var app = builder.Build();

                Assert.That(Application<IT>.Get().V, Is.EqualTo(900));
            }
        }
    }
}
