using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Buttr.Core.Analyzers {
    public sealed class Registration {
        public string? KeyType { get; set; }
        public string? KeyTypeFullName { get; set; }
        public string? ConcreteType { get; set; }
        public string? ConcreteTypeFullName { get; set; }
        public ContainerKind Container { get; set; }
        public string? ScopeKey { get; set; }
        public Lifetime Lifetime { get; set; }
        public Visibility Visibility { get; set; }
        public Location? CallSite { get; set; }
        public ISymbol? BuilderSymbol { get; set; }
        public List<AliasKey> Aliases { get; } = new();
    }

    public sealed class AliasKey {
        public string AliasTypeFullName { get; set; } = string.Empty;
        public Location? CallSite { get; set; }
    }
}
