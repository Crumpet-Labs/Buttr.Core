using Buttr.Core;
using NUnit.Framework;

namespace Buttr.Core.Tests {
    public class LoggerTests {
        private sealed class CaptureLogger : IButtrLogger {
            public string Last;
            public string LastLevel;
            public void Log(string message)        { LastLevel = "log";  Last = message; }
            public void LogWarning(string message) { LastLevel = "warn"; Last = message; }
            public void LogError(string message)   { LastLevel = "err";  Last = message; }
        }

        [TearDown]
        public void ResetLogger() {
            ButtrLog.SetLogger(null);
        }

        [Test]
        public void Default_Log_DoesNotThrow() {
            Assert.DoesNotThrow(() => ButtrLog.Log("hello"));
            Assert.DoesNotThrow(() => ButtrLog.LogWarning("hello"));
            Assert.DoesNotThrow(() => ButtrLog.LogError("hello"));
        }

        [Test]
        public void SetLogger_RoutesToCustomLogger() {
            var capture = new CaptureLogger();
            ButtrLog.SetLogger(capture);

            ButtrLog.Log("L");
            Assert.That(capture.Last, Is.EqualTo("L"));
            Assert.That(capture.LastLevel, Is.EqualTo("log"));

            ButtrLog.LogWarning("W");
            Assert.That(capture.Last, Is.EqualTo("W"));
            Assert.That(capture.LastLevel, Is.EqualTo("warn"));

            ButtrLog.LogError("E");
            Assert.That(capture.Last, Is.EqualTo("E"));
            Assert.That(capture.LastLevel, Is.EqualTo("err"));
        }

        [Test]
        public void SetLogger_Null_FallsBackToDefault_NoThrow() {
            ButtrLog.SetLogger(null);
            Assert.DoesNotThrow(() => ButtrLog.Log("after-null"));
        }
    }
}
