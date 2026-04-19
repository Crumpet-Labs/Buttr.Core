using System.Threading.Tasks;
using NUnit.Framework;

namespace Buttr.Core.Analyzers.Tests;

[TestFixture]
public class DuplicateAliasTests {
    [Test]
    public Task Alias_DuplicateInSameContainer_ReportsDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public interface IThing { }
    public sealed class ThingA : IThing { }
    public sealed class ThingB : IThing { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<ThingA>().As<IThing>();
            builder.Resolvers.AddSingleton<ThingB>().{|BUTTR014:As<IThing>|}();
        }
    }
}";
        return Verifiers.Analyze<DuplicateAliasAnalyzer>(source);
    }

    [Test]
    public Task Alias_UniqueAcrossRegistrations_NoDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public interface IFoo { }
    public interface IBar { }
    public sealed class Foo : IFoo { }
    public sealed class Bar : IBar { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>().As<IFoo>();
            builder.Resolvers.AddSingleton<Bar>().As<IBar>();
        }
    }
}";
        return Verifiers.Analyze<DuplicateAliasAnalyzer>(source);
    }

    [Test]
    public Task Alias_Duplicate_FixRemovesSecondAliasCall() {
        var source = @"using Buttr.Core;
namespace App {
    public interface IThing { }
    public sealed class ThingA : IThing { }
    public sealed class ThingB : IThing { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<ThingA>().As<IThing>();
            builder.Resolvers.AddSingleton<ThingB>().{|BUTTR014:As<IThing>|}();
        }
    }
}";
        var fixedSource = @"using Buttr.Core;
namespace App {
    public interface IThing { }
    public sealed class ThingA : IThing { }
    public sealed class ThingB : IThing { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<ThingA>().As<IThing>();
            builder.Resolvers.AddSingleton<ThingB>();
        }
    }
}";
        return Verifiers.AnalyzeAndFix<DuplicateAliasAnalyzer, Fixes.DuplicateAliasFix>(source, fixedSource);
    }
}
