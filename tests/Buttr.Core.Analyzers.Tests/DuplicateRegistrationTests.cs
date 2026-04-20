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

    [Test]
    public Task Duplicate_SeparateBuilderInstances_NoDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public class Program {
        public static void BuildA() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>();
        }
        public static void BuildB() {
            var builder = new DIBuilder();
            builder.Resolvers.AddSingleton<Foo>();
        }
    }
}";
        return Verifiers.Analyze<DuplicateRegistrationAnalyzer>(source);
    }

    [Test]
    public Task Duplicate_ChainedFluentSameFile_ReportsDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public class Program {
        public static void Build() {
            new DIBuilder().Resolvers.AddSingleton<Foo>();
            {|BUTTR006:new DIBuilder().Resolvers.AddSingleton<Foo>()|};
        }
    }
}";
        return Verifiers.Analyze<DuplicateRegistrationAnalyzer>(source);
    }

    [Test]
    public Task Duplicate_SameBuilderField_ReportsDiagnostic() {
        var source = @"using Buttr.Core;
namespace App {
    public sealed class Foo { }
    public class Program {
        private static readonly DIBuilder s_Builder = new DIBuilder();
        public static void RegisterA() {
            s_Builder.Resolvers.AddSingleton<Foo>();
        }
        public static void RegisterB() {
            {|BUTTR006:s_Builder.Resolvers.AddSingleton<Foo>()|};
        }
    }
}";
        return Verifiers.Analyze<DuplicateRegistrationAnalyzer>(source);
    }
}
