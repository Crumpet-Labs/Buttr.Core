using System;
using System.Collections.Generic;

namespace Buttr.Core {
    /// <summary>
    /// Created from a <see cref="DIBuilder{TID}"/>. A container to retrieve objects from.
    /// </summary>
    /// <remarks>
    /// The container should be cached and kept for use until disposal. It's advised to dispose of containers when finished.
    /// </remarks>
    /// <typeparam name="TID">The ID for which behaviours are to be identified</typeparam>
    internal class DIContainer<TID> : IDIContainer<TID> {
        private readonly Dictionary<TID, IObjectResolver> m_Registry;

        internal DIContainer(Dictionary<TID, IObjectResolver> registry) {
            m_Registry = registry;
        }

        public Type Type => typeof(TID);

        public T Get<T>(TID id) {
            if (m_Registry.TryGetValue(id, out var resolver)) {
                return (T)resolver.Resolve();
            }

            return default;
        }

        public bool TryGet<T>(TID id, out T value) {
            var found = m_Registry.TryGetValue(id, out var resolver);
            value = found ? (T)resolver.Resolve() : default;
            return found;
        }

        public void Dispose() {
            foreach (var resolver in m_Registry.Values) {
                if (resolver.IsCached && resolver.IsResolved && resolver.Resolve() is IDisposable disposable) {
                    disposable.Dispose();
                }
            }

            m_Registry.Clear();
        }
    }

    /// <summary>
    /// Created from a <see cref="DIBuilder"/>. A container to retrieve objects from.
    /// </summary>
    /// <remarks>
    /// The container should be cached and kept for use until disposal. It's advised to dispose of containers when finished.
    /// </remarks>
    internal sealed class DIContainer : IDIContainer {
        private readonly List<Registration> m_Registrations;
        private readonly Dictionary<Type, Registration> m_KeyIndex;

        internal DIContainer(List<Registration> registrations, Dictionary<Type, Registration> keyIndex) {
            m_Registrations = registrations;
            m_KeyIndex = keyIndex;
        }

        public T Get<T>() {
            if (m_KeyIndex.TryGetValue(typeof(T), out var registration)) {
                if (registration.IsHidden)
                    throw new ObjectResolverException("Attempting to retrieve a Hidden object from a DIContainer");

                return (T)registration.Resolver.Resolve();
            }

            return default;
        }

        public bool TryGet<T>(out T value) {
            if (m_KeyIndex.TryGetValue(typeof(T), out var registration)) {
                if (registration.IsHidden)
                    throw new ObjectResolverException("Attempting to retrieve a Hidden object from a DIContainer");

                value = (T)registration.Resolver.Resolve();
                return true;
            }

            value = default;
            return false;
        }

        public IEnumerable<T> All<T>() {
            var target = typeof(T);
            for (var i = 0; i < m_Registrations.Count; i++) {
                var registration = m_Registrations[i];
                if (registration.IsHidden) continue;
                if (target.IsAssignableFrom(registration.ConcreteType) == false) continue;
                yield return (T)registration.Resolver.Resolve();
            }
        }

        public void Dispose() {
            foreach (var registration in m_Registrations) {
                var resolver = registration.Resolver;
                if (resolver != null && resolver.IsCached && resolver.IsResolved && resolver.Resolve() is IDisposable disposable) {
                    disposable.Dispose();
                }
            }

            m_Registrations.Clear();
            m_KeyIndex.Clear();
        }
    }
}
