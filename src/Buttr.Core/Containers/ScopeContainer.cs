using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal sealed class ScopeContainer : IDIContainer {
        private readonly List<Registration> m_Registrations;
        private readonly Dictionary<Type, Registration> m_KeyIndex;

        private IDisposable m_Registration;

        internal ScopeContainer(List<Registration> registrations, Dictionary<Type, Registration> keyIndex) {
            m_Registrations = registrations;
            m_KeyIndex = keyIndex;
        }

        internal IDisposable ScopeRegistration {
            set { m_Registration = value; }
        }

        public T Get<T>() {
            if (m_KeyIndex.TryGetValue(typeof(T), out var registration)) {
                if (registration.IsHidden)
                    throw new ObjectResolverException("Attempting to retrieve a Hidden object from a ScopeContainer");

                return (T)registration.Resolver.Resolve();
            }

            return default;
        }

        public bool TryGet<T>(out T value) {
            if (m_KeyIndex.TryGetValue(typeof(T), out var registration)) {
                if (registration.IsHidden)
                    throw new ObjectResolverException("Attempting to retrieve a Hidden object from a ScopeContainer");

                value = (T)registration.Resolver.Resolve();
                return true;
            }

            value = default;
            return false;
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
            m_Registration.Dispose();
        }
    }
}
