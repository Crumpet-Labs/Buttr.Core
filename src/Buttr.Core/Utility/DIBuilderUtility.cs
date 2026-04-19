using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal static class DIBuilderUtility {
        /// <summary>
        /// For each requirement in order, if the registry has a resolver for it,
        /// writes the resolved instance to <c>output[i]</c>. Slots with no match
        /// (or null-entry requirements) are left untouched. Callers must pass a
        /// cleared <paramref name="output"/> buffer; the returned count is the
        /// number of slots that were written.
        /// </summary>
        public static int CollectResolvedDependencies(this Dictionary<Type, IObjectResolver> registry, Type[] requirements, object[] output) {
            var count = 0;
            for (var i = 0; i < requirements.Length; i++) {
                var type = requirements[i];
                if (type == null) continue;
                if (registry.TryGetValue(type, out var resolver)) {
                    output[i] = resolver.Resolve();
                    count++;
                }
                else {
                    ButtrLog.Log($"Unable to locate Dependency in Container {type.FullName} ");
                }
            }
            return count;
        }

        /// <summary>
        /// For each requirement whose type is NOT in the registry, writes the
        /// requirement type to <c>output[i]</c>. Slots that are in the registry
        /// are left untouched. Preserving the index lets callers pair the
        /// missing types back to their original requirement positions.
        /// </summary>
        public static int CollectUnresolvedTypes(this Dictionary<Type, IObjectResolver> registry, Type[] requirements, Type[] output) {
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
