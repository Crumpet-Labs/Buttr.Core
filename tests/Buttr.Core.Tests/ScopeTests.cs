using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // ── ScopeBuilder ─────────────────────────────────────────────────
    public class ScopeBuilderTests {
        public interface IService { }
        public sealed class Service : IService { }

        [TearDown]
        public void ClearScopeRegistry() => ScopeRegistry.Clear();

        [Test]
        public void Build_RegistersScopeUnderTheGivenKey() {
            var builder = new ScopeBuilder("scope-a");
            builder.AddSingleton<Service>();

            using var container = builder.Build();

            Assert.That(ScopeRegistry.Get("scope-a"), Is.SameAs(container));
        }

        [Test]
        public void AddSingleton_YieldsSameInstanceOnEveryResolve() {
            var builder = new ScopeBuilder("scope-singleton");
            builder.AddSingleton<IService, Service>();

            using var container = builder.Build();

            var a = container.Get<IService>();
            var b = container.Get<IService>();
            Assert.That(a, Is.Not.Null);
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void AddTransient_YieldsNewInstanceOnEveryResolve() {
            var builder = new ScopeBuilder("scope-transient");
            builder.AddTransient<IService, Service>();

            using var container = builder.Build();

            var a = container.Get<IService>();
            var b = container.Get<IService>();
            Assert.That(a, Is.Not.Null);
            Assert.That(b, Is.Not.Null);
            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void DuplicateScopeKey_Throws_WhenSecondScopeIsBuilt() {
            var builderA = new ScopeBuilder("scope-dup");
            builderA.AddSingleton<Service>();
            using var containerA = builderA.Build();

            var builderB = new ScopeBuilder("scope-dup");
            builderB.AddSingleton<Service>();

            Assert.Throws<ArgumentException>(() => {
                using var containerB = builderB.Build();
            });
        }

        [Test]
        public void TwoScopesWithDifferentKeys_HaveIndependentSingletons() {
            var a = new ScopeBuilder("scope-x");
            a.AddSingleton<Service>();
            using var containerA = a.Build();

            var b = new ScopeBuilder("scope-y");
            b.AddSingleton<Service>();
            using var containerB = b.Build();

            var instanceA = containerA.Get<Service>();
            var instanceB = containerB.Get<Service>();

            Assert.That(instanceA, Is.Not.Null);
            Assert.That(instanceB, Is.Not.Null);
            Assert.That(instanceA, Is.Not.SameAs(instanceB));
        }
    }

    // ── ScopeContainer ───────────────────────────────────────────────
    public class ScopeContainerTests {
        public interface IService { }
        public sealed class Service : IService { }
        public sealed class Unregistered { }
        public sealed class HiddenThing { }
        public sealed class DisposableThing : IDisposable {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        [TearDown]
        public void ClearScopeRegistry() => ScopeRegistry.Clear();

        [Test]
        public void Get_ReturnsDefault_WhenTypeNotRegistered() {
            var builder = new ScopeBuilder("scope-default");
            using var container = builder.Build();

            Assert.That(container.Get<Unregistered>(), Is.Null);
        }

        [Test]
        public void TryGet_ReturnsFalseAndDefault_WhenTypeNotRegistered() {
            var builder = new ScopeBuilder("scope-tryget-miss");
            using var container = builder.Build();

            var found = container.TryGet<Unregistered>(out var value);
            Assert.That(found, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void TryGet_ReturnsTrueAndInstance_WhenTypeRegistered() {
            var builder = new ScopeBuilder("scope-tryget-hit");
            builder.AddSingleton<IService, Service>();
            using var container = builder.Build();

            var found = container.TryGet<IService>(out var value);
            Assert.That(found, Is.True);
            Assert.That(value, Is.Not.Null);
            Assert.That(value, Is.InstanceOf<Service>());
        }

        [Test]
        public void Get_Throws_WhenTypeIsHidden() {
            var builder = new ScopeBuilder("scope-hidden-get");
            builder.Hidden.AddSingleton<HiddenThing>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => _ = container.Get<HiddenThing>());
        }

        [Test]
        public void TryGet_Throws_WhenTypeIsHidden() {
            var builder = new ScopeBuilder("scope-hidden-tryget");
            builder.Hidden.AddSingleton<HiddenThing>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(() => container.TryGet<HiddenThing>(out _));
        }

        [Test]
        public void Dispose_RemovesScopeFromRegistry() {
            var builder = new ScopeBuilder("scope-dispose-registry");
            builder.AddSingleton<Service>();
            var container = builder.Build();

            Assert.DoesNotThrow(() => _ = ScopeRegistry.Get("scope-dispose-registry"));

            container.Dispose();

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => _ = ScopeRegistry.Get("scope-dispose-registry"));
        }

        [Test]
        public void Dispose_DisposesResolvedDisposableSingletons() {
            var builder = new ScopeBuilder("scope-dispose-inner");
            builder.AddSingleton<DisposableThing>();
            var container = builder.Build();
            var thing = container.Get<DisposableThing>();

            container.Dispose();

            Assert.That(thing.Disposed, Is.True);
        }
    }

    // ── ScopeRegistry ────────────────────────────────────────────────
    public class ScopeRegistryTests {
        public sealed class Service { }

        [TearDown]
        public void ClearScopeRegistry() => ScopeRegistry.Clear();

        [Test]
        public void Get_Throws_WhenKeyMissing() {
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => _ = ScopeRegistry.Get("nope"));
        }

        [Test]
        public void RemoveScope_MakesSubsequentGetThrow() {
            var builder = new ScopeBuilder("scope-removed");
            builder.AddSingleton<Service>();
            using var container = builder.Build();

            ScopeRegistry.RemoveScope("scope-removed");

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => _ = ScopeRegistry.Get("scope-removed"));
        }

        [Test]
        public void Clear_RemovesAllRegisteredScopes() {
            var builderA = new ScopeBuilder("clear-a");
            builderA.AddSingleton<Service>();
            using var containerA = builderA.Build();

            var builderB = new ScopeBuilder("clear-b");
            builderB.AddSingleton<Service>();
            using var containerB = builderB.Build();

            ScopeRegistry.Clear();

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => _ = ScopeRegistry.Get("clear-a"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => _ = ScopeRegistry.Get("clear-b"));
        }
    }
}
