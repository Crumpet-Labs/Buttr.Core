namespace Buttr.Core {
    internal interface IObjectResolver {
        bool IsResolved { get; }
        bool IsCached { get; }
        object Resolve();
    }
}