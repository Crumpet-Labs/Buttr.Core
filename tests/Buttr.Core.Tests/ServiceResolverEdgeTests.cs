using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // Exercises `ServiceResolverUtilities.TryResolve` edge cases:
    //
    //   • all deps found in the container's own registry (fast path)
    //   • mixed sourcing — some deps in container, some in ApplicationRegistry
    //   • deps missing entirely (throws)
    //   • an out-of-order constructor (reorder path)
    //
    // The reorder path has a known bug where interface-keyed registrations
    // mismatch by concrete type — pinned by
    // MixedSourcing_WithAbstractKey_KnownBug_ThrowsMissingDependency.

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
            // LeafA lives in the DIBuilder's own registry. LeafB is only in
            // ApplicationRegistry. NeedsAB must be assembled from both.
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
            // A and C in container, B in ApplicationRegistry — requires the
            // reorder step to match constructor-parameter order (A, B, C).
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
        public void MixedSourcing_WithAbstractKey_KnownBug_ThrowsMissingDependency() {
            // Current behaviour: the reorder step in ServiceResolverUtilities.TryResolve
            // uses strict type equality `combined[j]?.GetType() == required` to match
            // resolved instances back to constructor-parameter slots. When the slot
            // type is an interface (ILeaf) but the resolved instance's concrete type
            // is InterfaceLeaf, the equality check fails and the reorder throws
            // "Missing dependency of type ILeaf" — even though ILeaf is registered.
            //
            // This only bites when sourcing is mixed (some deps in container, some
            // in ApplicationRegistry), because the all-internal fast path uses a
            // plain Array.Copy and doesn't reorder.
            //
            // Documented in KNOWN_ISSUES.md.
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<LeafB>();
            using var appContainer = (IDisposable)app.Build();

            var builder = new DIBuilder();
            builder.AddSingleton<ILeaf, InterfaceLeaf>();
            builder.AddSingleton<NeedsInterface>();
            using var container = (IDisposable)builder.Build();

            var ex = Assert.Throws<ObjectResolverException>(
                () => _ = ((IDIContainer)container).Get<NeedsInterface>());
            Assert.That(ex.Message, Does.Contain("ILeaf").Or.Contain("dependency"));
        }

        [Test]
        public void InterfaceKeyedContainerDep_AllInContainer_FastPath_Resolves() {
            // Sanity check: when ALL deps are in the container (fast path), the
            // known bug above is avoided because no reorder happens.
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
