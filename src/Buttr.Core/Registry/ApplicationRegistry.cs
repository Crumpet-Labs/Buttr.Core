using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal static class ApplicationRegistry {
        private static readonly Dictionary<Type, Registration> s_Registry;
        private static readonly List<Registration> s_Registrations;

        static ApplicationRegistry() {
            s_Registry = new();
            s_Registrations = new();
        }

        public static int GetDependencies(Type[] requirements, object[] output) {
            return s_Registry.CollectResolvedDependencies(requirements, output);
        }

        internal static void Register(Registration registration) {
            if (s_Registry.TryAdd(registration.PrimaryKey, registration) == false) {
                throw new ObjectResolverException($"Failed to add resolver to Application Registry, Has {registration.PrimaryKey.Name} already been added?");
            }
            s_Registrations.Add(registration);

            var aliases = registration.Aliases;
            if (aliases == null) return;

            foreach (var alias in aliases) {
                if (s_Registry.TryAdd(alias, registration) == false) {
                    throw new DuplicateAliasException(
                        $"Alias {alias.FullName} is already registered. An alias key must be unique; use All<{alias.Name}>() for bulk resolution across multiple implementations.");
                }
            }
        }

        internal static void Remove(Registration registration) {
            if (s_Registry.Remove(registration.PrimaryKey) == false) return;

            s_Registrations.Remove(registration);

            var aliases = registration.Aliases;
            if (aliases == null) return;

            foreach (var alias in aliases) s_Registry.Remove(alias);
        }

        internal static IEnumerable<T> All<T>() {
            var target = typeof(T);
            for (var i = 0; i < s_Registrations.Count; i++) {
                var registration = s_Registrations[i];
                if (registration.IsHidden) continue;
                if (target.IsAssignableFrom(registration.ConcreteType) == false) continue;
                yield return (T)registration.Resolver.Resolve();
            }
        }
    }
}
