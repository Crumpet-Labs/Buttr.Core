using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ConfigurableCollectionTests {
        public interface IService { int Value { get; set; } }
        public sealed class Service : IService { public int Value { get; set; } }
        public sealed class Unrelated { }

        [Test]
        public void Register_ReturnsCollectionForChaining() {
            var builder = new ApplicationBuilder();
            var configurable = builder.Resolvers.AddSingleton<IService, Service>();
            var collection = new ConfigurableCollection();

            var chained = collection.Register(configurable);
            Assert.That(chained, Is.SameAs(collection));
        }

        [Test]
        public void WithConfiguration_RegisteredType_FlowsThroughToUnderlyingConfigurable() {
            var builder = new ApplicationBuilder();
            var configurable = builder.Resolvers.AddSingleton<Service>();
            var collection = new ConfigurableCollection();
            collection.Register(configurable);

            var returned = collection.WithConfiguration<Service>(s => { s.Value = 42; return s; });
            Assert.That(returned, Is.SameAs(collection));

            using var app = (IDisposable)builder.Build();
            Assert.That(Application<Service>.Get().Value, Is.EqualTo(42));
        }

        [Test]
        public void WithFactory_RegisteredType_FlowsThroughToUnderlyingConfigurable() {
            var builder = new ApplicationBuilder();
            var configurable = builder.Resolvers.AddSingleton<Service>();
            var collection = new ConfigurableCollection();
            collection.Register(configurable);

            var returned = collection.WithFactory<Service>(() => new Service { Value = 99 });
            Assert.That(returned, Is.SameAs(collection));

            using var app = (IDisposable)builder.Build();
            Assert.That(Application<Service>.Get().Value, Is.EqualTo(99));
        }

        [Test]
        public void WithConfiguration_UnregisteredType_Throws() {
            var collection = new ConfigurableCollection();
            Assert.Throws<ConfigurableException>(
                () => collection.WithConfiguration<Unrelated>(u => u));
        }

        [Test]
        public void WithFactory_UnregisteredType_Throws() {
            var collection = new ConfigurableCollection();
            Assert.Throws<ConfigurableException>(
                () => collection.WithFactory<Unrelated>(() => new Unrelated()));
        }

        [Test]
        public void UnregisteredType_ErrorMessage_ContainsLiteralPlaceholder_KnownQuirk() {
            // ConfigurableCollection's error strings are missing the `$`
            // interpolation prefix, so `{typeof(TConcrete)}` is emitted literally
            // instead of the actual type name. Documented in KNOWN_ISSUES.md.
            var collection = new ConfigurableCollection();
            var ex = Assert.Throws<ConfigurableException>(
                () => collection.WithConfiguration<Unrelated>(u => u));
            Assert.That(ex.Message, Does.Contain("{typeof(TConcrete)}"));

            var ex2 = Assert.Throws<ConfigurableException>(
                () => collection.WithFactory<Unrelated>(() => new Unrelated()));
            Assert.That(ex2.Message, Does.Contain("{typeof(TConcrete)}"));
        }

        [Test]
        public void Register_SameType_Twice_Throws() {
            var builder = new ApplicationBuilder();
            var a = builder.Resolvers.AddSingleton<Service>();
            var b = builder.Resolvers.AddSingleton<Service>();
            var collection = new ConfigurableCollection();
            collection.Register(a);

            Assert.Throws<ArgumentException>(() => collection.Register(b));
        }
    }
}
