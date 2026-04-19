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
            using var app = builder.Build();

            var branch = Application<Branch>.Get();

            Assert.That(branch, Is.Not.Null);
            Assert.That(branch.Leaf, Is.Not.Null);
        }

        [Test]
        public void MissingDependency_ErrorPath_FormatsTypeNamesViaStringJoin() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Middle>();
            builder.Resolvers.AddSingleton<Top>();

            using var app = builder.Build();

            var ex = Assert.Throws<ObjectResolverException>(() => _ = Application<Top>.Get());
            Assert.That(ex.Message, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void NestedDependencyFailure_PreservesInnerExceptionAndListsFoundVsUnresolved() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Middle>();
            builder.Resolvers.AddSingleton<Top>();

            using var app = builder.Build();

            var ex = Assert.Throws<ObjectResolverException>(() => _ = Application<Top>.Get());

            Assert.That(ex.InnerException, Is.Not.Null);
            Assert.That(ex.InnerException, Is.InstanceOf<ObjectResolverException>());

            Assert.That(ex.Message, Does.Contain("Top"));
            Assert.That(ex.Message, Does.Contain("Found:"));
            Assert.That(ex.Message, Does.Contain("Unresolved:"));
            Assert.That(ex.Message, Does.Contain("Middle"));
        }
    }
}
