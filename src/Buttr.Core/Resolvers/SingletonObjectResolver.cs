using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal sealed class SingletonObjectResolver<TConcrete> : IResolver, IConfigurable<TConcrete> {
        private readonly Registration m_Registration;
        private readonly Dictionary<Type, Registration> m_Registry;
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        internal SingletonObjectResolver(Registration registration, Dictionary<Type, Registration> registry) {
            m_Registration = registration;
            m_Registry = registry;
        }

        public void Resolve() {
            m_Registration.Resolver = new SingletonResolverInternal<TConcrete>(m_Registry, m_Configuration, m_Factory);
        }

        IConfigurable<TConcrete> IConfigurable<TConcrete>.WithConfiguration(Func<TConcrete, TConcrete> configuration) {
            m_Configuration = configuration;
            return this;
        }

        IConfigurable<TConcrete> IConfigurable<TConcrete>.WithFactory(Func<TConcrete> factory) {
            m_Factory = factory;
            return this;
        }

        IConfigurable<TConcrete> IConfigurable<TConcrete>.As<TAlias>() {
            AliasSupport.AddLocalAlias<TConcrete, TAlias>(m_Registration, m_Registry);
            return this;
        }
    }

    internal sealed class SingletonObjectResolver<TAbstract, TConcrete> : IResolver, IConfigurable<TConcrete> where TConcrete : TAbstract {
        private readonly Registration m_Registration;
        private readonly Dictionary<Type, Registration> m_Registry;
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        internal SingletonObjectResolver(Registration registration, Dictionary<Type, Registration> registry) {
            m_Registration = registration;
            m_Registry = registry;
        }

        public void Resolve() {
            m_Registration.Resolver = new SingletonResolverInternal<TConcrete>(m_Registry, m_Configuration, m_Factory);
        }

        IConfigurable<TConcrete> IConfigurable<TConcrete>.WithConfiguration(Func<TConcrete, TConcrete> configuration) {
            m_Configuration = configuration;
            return this;
        }

        IConfigurable<TConcrete> IConfigurable<TConcrete>.WithFactory(Func<TConcrete> factory) {
            m_Factory = factory;
            return this;
        }

        IConfigurable<TConcrete> IConfigurable<TConcrete>.As<TAlias>() {
            AliasSupport.AddLocalAlias<TConcrete, TAlias>(m_Registration, m_Registry);
            return this;
        }
    }
}
