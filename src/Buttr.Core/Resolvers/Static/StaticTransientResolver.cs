using System;

namespace Buttr.Core {
    internal sealed class StaticTransientResolver<TConcrete> : IResolver, IDisposable, IConfigurable<TConcrete> {
        private readonly Registration m_Registration;
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        private StaticTransient<TConcrete> m_Transient;

        internal StaticTransientResolver(Registration registration) {
            m_Registration = registration;
        }

        public void Resolve() {
            m_Transient = new StaticTransient<TConcrete>(m_Configuration, m_Factory);
            m_Registration.Resolver = m_Transient;
            ApplicationRegistry.Register(m_Registration);
            Application<TConcrete>.Set(m_Transient);
            AliasSupport.SetApplicationForAliases(m_Registration, m_Transient);
        }

        public void Dispose() {
            if (m_Transient == null) return;

            m_Transient.Dispose();
            m_Transient = null;

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

    internal sealed class StaticTransientResolver<TAbstract, TConcrete> : IResolver, IDisposable, IConfigurable<TConcrete> {
        private readonly Registration m_Registration;
        private Func<TConcrete, TConcrete> m_Configuration = ConfigurationFactory.Empty<TConcrete>();
        private Func<TConcrete> m_Factory;

        private StaticTransient<TAbstract, TConcrete> m_Transient;

        internal StaticTransientResolver(Registration registration) {
            m_Registration = registration;
        }

        public void Resolve() {
            m_Transient = new StaticTransient<TAbstract, TConcrete>(m_Configuration, m_Factory);
            m_Registration.Resolver = m_Transient;
            ApplicationRegistry.Register(m_Registration);
            Application<TAbstract>.Set(m_Transient);
            AliasSupport.SetApplicationForAliases(m_Registration, m_Transient);
        }

        public void Dispose() {
            if (m_Transient == null) return;

            m_Transient.Dispose();
            m_Transient = null;
            AliasSupport.ClearApplicationForAliases(m_Registration);
            ApplicationRegistry.Remove(m_Registration);
            Application<TAbstract>.Set(null);
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
