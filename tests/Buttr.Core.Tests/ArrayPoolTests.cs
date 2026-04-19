using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    // Buttr.Core.ArrayPool<T> keeps exact-size semantics on Release (contents cleared)
    // and resizes on Get if the pooled array length doesn't match the request.
    public class ArrayPoolTests {
        [Test]
        public void Get_ReturnsArrayOfRequestedSize() {
            var arr = ArrayPool<int>.Get(7);
            Assert.That(arr.Length, Is.EqualTo(7));
            ArrayPool<int>.Release(arr);
        }

        [Test]
        public void Release_ClearsContents() {
            var arr = ArrayPool<string>.Get(3);
            arr[0] = "alpha";
            arr[1] = "beta";
            arr[2] = "gamma";
            ArrayPool<string>.Release(arr);

            // Re-rent same size. Custom pool reuses and guarantees cleared contents.
            var reused = ArrayPool<string>.Get(3);
            Assert.That(reused.Length, Is.EqualTo(3));
            foreach (var s in reused) Assert.That(s, Is.Null);
            ArrayPool<string>.Release(reused);
        }

        [Test]
        public void Release_ThenGet_SameSize_ReusesPooledArray() {
            var first = ArrayPool<int>.Get(5);
            ArrayPool<int>.Release(first);

            var second = ArrayPool<int>.Get(5);
            Assert.That(second, Is.SameAs(first));
            ArrayPool<int>.Release(second);
        }
    }
}
