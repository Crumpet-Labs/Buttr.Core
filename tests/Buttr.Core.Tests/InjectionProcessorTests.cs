using System;
using System.Collections.Generic;
using Buttr.Core;
using Buttr.Injection;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class InjectionProcessorRegisterTests {
        public sealed class Target : IInjectable {
            public bool Injected { get; set; }
            public string Marker;
        }

        [TearDown]
        public void ResetInjector() => InjectionProcessor.Clear();

        [Test]
        public void Register_NullInjector_Throws() {
            Assert.Throws<InjectionException>(
                () => InjectionProcessor.Register<Target>(null));
        }

        [Test]
        public void Register_SameTypeTwice_Throws() {
            InjectionProcessor.Register<Target>(t => t.Marker = "first");

            // Underlying Dictionary.Add throws on duplicate key; InjectionProcessor
            // does not pre-check so the raw ArgumentException surfaces.
            Assert.Throws<ArgumentException>(
                () => InjectionProcessor.Register<Target>(t => t.Marker = "second"));
        }

        [Test]
        public void Register_DelegateIsInvokedByInject() {
            InjectionProcessor.Register<Target>(t => t.Marker = "injected");

            var target = new Target();
            InjectionProcessor.Inject(target);

            Assert.That(target.Marker, Is.EqualTo("injected"));
            Assert.That(target.Injected, Is.True);
        }
    }

    public class InjectionProcessorInjectTests {
        public sealed class Target : IInjectable {
            public bool Injected { get; set; }
            public int Count;
        }
        public sealed class Unregistered : IInjectable {
            public bool Injected { get; set; }
        }
        public sealed class NotInjectable { }

        private sealed class CaptureLogger : IButtrLogger {
            public readonly List<string> Warnings = new();
            public void Log(string message) { }
            public void LogWarning(string message) => Warnings.Add(message);
            public void LogError(string message) { }
        }

        [TearDown]
        public void Reset() {
            InjectionProcessor.Clear();
            ButtrLog.SetLogger(null);
        }

        [Test]
        public void Inject_Null_Throws() {
            Assert.Throws<InjectionException>(() => InjectionProcessor.Inject(null));
        }

        [Test]
        public void Inject_NonIInjectable_Throws() {
            Assert.Throws<InjectionException>(() => InjectionProcessor.Inject(new NotInjectable()));
        }

        [Test]
        public void Inject_WithoutRegistration_Throws() {
            Assert.Throws<InjectionException>(() => InjectionProcessor.Inject(new Unregistered()));
        }

        [Test]
        public void Inject_SetsInjectedFlagTrue() {
            InjectionProcessor.Register<Target>(t => t.Count = 1);

            var target = new Target();
            Assert.That(target.Injected, Is.False);

            InjectionProcessor.Inject(target);
            Assert.That(target.Injected, Is.True);
        }

        [Test]
        public void Inject_AlreadyInjected_SkipsAndWarnsViaButtrLog() {
            var capture = new CaptureLogger();
            ButtrLog.SetLogger(capture);

            InjectionProcessor.Register<Target>(t => t.Count += 1);

            var target = new Target { Injected = true };
            InjectionProcessor.Inject(target);

            Assert.That(target.Count, Is.EqualTo(0),
                "Delegate must not run when Injected flag is already true.");
            Assert.That(capture.Warnings, Has.Count.EqualTo(1));
            Assert.That(capture.Warnings[0], Does.Contain("twice"));
        }

        [Test]
        public void Inject_SameInstanceTwice_SecondCallIsNoOpAndWarns() {
            var capture = new CaptureLogger();
            ButtrLog.SetLogger(capture);

            var invocations = 0;
            InjectionProcessor.Register<Target>(t => invocations++);

            var target = new Target();
            InjectionProcessor.Inject(target);
            InjectionProcessor.Inject(target);

            Assert.That(invocations, Is.EqualTo(1));
            Assert.That(capture.Warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void Inject_TwoDifferentInstancesOfSameType_BothInjected() {
            var invocations = 0;
            InjectionProcessor.Register<Target>(t => invocations++);

            var a = new Target();
            var b = new Target();
            InjectionProcessor.Inject(a);
            InjectionProcessor.Inject(b);

            Assert.That(invocations, Is.EqualTo(2));
            Assert.That(a.Injected, Is.True);
            Assert.That(b.Injected, Is.True);
        }
    }

    public class InjectionProcessorClearTests {
        public sealed class Target : IInjectable {
            public bool Injected { get; set; }
        }

        [TearDown]
        public void Reset() => InjectionProcessor.Clear();

        [Test]
        public void Clear_EmptiesRegistry_SubsequentInjectThrows() {
            InjectionProcessor.Register<Target>(t => { });
            InjectionProcessor.Clear();

            Assert.Throws<InjectionException>(() => InjectionProcessor.Inject(new Target()));
        }

        [Test]
        public void Clear_AllowsReregistrationOfSameType() {
            InjectionProcessor.Register<Target>(t => { });
            InjectionProcessor.Clear();

            Assert.DoesNotThrow(() => InjectionProcessor.Register<Target>(t => { }));
        }

        [Test]
        public void Clear_OnEmptyRegistry_IsNoOp() {
            Assert.DoesNotThrow(() => InjectionProcessor.Clear());
            Assert.DoesNotThrow(() => InjectionProcessor.Clear());
        }
    }

    public class InjectAttributeTests {
        [Test]
        public void DefaultConstructor_LeavesScopeNull() {
            var attr = new InjectAttribute();
            Assert.That(attr.Scope, Is.Null);
        }

        [Test]
        public void ScopeConstructor_StoresScope() {
            var attr = new InjectAttribute("gameplay");
            Assert.That(attr.Scope, Is.EqualTo("gameplay"));
        }

        [Test]
        public void ScopeConstructor_WithNull_StoresNull() {
            var attr = new InjectAttribute(null);
            Assert.That(attr.Scope, Is.Null);
        }

        [Test]
        public void AttributeUsage_IsFieldOnly() {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(InjectAttribute), typeof(AttributeUsageAttribute));
            Assert.That(usage, Is.Not.Null);
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Field));
        }
    }
}
