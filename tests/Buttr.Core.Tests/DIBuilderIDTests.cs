using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // ── DIBuilder<TID> — ID-keyed container builder ──────────────────
    public class DIBuilderIDTests {
        public interface INotAllowed { }
        public sealed class Alpha { }
        public sealed class Beta { }
        public sealed class Gamma { }
        public enum Key { One, Two, Three }

        [Test]
        public void Build_ReturnsContainer_WithTypePropertyMatchingTID() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Alpha>(1);
            using var container = builder.Build();

            Assert.That(container.Type, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void AddSingleton_YieldsSameInstanceOnEveryGet() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Alpha>(1);
            using var container = builder.Build();

            var a = container.Get<Alpha>(1);
            var b = container.Get<Alpha>(1);

            Assert.That(a, Is.Not.Null);
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void AddTransient_YieldsNewInstanceOnEveryGet() {
            var builder = new DIBuilder<string>();
            builder.AddTransient<Alpha>("a");
            using var container = builder.Build();

            var a = container.Get<Alpha>("a");
            var b = container.Get<Alpha>("a");

            Assert.That(a, Is.Not.Null);
            Assert.That(b, Is.Not.Null);
            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void AddSingleton_Throws_WhenTConcreteIsInterface() {
            var builder = new DIBuilder<int>();
            Assert.Throws<ArgumentException>(() => builder.AddSingleton<INotAllowed>(1));
        }

        [Test]
        public void AddTransient_Throws_WhenTConcreteIsInterface() {
            var builder = new DIBuilder<int>();
            Assert.Throws<ArgumentException>(() => builder.AddTransient<INotAllowed>(1));
        }

        [Test]
        public void DifferentIDs_CanMapToDifferentConcreteTypes() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Alpha>(1);
            builder.AddSingleton<Beta>(2);
            builder.AddSingleton<Gamma>(3);
            using var container = builder.Build();

            Assert.That(container.Get<Alpha>(1), Is.InstanceOf<Alpha>());
            Assert.That(container.Get<Beta>(2), Is.InstanceOf<Beta>());
            Assert.That(container.Get<Gamma>(3), Is.InstanceOf<Gamma>());
        }

        [Test]
        public void EnumTID_RoundTripsCorrectly() {
            var builder = new DIBuilder<Key>();
            builder.AddSingleton<Alpha>(Key.One);
            builder.AddSingleton<Beta>(Key.Two);
            using var container = builder.Build();

            Assert.That(container.Get<Alpha>(Key.One), Is.InstanceOf<Alpha>());
            Assert.That(container.Get<Beta>(Key.Two), Is.InstanceOf<Beta>());
        }

        [Test]
        public void DuplicateID_LastRegistrationWinsAtBuild() {
            // Current behaviour: at Resolve() time the registry is written with
            // an indexer assignment (not Add), so the second registration for the
            // same ID overwrites the first. Documenting the behaviour — if this
            // changes (e.g. to throw) this test will flag it.
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Alpha>(7);
            builder.AddSingleton<Beta>(7);
            using var container = builder.Build();

            var resolved = container.Get<Beta>(7);
            Assert.That(resolved, Is.InstanceOf<Beta>());
        }
    }

    // ── DIContainer<TID> ─────────────────────────────────────────────
    public class DIContainerIDTests {
        public sealed class Thing { }
        public sealed class DisposableThing : IDisposable {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        [Test]
        public void Get_ReturnsDefault_WhenIDUnknown() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Thing>(1);
            using var container = builder.Build();

            Assert.That(container.Get<Thing>(999), Is.Null);
        }

        [Test]
        public void TryGet_ReturnsFalseAndDefault_WhenIDUnknown() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Thing>(1);
            using var container = builder.Build();

            var found = container.TryGet<Thing>(999, out var value);
            Assert.That(found, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void TryGet_ReturnsTrueAndInstance_WhenIDKnown() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Thing>(1);
            using var container = builder.Build();

            var found = container.TryGet<Thing>(1, out var value);
            Assert.That(found, Is.True);
            Assert.That(value, Is.Not.Null);
            Assert.That(value, Is.InstanceOf<Thing>());
        }

        [Test]
        public void Dispose_DisposesResolvedSingletonDisposables() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<DisposableThing>(1);
            var container = builder.Build();
            var thing = container.Get<DisposableThing>(1);

            container.Dispose();

            Assert.That(thing.Disposed, Is.True);
        }

        [Test]
        public void Dispose_ClearsRegistry_SubsequentGetReturnsDefault() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Thing>(1);
            var container = builder.Build();
            Assert.That(container.Get<Thing>(1), Is.Not.Null);

            container.Dispose();

            Assert.That(container.Get<Thing>(1), Is.Null);
        }
    }

    // ── ID-keyed resolver + ApplicationRegistry fallback ─────────────
    public class DIBuilderIDDependencyResolutionTests {
        public sealed class Leaf { }
        public sealed class Branch {
            public Leaf Leaf;
            public Branch(Leaf leaf) { Leaf = leaf; }
        }
        public sealed class StandaloneNeedsMissing {
            public StandaloneNeedsMissing(Leaf leaf) { }
        }

        [Test]
        public void Singleton_WithCtorDep_ResolvesThroughApplicationRegistry() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<Leaf>();
            using var appContainer = app.Build();

            var builder = new DIBuilder<int>();
            builder.AddSingleton<Branch>(1);
            using var container = builder.Build();

            var branch = container.Get<Branch>(1);

            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Leaf, Is.Not.Null);
        }

        [Test]
        public void Singleton_WithMissingCtorDep_Throws_AtResolve() {
            // Leaf intentionally NOT registered anywhere.
            var builder = new DIBuilder<int>();
            builder.AddSingleton<StandaloneNeedsMissing>(1);
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => _ = container.Get<StandaloneNeedsMissing>(1));
        }

        [Test]
        public void Transient_WithCtorDep_ResolvesThroughApplicationRegistry() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<Leaf>();
            using var appContainer = app.Build();

            var builder = new DIBuilder<int>();
            builder.AddTransient<Branch>(1);
            using var container = builder.Build();

            var a = container.Get<Branch>(1);
            var b = container.Get<Branch>(1);

            Assert.That(a, Is.Not.Null);
            Assert.That(b, Is.Not.Null);
            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a.Leaf, Is.SameAs(b.Leaf));
        }
    }
}
