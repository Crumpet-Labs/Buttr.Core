using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ApplicationPairResolverTests {
        public interface IThing { int Value { get; set; } }
        public sealed class Thing : IThing { public int Value { get; set; } }
        public sealed class DisposableThing : IThing, IDisposable {
            public int Value { get; set; }
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        [Test]
        public void StaticSingletonResolver_Pair_ResolvesConcreteThroughAbstract() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<IThing, Thing>();
            using var app = builder.Build();

            var a = Application<IThing>.Get();
            var b = Application<IThing>.Get();

            Assert.That(a, Is.InstanceOf<Thing>());
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void StaticSingletonResolver_Pair_WithFactory_OverridesConstruction() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<IThing, Thing>()
                .WithFactory(() => new Thing { Value = 11 });
            using var app = builder.Build();

            Assert.That(Application<IThing>.Get().Value, Is.EqualTo(11));
        }

        [Test]
        public void StaticSingletonResolver_Pair_WithConfiguration_TransformsInstance() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<IThing, Thing>()
                .WithConfiguration(t => { t.Value = 22; return t; });
            using var app = builder.Build();

            Assert.That(Application<IThing>.Get().Value, Is.EqualTo(22));
        }

        [Test]
        public void StaticSingletonResolver_Pair_Dispose_DisposesResolvedIDisposable() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<IThing, DisposableThing>();
            var app = builder.Build();
            var instance = (DisposableThing)Application<IThing>.Get();

            app.Dispose();

            Assert.That(instance.Disposed, Is.True);
        }

        [Test]
        public void StaticTransientResolver_Pair_ResolvesNewConcretePerGet() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddTransient<IThing, Thing>();
            using var app = builder.Build();

            var a = Application<IThing>.Get();
            var b = Application<IThing>.Get();

            Assert.That(a, Is.InstanceOf<Thing>());
            Assert.That(b, Is.InstanceOf<Thing>());
            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void StaticTransientResolver_Pair_WithFactory_OverridesConstruction() {
            var counter = 0;
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddTransient<IThing, Thing>()
                .WithFactory(() => new Thing { Value = ++counter });
            using var app = builder.Build();

            Assert.That(Application<IThing>.Get().Value, Is.EqualTo(1));
            Assert.That(Application<IThing>.Get().Value, Is.EqualTo(2));
        }
    }

    public class HiddenStaticPairResolverTests {
        public interface IHidden { }
        public sealed class Hidden : IHidden { }
        public sealed class VisibleConsumer {
            public IHidden H;
            public VisibleConsumer(IHidden h) { H = h; }
        }

        [Test]
        public void HiddenStaticSingleton_Pair_RegistersAsDepButNotAsApplicationResolver() {
            var builder = new ApplicationBuilder();
            builder.Hidden.AddSingleton<IHidden, Hidden>();
            builder.Resolvers.AddSingleton<VisibleConsumer>();
            using var app = builder.Build();

            var consumer = Application<VisibleConsumer>.Get();
            Assert.That(consumer.H, Is.InstanceOf<Hidden>());
        }

        [Test]
        public void HiddenStaticTransient_Pair_RegistersAsDepButNotAsApplicationResolver() {
            var builder = new ApplicationBuilder();
            builder.Hidden.AddTransient<IHidden, Hidden>();
            builder.Resolvers.AddTransient<VisibleConsumer>();
            using var app = builder.Build();

            var a = Application<VisibleConsumer>.Get();
            var b = Application<VisibleConsumer>.Get();
            Assert.That(a.H, Is.Not.SameAs(b.H));
        }

        public sealed class ConcreteHidden { }
        public sealed class Consumes {
            public ConcreteHidden Dep;
            public Consumes(ConcreteHidden d) { Dep = d; }
        }

        [Test]
        public void HiddenStaticSingleton_Single_Generic_RegistersAsDep() {
            var builder = new ApplicationBuilder();
            builder.Hidden.AddSingleton<ConcreteHidden>();
            builder.Resolvers.AddSingleton<Consumes>();
            using var app = builder.Build();

            Assert.That(Application<Consumes>.Get().Dep, Is.Not.Null);
        }

        [Test]
        public void HiddenStaticTransient_Single_Generic_RegistersAsDep() {
            var builder = new ApplicationBuilder();
            builder.Hidden.AddTransient<ConcreteHidden>();
            builder.Resolvers.AddTransient<Consumes>();
            using var app = builder.Build();

            var a = Application<Consumes>.Get();
            var b = Application<Consumes>.Get();
            Assert.That(a.Dep, Is.Not.SameAs(b.Dep));
        }
    }

    public class DIBuilderPairResolverTests {
        public interface IWidget { string Label { get; set; } }
        public sealed class Widget : IWidget { public string Label { get; set; } }

        [Test]
        public void DIBuilder_Singleton_Pair_ResolvesConcreteThroughAbstract() {
            var builder = new DIBuilder();
            builder.AddSingleton<IWidget, Widget>();
            using var container = builder.Build();

            var a = container.Get<IWidget>();
            var b = container.Get<IWidget>();

            Assert.That(a, Is.InstanceOf<Widget>());
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void DIBuilder_Transient_Pair_YieldsNewConcretePerGet() {
            var builder = new DIBuilder();
            builder.AddTransient<IWidget, Widget>();
            using var container = builder.Build();

            var a = container.Get<IWidget>();
            var b = container.Get<IWidget>();

            Assert.That(a, Is.InstanceOf<Widget>());
            Assert.That(a, Is.Not.SameAs(b));
        }
    }

    public class ScopeBuilderPairResolverTests {
        public interface IGadget { double Setting { get; set; } }
        public sealed class Gadget : IGadget { public double Setting { get; set; } }

        [TearDown]
        public void ClearScopes() => ScopeRegistry.Clear();

        [Test]
        public void ScopeBuilder_Singleton_Pair_ResolvesConcreteThroughAbstract() {
            var builder = new ScopeBuilder("pair-scope-singleton");
            builder.AddSingleton<IGadget, Gadget>();
            using var container = builder.Build();

            var a = container.Get<IGadget>();
            var b = container.Get<IGadget>();

            Assert.That(a, Is.InstanceOf<Gadget>());
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void ScopeBuilder_Transient_Pair_YieldsNewConcretePerGet() {
            var builder = new ScopeBuilder("pair-scope-transient");
            builder.AddTransient<IGadget, Gadget>();
            using var container = builder.Build();

            var a = container.Get<IGadget>();
            var b = container.Get<IGadget>();

            Assert.That(a, Is.InstanceOf<Gadget>());
            Assert.That(a, Is.Not.SameAs(b));
        }
    }
}
