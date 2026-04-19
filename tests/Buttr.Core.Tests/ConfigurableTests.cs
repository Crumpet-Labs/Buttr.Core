using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // ── ApplicationBuilder resolvers: IConfigurable ──────────────────
    public class ApplicationBuilderConfigurableTests {
        public sealed class Item {
            public int Value;
            public Item() { Value = 0; }
            public Item(int seed) { Value = seed; }
        }
        public sealed class NeedsMissingDep {
            public NeedsMissingDep(Item ignoredDep) { }
        }

        [Test]
        public void WithFactory_ReplacesDefaultConstruction_OnSingleton() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Item>().WithFactory(() => new Item(42));
            using var app = (IDisposable)builder.Build();

            var resolved = Application<Item>.Get();
            Assert.That(resolved.Value, Is.EqualTo(42));
        }

        [Test]
        public void WithFactory_ReplacesDefaultConstruction_OnTransient() {
            var counter = 0;
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddTransient<Item>().WithFactory(() => new Item(++counter));
            using var app = (IDisposable)builder.Build();

            var a = Application<Item>.Get();
            var b = Application<Item>.Get();

            Assert.That(a.Value, Is.EqualTo(1));
            Assert.That(b.Value, Is.EqualTo(2));
        }

        [Test]
        public void WithFactory_BypassesCtorDependencyValidation() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<NeedsMissingDep>()
                .WithFactory(() => new NeedsMissingDep(null));
            using var app = (IDisposable)builder.Build();

            var resolved = Application<NeedsMissingDep>.Get();
            Assert.That(resolved, Is.Not.Null);
        }

        [Test]
        public void WithConfiguration_TransformsInstance_AfterConstruction() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Item>()
                .WithConfiguration(x => { x.Value = 100; return x; });
            using var app = (IDisposable)builder.Build();

            Assert.That(Application<Item>.Get().Value, Is.EqualTo(100));
        }

        [Test]
        public void WithFactory_AndWithConfiguration_ComposeInOrder() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Item>()
                .WithFactory(() => new Item(10))
                .WithConfiguration(x => { x.Value *= 3; return x; });
            using var app = (IDisposable)builder.Build();

            // Factory produces 10 → configuration multiplies by 3 → 30
            Assert.That(Application<Item>.Get().Value, Is.EqualTo(30));
        }

        [Test]
        public void WithConfiguration_CalledTwice_LastRegistrationWins() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Item>()
                .WithConfiguration(x => { x.Value = 1; return x; })
                .WithConfiguration(x => { x.Value = 2; return x; });
            using var app = (IDisposable)builder.Build();

            Assert.That(Application<Item>.Get().Value, Is.EqualTo(2));
        }

        [Test]
        public void WithFactory_CalledTwice_LastRegistrationWins() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Item>()
                .WithFactory(() => new Item(7))
                .WithFactory(() => new Item(99));
            using var app = (IDisposable)builder.Build();

            Assert.That(Application<Item>.Get().Value, Is.EqualTo(99));
        }

        [Test]
        public void WithConfiguration_ReturnsSameConfigurable_ForChaining() {
            var builder = new ApplicationBuilder();
            var configurable = builder.Resolvers.AddSingleton<Item>();

            var chained = configurable.WithConfiguration(x => x);
            Assert.That(chained, Is.SameAs(configurable));
        }

        [Test]
        public void WithFactory_ReturnsSameConfigurable_ForChaining() {
            var builder = new ApplicationBuilder();
            var configurable = builder.Resolvers.AddSingleton<Item>();

            var chained = configurable.WithFactory(() => new Item(0));
            Assert.That(chained, Is.SameAs(configurable));
        }
    }

    // ── DIBuilder resolvers: IConfigurable ───────────────────────────
    public class DIBuilderConfigurableTests {
        public sealed class Widget {
            public string Label;
            public Widget() { Label = "default"; }
            public Widget(string label) { Label = label; }
        }

        [Test]
        public void WithFactory_Singleton_OverridesConstruction() {
            var builder = new DIBuilder();
            builder.AddSingleton<Widget>().WithFactory(() => new Widget("factory"));
            using var container = (IDisposable)builder.Build();

            var w = ((IDIContainer)container).Get<Widget>();
            Assert.That(w.Label, Is.EqualTo("factory"));
        }

        [Test]
        public void WithConfiguration_Transient_TransformsEveryInstance() {
            var seed = 0;
            var builder = new DIBuilder();
            builder.AddTransient<Widget>()
                .WithConfiguration(w => { w.Label = "configured-" + (++seed); return w; });
            using var container = (IDisposable)builder.Build();

            var a = ((IDIContainer)container).Get<Widget>();
            var b = ((IDIContainer)container).Get<Widget>();

            Assert.That(a.Label, Is.EqualTo("configured-1"));
            Assert.That(b.Label, Is.EqualTo("configured-2"));
        }
    }

    // ── ScopeBuilder resolvers: IConfigurable ────────────────────────
    public class ScopeBuilderConfigurableTests {
        public sealed class Gadget {
            public double Setting;
            public Gadget() { Setting = 0; }
        }

        [TearDown]
        public void ClearScopeRegistry() => ScopeRegistry.Clear();

        [Test]
        public void WithFactory_InScope_OverridesConstruction() {
            var builder = new ScopeBuilder("configurable-scope-factory");
            builder.AddSingleton<Gadget>().WithFactory(() => new Gadget { Setting = 3.14 });
            using var container = builder.Build();

            Assert.That(container.Get<Gadget>().Setting, Is.EqualTo(3.14));
        }

        [Test]
        public void WithConfiguration_InScope_TransformsInstance() {
            var builder = new ScopeBuilder("configurable-scope-config");
            builder.AddSingleton<Gadget>()
                .WithConfiguration(g => { g.Setting = 9.99; return g; });
            using var container = builder.Build();

            Assert.That(container.Get<Gadget>().Setting, Is.EqualTo(9.99));
        }
    }

    // ── DIBuilder<TID> resolvers: IConfigurable ──────────────────────
    public class DIBuilderIDConfigurableTests {
        public sealed class Doodad {
            public int Id;
            public Doodad() { Id = 0; }
        }

        [Test]
        public void WithFactory_Singleton_ByID_OverridesConstruction() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Doodad>(1).WithFactory(() => new Doodad { Id = 55 });
            using var container = builder.Build();

            Assert.That(container.Get<Doodad>(1).Id, Is.EqualTo(55));
        }

        [Test]
        public void WithConfiguration_Transient_ByID_TransformsInstance() {
            var builder = new DIBuilder<string>();
            builder.AddTransient<Doodad>("x")
                .WithConfiguration(d => { d.Id = 77; return d; });
            using var container = builder.Build();

            Assert.That(container.Get<Doodad>("x").Id, Is.EqualTo(77));
        }
    }
}
