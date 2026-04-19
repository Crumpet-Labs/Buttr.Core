using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal sealed class ObjectResolverCollection : IResolverCollection {
        private readonly List<Registration> m_Registrations;
        private readonly Dictionary<Type, Registration> m_KeyIndex;
        private readonly List<IResolver> m_Resolvers = new();

        internal ObjectResolverCollection(List<Registration> registrations, Dictionary<Type, Registration> keyIndex) {
            m_Registrations = registrations;
            m_KeyIndex = keyIndex;
        }

        public IConfigurable<TConcrete> AddSingleton<TConcrete>() {
            var registration = new Registration(typeof(TConcrete), typeof(TConcrete), isHidden: false);
            Track(registration);
            var resolver = new SingletonObjectResolver<TConcrete>(registration, m_KeyIndex);
            m_Resolvers.Add(resolver);
            return resolver;
        }

        public IConfigurable<TConcrete> AddSingleton<TAbstract, TConcrete>() where TConcrete : TAbstract {
            var registration = new Registration(typeof(TAbstract), typeof(TConcrete), isHidden: false);
            Track(registration);
            var resolver = new SingletonObjectResolver<TAbstract, TConcrete>(registration, m_KeyIndex);
            m_Resolvers.Add(resolver);
            return resolver;
        }

        public IConfigurable<TConcrete> AddTransient<TConcrete>() {
            var registration = new Registration(typeof(TConcrete), typeof(TConcrete), isHidden: false);
            Track(registration);
            var resolver = new TransientObjectResolver<TConcrete>(registration, m_KeyIndex);
            m_Resolvers.Add(resolver);
            return resolver;
        }

        public IConfigurable<TConcrete> AddTransient<TAbstract, TConcrete>() where TConcrete : TAbstract {
            var registration = new Registration(typeof(TAbstract), typeof(TConcrete), isHidden: false);
            Track(registration);
            var resolver = new TransientObjectResolver<TAbstract, TConcrete>(registration, m_KeyIndex);
            m_Resolvers.Add(resolver);
            return resolver;
        }

        public void Resolve() {
            foreach (var resolver in m_Resolvers) resolver.Resolve();
        }

        public void Dispose() {
            foreach (var resolver in m_Resolvers) {
                if (resolver is IDisposable disposable) disposable.Dispose();
            }
        }

        private void Track(Registration registration) {
            m_Registrations.Add(registration);
            m_KeyIndex[registration.PrimaryKey] = registration;
        }
    }
}
