using NUnit.Framework;

// Buttr's Core uses process-wide static state (ApplicationRegistry, Application<T>,
// CMDArgs, ButtrLog). Tests must not run in parallel.
[assembly: Parallelizable(ParallelScope.None)]
[assembly: LevelOfParallelism(1)]
