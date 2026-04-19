using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class DisposalSemanticsTests {
        public sealed class Tracked : IDisposable {
            public int Count;
            public void Dispose() => Count++;
        }

        [TearDown]
        public void ClearScopes() => ScopeRegistry.Clear();

        [Test]
        public void DIContainer_Dispose_DisposesResolvedSingleton() {
            var builder = new DIBuilder();
            builder.AddSingleton<Tracked>();
            var container = builder.Build();
            var instance = container.Get<Tracked>();

            ((IDisposable)container).Dispose();

            Assert.That(instance.Count, Is.EqualTo(1));
        }

        [Test]
        public void DIContainer_Dispose_SkipsSingletonsThatWereNeverResolved() {
            var builder = new DIBuilder();
            builder.AddSingleton<Tracked>();
            var container = builder.Build();

            ((IDisposable)container).Dispose();

            Assert.Pass("No throw; unresolved singleton not force-resolved during dispose.");
        }

        [Test]
        public void DIContainer_Dispose_DoesNotDisposeTransientHandouts() {
            var builder = new DIBuilder();
            builder.AddTransient<Tracked>();
            var container = builder.Build();

            var a = container.Get<Tracked>();
            var b = container.Get<Tracked>();

            ((IDisposable)container).Dispose();

            Assert.That(a.Count, Is.EqualTo(0));
            Assert.That(b.Count, Is.EqualTo(0));
        }

        [Test]
        public void ScopeContainer_Dispose_DisposesResolvedSingleton() {
            var builder = new ScopeBuilder("dispose-scope-singleton");
            builder.AddSingleton<Tracked>();
            var container = builder.Build();
            var instance = container.Get<Tracked>();

            container.Dispose();

            Assert.That(instance.Count, Is.EqualTo(1));
        }

        [Test]
        public void ScopeContainer_Dispose_DoesNotDisposeTransientHandouts() {
            var builder = new ScopeBuilder("dispose-scope-transient");
            builder.AddTransient<Tracked>();
            var container = builder.Build();
            var a = container.Get<Tracked>();

            container.Dispose();

            Assert.That(a.Count, Is.EqualTo(0));
        }

        [Test]
        public void IDContainer_Dispose_DisposesResolvedSingleton() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<Tracked>(1);
            var container = builder.Build();
            var instance = container.Get<Tracked>(1);

            container.Dispose();

            Assert.That(instance.Count, Is.EqualTo(1));
        }

        [Test]
        public void IDContainer_Dispose_DoesNotDisposeTransientHandouts() {
            var builder = new DIBuilder<int>();
            builder.AddTransient<Tracked>(1);
            var container = builder.Build();
            var a = container.Get<Tracked>(1);

            container.Dispose();

            Assert.That(a.Count, Is.EqualTo(0));
        }
    }
}
