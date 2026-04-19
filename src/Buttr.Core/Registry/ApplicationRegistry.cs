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
        }

        internal static void Remove(Type primaryKey) {
            if (s_Registry.TryGetValue(primaryKey, out var registration)) {
                s_Registry.Remove(primaryKey);
                s_Registrations.Remove(registration);
            }
        }
    }
}
