using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class FactoryOnlyTypeTests {
        public sealed class PrivateCtorOnly {
            public int Value;

            private PrivateCtorOnly(int seed) { Value = seed; }

            public static PrivateCtorOnly Create(int seed) => new PrivateCtorOnly(seed);
        }

        public interface IGate { int V { get; set; } }
        public sealed class Gate : IGate {
            public int V { get; set; }
            private Gate() { }
            public static Gate Make(int seed) => new Gate { V = seed };
        }

        [TearDown]
        public void ClearScopes() => ScopeRegistry.Clear();

        [Test]
        public void ApplicationBuilder_Singleton_WithFactory_OnPrivateCtorType_Resolves() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<PrivateCtorOnly>()
                .WithFactory(() => PrivateCtorOnly.Create(77));
            using var app = builder.Build();

            Assert.That(Application<PrivateCtorOnly>.Get().Value, Is.EqualTo(77));
        }

        [Test]
        public void ApplicationBuilder_Transient_WithFactory_OnPrivateCtorType_Resolves() {
            var seed = 0;
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddTransient<PrivateCtorOnly>()
                .WithFactory(() => PrivateCtorOnly.Create(++seed));
            using var app = builder.Build();

            Assert.That(Application<PrivateCtorOnly>.Get().Value, Is.EqualTo(1));
            Assert.That(Application<PrivateCtorOnly>.Get().Value, Is.EqualTo(2));
        }

        [Test]
        public void ApplicationBuilder_Pair_WithFactory_OnPrivateCtorType_Resolves() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<IGate, Gate>()
                .WithFactory(() => Gate.Make(42));
            using var app = builder.Build();

            Assert.That(Application<IGate>.Get().V, Is.EqualTo(42));
        }

        [Test]
        public void DIBuilder_Singleton_WithFactory_OnPrivateCtorType_Resolves() {
            var builder = new DIBuilder();
            builder.AddSingleton<PrivateCtorOnly>()
                .WithFactory(() => PrivateCtorOnly.Create(5));
            using var container = builder.Build();

            Assert.That(container.Get<PrivateCtorOnly>().Value, Is.EqualTo(5));
        }

        [Test]
        public void ScopeBuilder_Transient_WithFactory_OnPrivateCtorType_Resolves() {
            var builder = new ScopeBuilder("private-ctor-scope");
            builder.AddTransient<PrivateCtorOnly>()
                .WithFactory(() => PrivateCtorOnly.Create(9));
            using var container = builder.Build();

            Assert.That(container.Get<PrivateCtorOnly>().Value, Is.EqualTo(9));
        }

        [Test]
        public void DIBuilderID_Singleton_WithFactory_OnPrivateCtorType_Resolves() {
            var builder = new DIBuilder<int>();
            builder.AddSingleton<PrivateCtorOnly>(1)
                .WithFactory(() => PrivateCtorOnly.Create(100));
            using var container = builder.Build();

            Assert.That(container.Get<PrivateCtorOnly>(1).Value, Is.EqualTo(100));
        }

        [Test]
        public void WithoutFactory_PrivateCtorTypeStillThrows() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<PrivateCtorOnly>();

            Assert.Throws<InvalidOperationException>(() => {
                using var app = builder.Build();
            });
        }
    }
}
