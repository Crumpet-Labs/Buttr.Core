using System;
using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class DisposableCollectionTests {
        private sealed class Stub : IDisposable {
            public int Count;
            public void Dispose() => Count++;
        }

        [Test]
        public void Dispose_CallsDisposeOnEveryMember_InOrder() {
            var order = new System.Collections.Generic.List<string>();
            var a = new Disposable("a", order);
            var b = new Disposable("b", order);
            var c = new Disposable("c", order);
            var collection = new DisposableCollection(a, b, c);

            collection.Dispose();

            Assert.That(order, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Dispose_EmptyCollection_IsNoOp() {
            var collection = new DisposableCollection();
            Assert.DoesNotThrow(() => collection.Dispose());
        }

        [Test]
        public void Dispose_TolerantOfNullEntries() {
            var stub = new Stub();
            var collection = new DisposableCollection(null, stub, null);

            Assert.DoesNotThrow(() => collection.Dispose());
            Assert.That(stub.Count, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_CalledTwice_InvokesEachMemberTwice() {
            // Documents current behaviour: DisposableCollection is not idempotent.
            // Calling Dispose twice calls each member's Dispose twice.
            var stub = new Stub();
            var collection = new DisposableCollection(stub);

            collection.Dispose();
            collection.Dispose();

            Assert.That(stub.Count, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_NullArray_ThrowsArgumentNullException() {
            Assert.Throws<ArgumentNullException>(() => new DisposableCollection(null));
        }

        private sealed class Disposable : IDisposable {
            private readonly string m_Name;
            private readonly System.Collections.Generic.List<string> m_Order;
            public Disposable(string name, System.Collections.Generic.List<string> order) {
                m_Name = name;
                m_Order = order;
            }
            public void Dispose() => m_Order.Add(m_Name);
        }
    }
}
