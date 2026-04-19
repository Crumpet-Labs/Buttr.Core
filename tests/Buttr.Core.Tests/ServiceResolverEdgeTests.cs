using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ServiceResolverEdgeTests {
        public sealed class LeafA { }
        public sealed class LeafB { }
        public sealed class LeafC { }

        public sealed class NeedsAB {
            public LeafA A;
            public LeafB B;
            public NeedsAB(LeafA a, LeafB b) { A = a; B = b; }
        }

        public sealed class NeedsABC {
            public LeafA A;
            public LeafB B;
            public LeafC C;
            public NeedsABC(LeafA a, LeafB b, LeafC c) { A = a; B = b; C = c; }
        }

        public interface ILeaf { }
        public sealed class InterfaceLeaf : ILeaf { }
        public sealed class NeedsInterface {
            public ILeaf I;
            public LeafB B;
            public NeedsInterface(ILeaf i, LeafB b) { I = i; B = b; }
        }

        [Test]
        public void AllDepsInContainer_FastPath_ResolvesAllConstructorArgs() {
            var builder = new DIBuilder();
            builder.AddSingleton<LeafA>();
            builder.AddSingleton<LeafB>();
            builder.AddSingleton<NeedsAB>();
            using var container = (IDisposable)builder.Build();

            var needs = ((IDIContainer)container).Get<NeedsAB>();
            Assert.That(needs.A, Is.Not.Null);
            Assert.That(needs.B, Is.Not.Null);
        }

        [Test]
        public void MixedSourcing_SomeDepsInContainer_SomeInApplicationRegistry_Resolves() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<LeafB>();
            using var appContainer = (IDisposable)app.Build();

            var builder = new DIBuilder();
            builder.AddSingleton<LeafA>();
            builder.AddSingleton<NeedsAB>();
            using var container = (IDisposable)builder.Build();

            var needs = ((IDIContainer)container).Get<NeedsAB>();
            Assert.That(needs.A, Is.Not.Null);
            Assert.That(needs.B, Is.Not.Null);
        }

        [Test]
        public void MixedSourcing_ThreeWayInterleaved_Resolves() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<LeafB>();
            using var appContainer = (IDisposable)app.Build();

            var builder = new DIBuilder();
            builder.AddSingleton<LeafA>();
            builder.AddSingleton<LeafC>();
            builder.AddSingleton<NeedsABC>();
            using var container = (IDisposable)builder.Build();

            var needs = ((IDIContainer)container).Get<NeedsABC>();
            Assert.That(needs.A, Is.InstanceOf<LeafA>());
            Assert.That(needs.B, Is.InstanceOf<LeafB>());
            Assert.That(needs.C, Is.InstanceOf<LeafC>());
        }

        [Test]
        public void AllDepsMissing_Throws_ObjectResolverException() {
            var builder = new DIBuilder();
            builder.AddSingleton<NeedsAB>();
            using var container = (IDisposable)builder.Build();

            Assert.Throws<ObjectResolverException>(
                () => _ = ((IDIContainer)container).Get<NeedsAB>());
        }

        [Test]
        public void MixedSourcing_WithAbstractKey_ResolvesCorrectly() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<LeafB>();
            using var appContainer = (IDisposable)app.Build();

            var builder = new DIBuilder();
            builder.AddSingleton<ILeaf, InterfaceLeaf>();
            builder.AddSingleton<NeedsInterface>();
            using var container = (IDisposable)builder.Build();

            var needs = ((IDIContainer)container).Get<NeedsInterface>();
            Assert.That(needs.I, Is.InstanceOf<InterfaceLeaf>());
            Assert.That(needs.B, Is.InstanceOf<LeafB>());
        }

        [Test]
        public void InterfaceKeyedContainerDep_AllInContainer_FastPath_Resolves() {
            var builder = new DIBuilder();
            builder.AddSingleton<ILeaf, InterfaceLeaf>();
            builder.AddSingleton<LeafB>();
            builder.AddSingleton<NeedsInterface>();
            using var container = (IDisposable)builder.Build();

            var needs = ((IDIContainer)container).Get<NeedsInterface>();
            Assert.That(needs.I, Is.InstanceOf<InterfaceLeaf>());
            Assert.That(needs.B, Is.InstanceOf<LeafB>());
        }
    }
}
