using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ApplicationStaticGetAfterBuildTests {
        public sealed class GetTarget { public int Stamp = 123; }

        [Test]
        public void Get_ReturnsResolvedInstance_AfterBuild() {
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<GetTarget>();
            using var app = builder.Build();

            var got = Application<GetTarget>.Get();
            Assert.That(got, Is.Not.Null);
            Assert.That(got.Stamp, Is.EqualTo(123));
        }
    }

    public class ApplicationStaticDisposeTests {
        public sealed class DisposeTarget { }

        [Test]
        public void Get_AfterContainerDisposed_Throws() {
            // StaticSingletonResolver.Dispose() calls Application<T>.Set(null) —
            // so post-dispose Get() dereferences a null resolver.
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<DisposeTarget>();
            var app = builder.Build();

            Assert.That(Application<DisposeTarget>.Get(), Is.Not.Null);

            app.Dispose();

            Assert.Throws<NullReferenceException>(() => _ = Application<DisposeTarget>.Get());
        }
    }

    public class ApplicationStaticNeverSetTests {
        // Uniquely-named type: never referenced anywhere else so its static slot
        // stays untouched across the whole test run.
        public sealed class NeverRegistered { }

        [Test]
        public void Get_BeforeAnySetCall_Throws() {
            // s_Resolver default is null — first Get() NREs. Documents the
            // invariant: callers must Build() before Get().
            Assert.Throws<NullReferenceException>(() => _ = Application<NeverRegistered>.Get());
        }
    }
}
