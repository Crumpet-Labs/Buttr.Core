using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ApplicationBuilderTests {
        public sealed class Service { }

        [Test]
        public void ApplicationBuilder_Build_MakesTypeReachableViaApplicationGet() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Service>();
            using var app = builder.Build();

            var resolved = Application<Service>.Get();

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved, Is.InstanceOf<Service>());
        }

        [Test]
        public void ApplicationBuilder_SingletonThroughApplication_IsSameInstanceAcrossGets() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<Service>();
            using var app = builder.Build();

            var a = Application<Service>.Get();
            var b = Application<Service>.Get();

            Assert.That(a, Is.SameAs(b));
        }
    }
}
