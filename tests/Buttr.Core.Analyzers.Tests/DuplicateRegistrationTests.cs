using System.Threading.Tasks;
using NUnit.Framework;

namespace Buttr.Core.Analyzers.Tests;

[TestFixture]
public class DuplicateRegistrationTests {
    [Test]
    public Task Duplicate_SameKeyInSameContainer_ReportsDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>();
            {|BUTTR006:builder.Resolvers.AddSingleton<Foo>()|};
        }
    }
}";
        return Verifiers.Analyze<DuplicateRegistrationAnalyzer>(source);
    }

    [Test]
    public Task Unique_Registrations_NoDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public sealed class Bar { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>();
            builder.Resolvers.AddSingleton<Bar>();
        }
    }
}";
        return Verifiers.Analyze<DuplicateRegistrationAnalyzer>(source);
    }

    [Test]
    public Task Duplicate_FixRemovesSecondCall() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>();
            {|BUTTR006:builder.Resolvers.AddSingleton<Foo>()|};
        }
    }
}";
        var fixedSource = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public class Program {
        public static void Build() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>();
        }
    }
}";
        return Verifiers.AnalyzeAndFix<DuplicateRegistrationAnalyzer, Fixes.RemoveDuplicateRegistrationFix>(source, fixedSource);
    }
}
