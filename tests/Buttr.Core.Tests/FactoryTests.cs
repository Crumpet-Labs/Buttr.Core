using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // Factory.Empty<T>() and ConfigurationFactory.Empty<T>() are internal, so we
    // test their behaviour indirectly through the resolvers that consume them.
    //
    // - Factory.Empty<T>() returns null — no factory override applied. Observable
    //   effect: the default expression-tree construction path runs.
    // - ConfigurationFactory.Empty<T>() returns identity. Observable effect:
    //   the resolved instance is returned verbatim, unchanged.
    //
    // ObjectFactory.Create<T>(ctor, paramTypes) is internal too and exhaustively
    // exercised by every resolver test in this suite. No extra direct coverage.

    public class FactoryDefaultsTests {
        public sealed class NoArgs {
            public int Touched = 0;
        }
        public sealed class Leaf { }
        public sealed class Branch {
            public Leaf Dep;
            public Branch(Leaf dep) { Dep = dep; }
        }

        [Test]
        public void DefaultConfiguration_IsIdentity_ReturnsUntouchedInstance() {
            // If ConfigurationFactory.Empty<T> isn't the identity, the
            // "Touched" field would surface any mutation.
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<NoArgs>();
            using var app = builder.Build();

            var resolved = Application<NoArgs>.Get();
            Assert.That(resolved.Touched, Is.EqualTo(0));
        }

        [Test]
        public void DefaultFactory_IsNull_TriggersExpressionTreeConstruction() {
            // If Factory.Empty<T> wasn't null, it would short-circuit the
            // expression-tree factory and Branch's Dep would never be populated.
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Leaf>();
            builder.Resolvers.AddSingleton<Branch>();
            using var app = builder.Build();

            var branch = Application<Branch>.Get();
            Assert.That(branch.Dep, Is.Not.Null);
        }
    }
}
