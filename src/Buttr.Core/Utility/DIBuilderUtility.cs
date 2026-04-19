using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal static class DIBuilderUtility {
        public static int CollectResolvedDependencies(this Dictionary<Type, Registration> registry, Type[] requirements, object[] output) {
            var count = 0;
            for (var i = 0; i < requirements.Length; i++) {
                var type = requirements[i];
                if (type == null) continue;
                if (registry.TryGetValue(type, out var registration)) {
                    output[i] = registration.Resolver.Resolve();
                    count++;
                }
                else {
                    ButtrLog.Log($"Unable to locate Dependency in Container {type.FullName} ");
                }
            }
            return count;
        }

        public static int CollectUnresolvedTypes(this Dictionary<Type, Registration> registry, Type[] requirements, Type[] output) {
            var count = 0;
            for (var i = 0; i < requirements.Length; i++) {
                var type = requirements[i];
                if (type != null && registry.ContainsKey(type) == false) {
                    output[i] = type;
                    count++;
                }
            }
            return count;
        }
    }
}
