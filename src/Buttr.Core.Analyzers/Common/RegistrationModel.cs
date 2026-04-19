using System.Collections.Generic;
using System.Linq;

namespace Buttr.Core.Analyzers {
    public sealed class RegistrationModel {
        public List<Registration> ApplicationRegistrations { get; } = new();
        public List<Registration> DIRegistrations { get; } = new();
        public Dictionary<string, List<Registration>> ScopeRegistrations { get; } = new();

        public bool IsHidden(string typeFullName) {
            foreach (var r in ApplicationRegistrations) {
                if (r.Visibility == Visibility.Hidden && MatchesAnyKey(r, typeFullName))
                    return true;
            }

            foreach (var r in DIRegistrations) {
                if (r.Visibility == Visibility.Hidden && MatchesAnyKey(r, typeFullName))
                    return true;
            }

            foreach (var kvp in ScopeRegistrations) {
                foreach (var r in kvp.Value) {
                    if (r.Visibility == Visibility.Hidden && MatchesAnyKey(r, typeFullName))
                        return true;
                }
            }

            return false;
        }

        public bool IsResolvableFrom(string typeFullName, ContainerKind container, string? scopeKey) {
            switch (container) {
                case ContainerKind.Scope when scopeKey != null:
                    if (ScopeRegistrations.TryGetValue(scopeKey, out var scopeRegs)) {
                        if (scopeRegs.Any(r => MatchesAnyKey(r, typeFullName)))
                            return true;
                    }
                    goto case ContainerKind.Application;

                case ContainerKind.DI:
                    if (DIRegistrations.Any(r => MatchesAnyKey(r, typeFullName)))
                        return true;
                    goto case ContainerKind.Application;

                case ContainerKind.Application:
                    return ApplicationRegistrations.Any(r => MatchesAnyKey(r, typeFullName));
            }

            return false;
        }

        public List<(ContainerKind kind, string? scopeKey)> ContainersFor(string concreteTypeFullName) {
            var results = new List<(ContainerKind, string?)>();

            if (ApplicationRegistrations.Any(r => r.ConcreteTypeFullName == concreteTypeFullName))
                results.Add((ContainerKind.Application, null));

            if (DIRegistrations.Any(r => r.ConcreteTypeFullName == concreteTypeFullName))
                results.Add((ContainerKind.DI, null));

            foreach (var kvp in ScopeRegistrations) {
                if (kvp.Value.Any(r => r.ConcreteTypeFullName == concreteTypeFullName))
                    results.Add((ContainerKind.Scope, kvp.Key));
            }

            return results;
        }

        private static bool MatchesAnyKey(Registration registration, string typeFullName) {
            if (registration.KeyTypeFullName == typeFullName) return true;

            for (var i = 0; i < registration.Aliases.Count; i++) {
                if (registration.Aliases[i].AliasTypeFullName == typeFullName) return true;
            }

            return false;
        }
    }
}
