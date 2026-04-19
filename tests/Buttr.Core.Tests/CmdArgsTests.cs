using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class CmdArgsTests {
        [Test]
        public void Read_BeforeInitialize_ReturnsEmptyDictionary_DoesNotThrow() {
            // Reset to known-empty state by initialising with no args.
            CMDArgs.Initialize(System.Array.Empty<string>());

            var dict = CMDArgs.Read();

            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(CMDArgs.Exists("--anything"), Is.False);
        }

        [Test]
        public void Initialize_PairwiseArgs_AreReadable() {
            CMDArgs.Initialize(new[] { "--foo", "bar", "--baz", "qux" });

            Assert.That(CMDArgs.Exists("--foo"), Is.True);
            Assert.That(CMDArgs.Exists("--baz"), Is.True);
            Assert.That(CMDArgs.TryGetValue("--foo", out var v1), Is.True);
            Assert.That(v1, Is.EqualTo("bar"));
            Assert.That(CMDArgs.TryGetValue("--baz", out var v2), Is.True);
            Assert.That(v2, Is.EqualTo("qux"));
        }

        [Test]
        public void Initialize_TrailingUnpairedKey_GetsEmptyValue() {
            CMDArgs.Initialize(new[] { "--flag" });

            Assert.That(CMDArgs.Exists("--flag"), Is.True);
            Assert.That(CMDArgs.TryGetValue("--flag", out var v), Is.True);
            Assert.That(v, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Initialize_NullArgs_ResultsInEmptyDictionary() {
            CMDArgs.Initialize(null);

            Assert.That(CMDArgs.Read().Count, Is.EqualTo(0));
        }
    }
}
