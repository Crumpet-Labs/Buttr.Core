using System.Threading.Tasks;
using NUnit.Framework;

namespace Buttr.Core.Analyzers.Tests;

[TestFixture]
public class AliasSupertypeTests {
    [Test]
    public Task Alias_NotSupertypeOfConcrete_ReportsDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public sealed class Unrelated { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>().{|BUTTR013:As<Unrelated>|}();
        }
    }
}";
        return Verifiers.Analyze<AliasSupertypeAnalyzer>(source);
    }

    [Test]
    public Task Alias_SupertypeOfConcrete_NoDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public interface IFoo { }
    public sealed class Foo : IFoo { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>().As<IFoo>();
        }
    }
}";
        return Verifiers.Analyze<AliasSupertypeAnalyzer>(source);
    }

    [Test]
    public Task Alias_ReplacedWithSupertype_RoundTrips() {
        var source = @"using Buttr.Core;
namespace App {
    public interface IFoo { }
    public sealed class Foo : IFoo { }
    public sealed class Unrelated { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>().{|BUTTR013:As<Unrelated>|}();
        }
    }
}";
        var fixedSource = @"using Buttr.Core;
namespace App {
    public interface IFoo { }
    public sealed class Foo : IFoo { }
    public sealed class Unrelated { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>().As<App.IFoo>();
        }
    }
}";

        return Verifiers.AnalyzeAndFix<AliasSupertypeAnalyzer, Fixes.AliasSupertypeFix>(source, fixedSource);
    }
}
