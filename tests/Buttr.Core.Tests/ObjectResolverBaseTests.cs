using System;
using System.Collections.Generic;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // ObjectResolverBase<TConcrete> is internal and abstract; it's the base for
    // every singleton/transient resolver. Its construction-time work — scanning
    // for the first public ctor, capturing `requirements`, and compiling the
    // expression-tree factory — runs via any concrete resolver. These tests
    // exercise that behaviour through the public Application-builder surface.

    public class ObjectResolverBaseTests {
        public sealed class OnlyPrivateCtor {
            private OnlyPrivateCtor() { }
        }

        public sealed class MultipleCtors {
            public string Picked;
            public MultipleCtors() { Picked = "noargs"; }
            public MultipleCtors(string _) { Picked = "string"; }
        }

        private sealed class CaptureLogger : IButtrLogger {
            public readonly List<string> Logs = new();
            public void Log(string message) => Logs.Add(message);
            public void LogWarning(string message) { }
            public void LogError(string message) { }
        }

        [TearDown]
        public void ResetLogger() => ButtrLog.SetLogger(null);

        [Test]
        public void Registration_OfTypeWithNoPublicCtor_Throws_AtBuild() {
            // Building constructs the resolver, which walks typeof(TConcrete).GetConstructors()
            // (public only) and throws InvalidOperationException if none found.
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<OnlyPrivateCtor>();

            Assert.Throws<InvalidOperationException>(() => {
                using var app = builder.Build();
            });
        }

        [Test]
        public void Registration_PicksFirstPublicConstructor() {
            // Observable: MultipleCtors has a parameterless ctor first. Expected
            // behaviour (current): resolver picks ctor #0 — the parameterless one.
            // The string-arg ctor is never invoked.
            var builder = new ApplicationBuilder();
            builder.Resolvers.AddSingleton<MultipleCtors>();
            using var app = builder.Build();

            var resolved = Application<MultipleCtors>.Get();
            Assert.That(resolved.Picked, Is.EqualTo("noargs"));
        }
    }
}
