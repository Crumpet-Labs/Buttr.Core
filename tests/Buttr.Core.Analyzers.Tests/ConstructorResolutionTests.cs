using System.Threading.Tasks;
using NUnit.Framework;

namespace Buttr.Core.Analyzers.Tests;

[TestFixture]
public class ConstructorResolutionTests {
    [Test]
    public Task Constructor_UnresolvableDependency_ReportsDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Dep { }
    public sealed class Consumer { {|BUTTR004:public Consumer(Dep dep) { }|} }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Consumer>();
        }
    }
}";
        return Verifiers.Analyze<ConstructorResolutionAnalyzer>(source);
    }

    [Test]
    public Task Constructor_AllDependenciesRegistered_NoDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Dep { }
    public sealed class Consumer { public Consumer(Dep dep) { } }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Dep>();
            builder.Resolvers.AddSingleton<Consumer>();
        }
    }
}";
        return Verifiers.Analyze<ConstructorResolutionAnalyzer>(source);
    }
}
