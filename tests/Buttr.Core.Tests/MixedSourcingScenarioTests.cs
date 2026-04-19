using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class MixedSourcingScenarioTests {
        public interface IAudio { }
        public sealed class AudioSystem : IAudio { }
        public sealed class SilentAudio : IAudio { }

        public interface IItemCatalog { }
        public sealed class ItemCatalog : IItemCatalog { }

        public sealed class InventoryController {
            public IItemCatalog Catalog;
            public IAudio Audio;
            public InventoryController(IItemCatalog catalog, IAudio audio) {
                Catalog = catalog;
                Audio = audio;
            }
        }

        public sealed class ComplexConsumer {
            public IAudio Audio;
            public IItemCatalog Catalog;
            public ComplexConsumer(IAudio audio, IItemCatalog catalog) {
                Audio = audio;
                Catalog = catalog;
            }
        }

        [TearDown]
        public void ClearScopes() => ScopeRegistry.Clear();

        [Test]
        public void PackageComposition_FeatureScopeConsumesGlobalInterface_Resolves() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<IAudio, AudioSystem>();
            using var appContainer = (IDisposable)app.Build();

            var scope = new ScopeBuilder("inventory");
            scope.AddSingleton<IItemCatalog, ItemCatalog>();
            scope.AddSingleton<InventoryController>();
            using var container = scope.Build();

            var controller = container.Get<InventoryController>();
            Assert.That(controller.Catalog, Is.InstanceOf<ItemCatalog>());
            Assert.That(controller.Audio, Is.InstanceOf<AudioSystem>());
        }

        [Test]
        public void ParameterOrderIndependence_GlobalFirst_LocalSecond() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<IAudio, AudioSystem>();
            using var appContainer = (IDisposable)app.Build();

            var scope = new ScopeBuilder("complex");
            scope.AddSingleton<IItemCatalog, ItemCatalog>();
            scope.AddSingleton<ComplexConsumer>();
            using var container = scope.Build();

            var consumer = container.Get<ComplexConsumer>();
            Assert.That(consumer.Audio, Is.InstanceOf<AudioSystem>());
            Assert.That(consumer.Catalog, Is.InstanceOf<ItemCatalog>());
        }

        [Test]
        public void ScopeOverride_LocalInterfaceImplementationShadowsGlobal() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<IAudio, AudioSystem>();
            app.Resolvers.AddSingleton<IItemCatalog, ItemCatalog>();
            using var appContainer = (IDisposable)app.Build();

            var scope = new ScopeBuilder("test-scope");
            scope.AddSingleton<IAudio, SilentAudio>();
            scope.AddSingleton<InventoryController>();
            using var container = scope.Build();

            var controller = container.Get<InventoryController>();
            Assert.That(controller.Audio, Is.InstanceOf<SilentAudio>());
            Assert.That(controller.Catalog, Is.InstanceOf<ItemCatalog>());
        }

        [Test]
        public void MixedSourcing_DIBuilder_WithGlobalInterface_Resolves() {
            var app = new ApplicationBuilder();
            app.Resolvers.AddSingleton<IAudio, AudioSystem>();
            using var appContainer = (IDisposable)app.Build();

            var builder = new DIBuilder();
            builder.AddSingleton<IItemCatalog, ItemCatalog>();
            builder.AddSingleton<InventoryController>();
            using var container = (IDisposable)builder.Build();

            var controller = ((IDIContainer)container).Get<InventoryController>();
            Assert.That(controller.Catalog, Is.InstanceOf<ItemCatalog>());
            Assert.That(controller.Audio, Is.InstanceOf<AudioSystem>());
        }
    }
}
