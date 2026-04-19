using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Buttr.Core.Analyzers.Tests;

[TestFixture]
public class DisposableTransientTests {
    [Test]
    public Task Transient_DisposableType_ReportsDiagnostic() {
        var source = @"using System;
using Buttr.Core;
namespace App {
    public sealed class Leaky : IDisposable { public void Dispose() { } }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            {|BUTTR012:builder.Resolvers.AddTransient<Leaky>()|};
        }
    }
}";
        return Verifiers.Analyze<DisposableTransientAnalyzer>(source);
    }

    [Test]
    public Task Transient_NonDisposable_NoDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Clean { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddTransient<Clean>();
        }
    }
}";
        return Verifiers.Analyze<DisposableTransientAnalyzer>(source);
    }

    [Test]
    public Task Transient_Disposable_FixChangesToSingleton() {
        var source = @"using System;
using Buttr.Core;
namespace App {
    public sealed class Leaky : IDisposable { public void Dispose() { } }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            {|BUTTR012:builder.Resolvers.AddTransient<Leaky>()|};
        }
    }
}";
        var fixedSource = @"using System;
using Buttr.Core;
namespace App {
    public sealed class Leaky : IDisposable { public void Dispose() { } }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Leaky>();
        }
    }
}";
        return Verifiers.AnalyzeAndFix<DisposableTransientAnalyzer, Fixes.DisposableTransientFix>(source, fixedSource);
    }
}
