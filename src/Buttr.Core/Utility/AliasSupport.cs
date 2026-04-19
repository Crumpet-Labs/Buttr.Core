using System;
using System.Collections.Generic;
using System.Reflection;

namespace Buttr.Core {
    internal static class AliasSupport {
        private static readonly object[] s_NullArgs = { null };

        internal static void SetApplicationForAliases(Registration registration, IObjectResolver resolver) {
            var aliases = registration.Aliases;
            if (aliases == null) return;

            foreach (var alias in aliases) InvokeApplicationSet(alias, resolver);
        }

        internal static void ClearApplicationForAliases(Registration registration) {
            var aliases = registration.Aliases;
            if (aliases == null) return;

            foreach (var alias in aliases) InvokeApplicationSet(alias, null);
        }

        private static void InvokeApplicationSet(Type aliasType, IObjectResolver resolver) {
            var appType = typeof(Application<>).MakeGenericType(aliasType);
            var setMethod = appType.GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Static);
            setMethod.Invoke(null, resolver == null ? s_NullArgs : new object[] { resolver });
        }

        internal static void AddLocalAlias<TConcrete, TAlias>(Registration registration, Dictionary<Type, Registration> registry) {
            ValidateAssignability<TConcrete, TAlias>();

            var aliasType = typeof(TAlias);
            if (registry.ContainsKey(aliasType))
                throw new DuplicateAliasException(
                    $"Alias {aliasType.FullName} is already registered in this container. An alias key must be unique; use All<{aliasType.Name}>() for bulk resolution across multiple implementations.");

            registration.AddAlias(aliasType);
            registry[aliasType] = registration;
        }

        internal static void StageApplicationAlias<TConcrete, TAlias>(Registration registration) {
            ValidateAssignability<TConcrete, TAlias>();
            registration.AddAlias(typeof(TAlias));
        }

        private static void ValidateAssignability<TConcrete, TAlias>() {
            var aliasType = typeof(TAlias);
            var concreteType = typeof(TConcrete);

            if (aliasType.IsAssignableFrom(concreteType) == false)
                throw new ObjectResolverException(
                    $"Cannot alias {concreteType.FullName} as {aliasType.FullName}: the alias must be a supertype of the concrete type.");
        }
    }
}
