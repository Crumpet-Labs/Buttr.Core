using System;

namespace Buttr.Core {
    internal sealed class StaticSingletonResolver<TConcrete> : IResolver, IDisposable, IConfigurable<TConcrete> {
        private readonly Registration m_Registration;
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        private StaticSingleton<TConcrete> m_Singleton;

        internal StaticSingletonResolver(Registration registration) {
            m_Registration = registration;
        }

        public void Resolve() {
            m_Singleton = new StaticSingleton<TConcrete>(m_Configuration, m_Factory);
            m_Registration.Resolver = m_Singleton;
            ApplicationRegistry.Register(m_Registration);
            Application<TConcrete>.Set(m_Singleton);
            AliasSupport.SetApplicationForAliases(m_Registration, m_Singleton);
        }

        public void Dispose() {
            if (m_Singleton == null) return;

            if (m_Singleton.IsResolved && m_Singleton.Resolve() is IDisposable disposable) {
                disposable.Dispose();
            }

            m_Singleton.Dispose();
            m_Singleton = null;
            AliasSupport.ClearApplicationForAliases(m_Registration);
            ApplicationRegistry.Remove(m_Registration);
            Application<TConcrete>.Set(null);
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
            AliasSupport.StageApplicationAlias<TConcrete, TAlias>(m_Registration);
            return this;
        }
    }

    internal sealed class StaticSingletonResolver<TAbstract, TConcrete> : IResolver, IDisposable, IConfigurable<TConcrete> {
        private readonly Registration m_Registration;
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        private StaticSingleton<TAbstract, TConcrete> m_Singleton;

        internal StaticSingletonResolver(Registration registration) {
            m_Registration = registration;
        }

        public void Resolve() {
            m_Singleton = new StaticSingleton<TAbstract, TConcrete>(m_Configuration, m_Factory);
            m_Registration.Resolver = m_Singleton;
            ApplicationRegistry.Register(m_Registration);
            Application<TAbstract>.Set(m_Singleton);
            AliasSupport.SetApplicationForAliases(m_Registration, m_Singleton);
        }

        public void Dispose() {
            if (m_Singleton == null) return;

            if (m_Singleton.IsResolved && m_Singleton.Resolve() is IDisposable disposable) {
                disposable.Dispose();
            }

            m_Singleton.Dispose();
            m_Singleton = null;
            AliasSupport.ClearApplicationForAliases(m_Registration);
            ApplicationRegistry.Remove(m_Registration);
            Application<TConcrete>.Set(null);
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
            AliasSupport.StageApplicationAlias<TConcrete, TAlias>(m_Registration);
            return this;
        }
    }
}
