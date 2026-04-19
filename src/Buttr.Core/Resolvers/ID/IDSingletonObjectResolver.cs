using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal sealed class IDSingletonObjectResolver<TID, TConcrete> : IResolver, IConfigurable<TConcrete> {
        private readonly TID m_ID;
        private readonly Dictionary<TID, IObjectResolver> m_Registry;
        
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        internal IDSingletonObjectResolver(TID id, Dictionary<TID, IObjectResolver> registry) {
            m_ID = id;
            m_Registry = registry;
        }
        
        public void Resolve() {
            m_Registry[m_ID] = new IDSingletonResolverInternal<TConcrete>(m_Configuration, m_Factory);
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
            throw new NotSupportedException("Aliasing is not supported on ID-keyed registrations. Use a type-keyed DIBuilder instead.");
        }
    }
}