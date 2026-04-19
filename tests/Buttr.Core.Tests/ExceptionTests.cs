using System;
using Buttr.Core;
using Buttr.Injection;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class ObjectResolverExceptionTests {
        [Test]
        public void Constructor_StoresMessage() {
            var ex = new ObjectResolverException("boom");
            Assert.That(ex.Message, Is.EqualTo("boom"));
        }

        [Test]
        public void InheritsFromException() {
            Assert.That(typeof(Exception).IsAssignableFrom(typeof(ObjectResolverException)), Is.True);
        }

        [Test]
        public void IsSealed() {
            Assert.That(typeof(ObjectResolverException).IsSealed, Is.True);
        }
    }

    public class ConfigurableExceptionTests {
        [Test]
        public void Constructor_StoresMessage() {
            var ex = new ConfigurableException("nope");
            Assert.That(ex.Message, Is.EqualTo("nope"));
        }

        [Test]
        public void InheritsFromException() {
            Assert.That(typeof(Exception).IsAssignableFrom(typeof(ConfigurableException)), Is.True);
        }

        [Test]
        public void IsSealed() {
            Assert.That(typeof(ConfigurableException).IsSealed, Is.True);
        }
    }

    public class InjectionExceptionTests {
        [Test]
        public void Constructor_StoresMessage() {
            var ex = new InjectionException("bad");
            Assert.That(ex.Message, Is.EqualTo("bad"));
        }

        [Test]
        public void InheritsFromException() {
            Assert.That(typeof(Exception).IsAssignableFrom(typeof(InjectionException)), Is.True);
        }

        [Test]
        public void IsSealed() {
            Assert.That(typeof(InjectionException).IsSealed, Is.True);
        }
    }
}
