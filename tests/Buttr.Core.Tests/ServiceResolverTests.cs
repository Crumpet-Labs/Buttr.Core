using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ServiceResolverTests {
        public sealed class Leaf { }
        public sealed class Branch {
            public Leaf Leaf;
            public Branch(Leaf leaf) { Leaf = leaf; }
        }

        // A chain where the deepest dependency is unregistered. Resolving Top triggers
        // the error-formatting path in StaticSingleton.Resolve() (previously used
        // JsonUtility.ToJson; now uses string.Join).
        public sealed class Missing { }
        public sealed class Middle {
            public Middle(Missing m) { }
        }
        public sealed class Top {
            public Top(Middle m) { }
        }

        [Test]
        public void NestedDependencies_ResolveThroughApplicationRegistry() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Leaf>();
            builder.Resolvers.AddSingleton<Branch>();
            using var app = (IDisposable)builder.Build();

            var branch = Application<Branch>.Get();

            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Leaf, Is.Not.Null);
        }

        [Test]
        public void MissingDependency_ErrorPath_FormatsTypeNamesViaStringJoin() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Middle>();
            builder.Resolvers.AddSingleton<Top>();
            // Missing is intentionally NOT registered.

            using var app = (IDisposable)builder.Build();

            // Resolution is lazy — forcing Get() on Top drives the
            // StaticSingleton.Resolve() → error path (previously JsonUtility.ToJson,
            // now string.Join).
            var ex = Assert.Throws<ObjectResolverException>(() => _ = Application<Top>.Get());

            Assert.That(ex.Message, Is.Not.Null.And.Not.Empty);
        }
    }
}
