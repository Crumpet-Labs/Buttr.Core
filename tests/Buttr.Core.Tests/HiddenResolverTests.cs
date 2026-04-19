using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // ── Container-level hidden (DIBuilder) ───────────────────────────
    public class DIBuilderHiddenTests {
        public interface ISecret { }
        public sealed class Secret : ISecret { }
        public sealed class Visible {
            public ISecret Secret;
            public Visible(ISecret secret) { Secret = secret; }
        }
        public sealed class MultiVisible {
            public Secret ConcreteA;
            public OtherSecret ConcreteB;
            public MultiVisible(Secret a, OtherSecret b) { ConcreteA = a; ConcreteB = b; }
        }
        public sealed class OtherSecret { }

        [Test]
        public void Hidden_AddSingleton_BlocksGet() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<Secret>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => _ = container.Get<Secret>());
        }

        [Test]
        public void Hidden_AddSingleton_BlocksTryGet() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<Secret>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => container.TryGet<Secret>(out _));
        }

        [Test]
        public void Hidden_Singleton_StillResolvesAsConstructorDependency() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<ISecret, Secret>();
            builder.AddSingleton<Visible>();
            using var container = builder.Build();

            var visible = container.Get<Visible>();
            Assert.That(visible, Is.Not.Null);
            Assert.That(visible.Secret, Is.Not.Null);
            Assert.That(visible.Secret, Is.InstanceOf<Secret>());
        }

        [Test]
        public void Hidden_AbstractConcretePair_KeyedOnAbstract() {
            // HiddenObjectResolverCollection.AddSingleton<TAbstract, TConcrete>()
            // stores typeof(TAbstract) in the hidden set. Get<TAbstract> throws,
            // but Get<TConcrete> does not — the concrete type isn't marked hidden.
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<ISecret, Secret>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => _ = container.Get<ISecret>());
            // Concrete isn't hidden, and isn't registered under its own key either,
            // so it returns default — documenting the asymmetry.
            Assert.That(container.Get<Secret>(), Is.Null);
        }

        [Test]
        public void Hidden_Transient_BlocksGet_ButResolvesAsDependency() {
            var builder = new DIBuilder();
            builder.Hidden.AddTransient<ISecret, Secret>();
            builder.AddSingleton<Visible>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => _ = container.Get<ISecret>());

            var visible = container.Get<Visible>();
            Assert.That(visible.Secret, Is.Not.Null);
        }

        [Test]
        public void Hidden_MultipleTypes_AllBlockGetAndAllResolveAsDependencies() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<Secret>();
            builder.Hidden.AddSingleton<OtherSecret>();
            builder.AddSingleton<MultiVisible>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => _ = container.Get<Secret>());
            Assert.Throws<ObjectResolverException>(() => _ = container.Get<OtherSecret>());

            var multi = container.Get<MultiVisible>();
            Assert.That(multi.ConcreteA, Is.Not.Null);
            Assert.That(multi.ConcreteB, Is.Not.Null);
        }
    }

    // ── Container-level hidden (ScopeBuilder) ────────────────────────
    public class ScopeBuilderHiddenTests {
        public interface IHiddenService { }
        public sealed class HiddenService : IHiddenService { }
        public sealed class DependsOnHidden {
            public IHiddenService Service;
            public DependsOnHidden(IHiddenService s) { Service = s; }
        }

        [TearDown]
        public void ClearScopeRegistry() => ScopeRegistry.Clear();

        [Test]
        public void Hidden_Singleton_InScope_StillResolvesAsDependency() {
            var builder = new ScopeBuilder("scope-hidden-dep");
            builder.Hidden.AddSingleton<IHiddenService, HiddenService>();
            builder.AddSingleton<DependsOnHidden>();
            using var container = builder.Build();

            var dependent = container.Get<DependsOnHidden>();
            Assert.That(dependent, Is.Not.Null);
            Assert.That(dependent.Service, Is.Not.Null);
            Assert.That(dependent.Service, Is.InstanceOf<HiddenService>());
        }
    }

    // ── Application-level hidden (ApplicationBuilder.Hidden) ─────────
    public class ApplicationHiddenTests {
        public interface IHiddenDep { }
        public sealed class HiddenDep : IHiddenDep { }
        public sealed class VisibleConsumer {
            public IHiddenDep Dep;
            public VisibleConsumer(IHiddenDep dep) { Dep = dep; }
        }

        // Unique types per-test keep Application<T>.s_Resolver state clean between runs.
        public sealed class HiddenOnly { }

        [Test]
        public void ApplicationHidden_DoesNotSetApplicationResolver() {
            // A Hidden registration must not expose the type via Application<T>.Get().
            // Application<T>.s_Resolver is only written by StaticSingletonResolver /
            // StaticTransientResolver — NOT by the Hidden variants.
            var builder = new ApplicationBuilder();
            builder.Hidden.AddSingleton<HiddenOnly>();
            using var app = builder.Build();

            // s_Resolver is still null for this T — Get() would NRE.
            // We don't assert the exception shape, just that no one set it.
            Assert.Throws<NullReferenceException>(() => _ = Application<HiddenOnly>.Get());
        }

        [Test]
        public void ApplicationHidden_RegistersInApplicationRegistry_ForDependencyResolution() {
            // A visible resolver with a hidden ctor dep must still resolve.
            var builder = new ApplicationBuilder();
            builder.Hidden.AddSingleton<IHiddenDep, HiddenDep>();
            builder.Resolvers.AddSingleton<VisibleConsumer>();
            using var app = builder.Build();

            var consumer = Application<VisibleConsumer>.Get();
            Assert.That(consumer, Is.Not.Null);
            Assert.That(consumer.Dep, Is.Not.Null);
            Assert.That(consumer.Dep, Is.InstanceOf<HiddenDep>());
        }

        [Test]
        public void ApplicationHidden_Transient_StillYieldsFreshDependencyPerConsumer() {
            // Transient hidden dep: two visible consumers get distinct instances
            // (transient re-constructs on every resolve).
            var builder = new ApplicationBuilder();
            builder.Hidden.AddTransient<IHiddenDep, HiddenDep>();
            builder.Resolvers.AddTransient<VisibleConsumer>();
            using var app = builder.Build();

            var a = Application<VisibleConsumer>.Get();
            var b = Application<VisibleConsumer>.Get();

            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a.Dep, Is.Not.SameAs(b.Dep));
        }
    }
}
