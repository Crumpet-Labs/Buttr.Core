using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ContainerTests {
        public interface IThing { }
        public sealed class Thing : IThing { }
        public sealed class DisposableThing : IDisposable {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        [Test]
        public void DIBuilder_AddSingleton_ReturnsSameInstance() {
            var builder = new DIBuilder();
            builder.AddSingleton<IThing, Thing>();
            using var container = (IDisposable)builder.Build();

            var a = ((IDIContainer)container).Get<IThing>();
            var b = ((IDIContainer)container).Get<IThing>();

            Assert.That(a, Is.Not.Null);
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void DIBuilder_AddTransient_ReturnsNewInstanceEachResolve() {
            var builder = new DIBuilder();
            builder.AddTransient<IThing, Thing>();
            using var container = (IDisposable)builder.Build();

            var a = ((IDIContainer)container).Get<IThing>();
            var b = ((IDIContainer)container).Get<IThing>();

            Assert.That(a, Is.Not.Null);
            Assert.That(b, Is.Not.Null);
            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void DIBuilder_Dispose_DisposesRegisteredDisposables() {
            var builder = new DIBuilder();
            builder.AddSingleton<DisposableThing>();
            var container = builder.Build();
            var thing = container.Get<DisposableThing>();

            ((IDisposable)container).Dispose();

            Assert.That(thing.Disposed, Is.True);
        }
    }
}
