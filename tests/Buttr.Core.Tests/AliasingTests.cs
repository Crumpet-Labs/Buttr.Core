using System;
using System.Linq;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class AliasingSingletonTests {
        public interface IFoo { }
        public interface IBar { }
        public interface IBaz { }
        public sealed class Foo : IFoo, IBar, IBaz { }

        [Test]
        public void Singleton_AliasedAbstract_GetByAliasReturnsSameInstance() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<IFoo, Foo>().As<IBar>();
            using var container = builder.Build();

            var viaPrimary = container.Get<IFoo>();
            var viaAlias = container.Get<IBar>();

            Assert.That(viaPrimary, Is.Not.Null);
            Assert.That(viaAlias, Is.SameAs(viaPrimary));
        }

        [Test]
        public void Singleton_ChainedAliases_AllKeysResolveToSameInstance() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<IFoo, Foo>().As<IBar>().As<IBaz>();
            using var container = builder.Build();

            var a = container.Get<IFoo>();
            var b = container.Get<IBar>();
            var c = container.Get<IBaz>();

            Assert.That(a, Is.SameAs(b));
            Assert.That(b, Is.SameAs(c));
        }

        [Test]
        public void Singleton_AliasOnConcreteRegistration_ResolvesViaAlias() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>().As<IFoo>();
            using var container = builder.Build();

            var concrete = container.Get<Foo>();
            var viaAlias = container.Get<IFoo>();

            Assert.That(viaAlias, Is.SameAs(concrete));
        }
    }

    public class AliasingTransientTests {
        public interface IWidget { }
        public interface INamed { }
        public sealed class Widget : IWidget, INamed { }

        [Test]
        public void Transient_AliasedAbstract_EachGetYieldsFreshInstance() {
            var builder = new DIBuilder();
            builder.Resolvers.AddTransient<IWidget, Widget>().As<INamed>();
            using var container = builder.Build();

            var a = container.Get<IWidget>();
            var b = container.Get<IWidget>();
            var c = container.Get<INamed>();

            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a, Is.Not.SameAs(c));
            Assert.That(a, Is.InstanceOf<Widget>());
            Assert.That(c, Is.InstanceOf<Widget>());
        }
    }

    public class AliasingAllTests {
        public interface IHandler { string Tag { get; } }
        public sealed class AlphaHandler : IHandler { public string Tag => "alpha"; }
        public sealed class BetaHandler : IHandler { public string Tag => "beta"; }
        public sealed class UnrelatedService { }

        [Test]
        public void All_ReturnsAllConcreteImplementationsAssignableToT() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<AlphaHandler>();
            builder.Resolvers.AddSingleton<BetaHandler>();
            builder.Resolvers.AddSingleton<UnrelatedService>();
            using var container = builder.Build();

            var handlers = container.All<IHandler>().ToList();

            Assert.That(handlers, Has.Count.EqualTo(2));
            var tags = handlers.Select(h => h.Tag).OrderBy(t => t).ToList();
            Assert.That(tags, Is.EqualTo(new[] { "alpha", "beta" }));
        }

        [Test]
        public void All_WithAliasedSingleRegistration_YieldsSingleInstanceNotPerAlias() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<AlphaHandler>().As<IHandler>();
            using var container = builder.Build();

            var handlers = container.All<IHandler>().ToList();

            Assert.That(handlers, Has.Count.EqualTo(1));
        }

        [Test]
        public void All_NoMatchingRegistrations_YieldsEmpty() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<UnrelatedService>();
            using var container = builder.Build();

            var handlers = container.All<IHandler>().ToList();
            Assert.That(handlers, Is.Empty);
        }
    }

    public class AliasingDuplicateTests {
        public interface IThing { }
        public sealed class ThingA : IThing { }
        public sealed class ThingB : IThing { }

        [Test]
        public void DuplicateAlias_AcrossRegistrations_Throws() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<ThingA>().As<IThing>();

            Assert.Throws<DuplicateAliasException>(
                () => builder.Resolvers.AddSingleton<ThingB>().As<IThing>());
        }

        [Test]
        public void AliasMatchingExistingPrimaryKey_Throws() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<IThing, ThingA>();

            Assert.Throws<DuplicateAliasException>(
                () => builder.Resolvers.AddSingleton<ThingB>().As<IThing>());
        }
    }

    public class AliasingAssignabilityTests {
        public sealed class Unrelated { }
        public sealed class Concrete { }

        [Test]
        public void Alias_NotSupertypeOfConcrete_Throws() {
            var builder = new DIBuilder();

            Assert.Throws<ObjectResolverException>(
                () => builder.Resolvers.AddSingleton<Concrete>().As<Unrelated>());
        }
    }

    public class AliasingHiddenTests {
        public interface ISecret { }
        public interface ISecretAlias { }
        public sealed class Secret : ISecret, ISecretAlias { }
        public sealed class Consumer {
            public ISecret Secret;
            public Consumer(ISecret secret) { Secret = secret; }
        }

        [Test]
        public void HiddenAlias_GetByAlias_Throws() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<ISecret, Secret>().As<ISecretAlias>();
            using var container = builder.Build();

            Assert.Throws<ObjectResolverException>(
                () => _ = container.Get<ISecretAlias>());
        }

        [Test]
        public void HiddenAlias_ExcludedFromAll() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<ISecret, Secret>().As<ISecretAlias>();
            using var container = builder.Build();

            var all = container.All<ISecret>().ToList();
            Assert.That(all, Is.Empty);
        }

        [Test]
        public void HiddenAliased_StillResolvesAsConstructorDependency() {
            var builder = new DIBuilder();
            builder.Hidden.AddSingleton<ISecret, Secret>().As<ISecretAlias>();
            builder.AddSingleton<Consumer>();
            using var container = builder.Build();

            var consumer = container.Get<Consumer>();
            Assert.That(consumer.Secret, Is.Not.Null);
            Assert.That(consumer.Secret, Is.InstanceOf<Secret>());
        }
    }

    public class AliasingScopeTests {
        public interface IScoped { }
        public interface IAlsoScoped { }
        public sealed class Scoped : IScoped, IAlsoScoped { }
        public sealed class Scoped2 : IScoped { }

        [Test]
        public void Scope_AliasedSingleton_SharesInstance() {
            var scope = new ScopeBuilder("aliasing-scope-sharing");
            scope.Resolvers.AddSingleton<IScoped, Scoped>().As<IAlsoScoped>();
            using var container = scope.Build();

            var a = container.Get<IScoped>();
            var b = container.Get<IAlsoScoped>();

            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void Scope_All_ReturnsEveryAssignable() {
            var scope = new ScopeBuilder("aliasing-scope-all");
            scope.Resolvers.AddSingleton<Scoped>();
            scope.Resolvers.AddSingleton<Scoped2>();
            using var container = scope.Build();

            var all = container.All<IScoped>().ToList();
            Assert.That(all, Has.Count.EqualTo(2));
        }
    }

    public class AliasingApplicationTests {
        public interface IAppService { }
        public interface IAppFacade { }
        public sealed class AppService : IAppService, IAppFacade { }
        public sealed class AnotherAppService : IAppService { }

        [Test]
        public void Application_Alias_ResolvesViaApplicationT() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<IAppService, AppService>().As<IAppFacade>();
            using var app = builder.Build();

            var viaPrimary = Application<IAppService>.Get();
            var viaAlias = Application<IAppFacade>.Get();

            Assert.That(viaAlias, Is.SameAs(viaPrimary));
        }

        [Test]
        public void Application_All_ReturnsEveryAssignable() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<AppService>();
            builder.Resolvers.AddSingleton<AnotherAppService>();
            using var app = builder.Build();

            var all = Application.All<IAppService>().ToList();
            Assert.That(all, Has.Count.EqualTo(2));
        }

        [Test]
        public void Application_All_ExcludesHidden() {
            var builder = new ApplicationBuilder();
            builder.Hidden.AddSingleton<IAppService, AppService>();
            using var app = builder.Build();

            var all = Application.All<IAppService>().ToList();
            Assert.That(all, Is.Empty);
        }
    }

    public class AliasingConfigurableCollectionTests {
        public interface IPackageService { }
        public interface IPackageFacade { }
        public sealed class PackageService : IPackageService, IPackageFacade { }

        [Test]
        public void ConfigurableCollection_As_DelegatesToRegistration() {
            var builder = new DIBuilder();
            var cfg = builder.Resolvers.AddSingleton<IPackageService, PackageService>();

            var collection = new ConfigurableCollection()
                .Register(cfg);

            collection.As<PackageService, IPackageFacade>();

            using var container = builder.Build();

            var viaPrimary = container.Get<IPackageService>();
            var viaAlias = container.Get<IPackageFacade>();

            Assert.That(viaAlias, Is.SameAs(viaPrimary));
        }

        [Test]
        public void ConfigurableCollection_As_MissingType_Throws() {
            var collection = new ConfigurableCollection();

            Assert.Throws<ConfigurableException>(
                () => collection.As<PackageService, IPackageFacade>());
        }
    }

    public class AliasingIDBuilderTests {
        public sealed class Widget { }

        [Test]
        public void IDBuilder_AsCall_Throws() {
            var builder = new DIBuilder<string>();
            var cfg = builder.AddSingleton<Widget>("w");

            Assert.Throws<NotSupportedException>(() => cfg.As<object>());
        }
    }
}
