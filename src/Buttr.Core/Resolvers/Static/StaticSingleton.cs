using System;

namespace Buttr.Core {
    internal sealed class StaticSingleton<TConcrete> : ObjectResolverBase<TConcrete>, IDisposable{
        private readonly Func<TConcrete, TConcrete> m_Configuration;
        private readonly Func<TConcrete> m_FactoryOverride;
        private bool m_IsResolved;

        private TConcrete m_Instance;
        
        internal StaticSingleton(Func<TConcrete, TConcrete> configuration, Func<TConcrete> factoryOverride) : base(skipCtorScan: factoryOverride != null) {
            m_Configuration = configuration;
            m_FactoryOverride = factoryOverride;
            ApplicationRegistry.Register<TConcrete>(this);
        }

        public override bool IsResolved {
            get => m_IsResolved;
        }

        public override bool IsCached => true;
        
        public override object Resolve() {
            if (null != m_Instance) return m_Instance;

            if (m_FactoryOverride != null) {
                m_Instance = m_Configuration(m_FactoryOverride());
                m_IsResolved = true;
                return m_Instance;
            }

            var dependencies = ArrayPool<object>.Get(requirements.Length);
            if(requirements.Length > 0 ) {

                try {
                    ApplicationRegistry.GetDependencies(requirements, dependencies);
                }
                catch (Exception ex) {
                    var found = new System.Collections.Generic.List<string>();
                    var unresolved = new System.Collections.Generic.List<string>();
                    for (var i = 0; i < requirements.Length; i++) {
                        if (dependencies[i] != null) found.Add(requirements[i].FullName);
                        else unresolved.Add(requirements[i].FullName);
                    }

                    throw new ObjectResolverException(
                        $"Failed to resolve dependencies for {typeof(TConcrete)}. " +
                        $"Found: [{string.Join(", ", found)}]. " +
                        $"Unresolved: [{string.Join(", ", unresolved)}]. " +
                        $"See inner exception for the underlying failure.",
                        ex);
                }

                if (dependencies.TryValidate(requirements) == false) {
                    throw new ObjectResolverException($"Unable to locate all dependencies for {typeof(TConcrete)})");
                }
            }

            m_Instance = m_Configuration(factory(dependencies));

            ArrayPool<object>.Release(dependencies);
            m_IsResolved = true;
            return m_Instance;
        }

        public void Dispose() {
            ApplicationRegistry.Remove<TConcrete>();
        }
    }

    internal sealed class StaticSingleton<TAbstract, TConcrete> : ObjectResolverBase<TConcrete>, IDisposable {
        private readonly Func<TConcrete, TConcrete> m_Configuration;
        private readonly Func<TConcrete> m_FactoryOverride;
        private bool m_IsResolved;
        
        private TConcrete m_Instance;

        internal StaticSingleton(Func<TConcrete, TConcrete> configuration, Func<TConcrete> factoryOverride) : base(skipCtorScan: factoryOverride != null) {
            m_Configuration = configuration;
            m_FactoryOverride = factoryOverride;
            ApplicationRegistry.Register<TAbstract>(this);
        }

        public override bool IsResolved {
            get => m_IsResolved;
        }

        public override bool IsCached => true;

        public override object Resolve() {
            if (null != m_Instance) return m_Instance;

            if (m_FactoryOverride != null) {
                m_Instance = m_Configuration(m_FactoryOverride());
                m_IsResolved = true;
                return m_Instance;
            }

            var dependencies = ArrayPool<object>.Get(requirements.Length);

            if(requirements.Length > 0) {
                ApplicationRegistry.GetDependencies(requirements, dependencies);
                if (dependencies.TryValidate(requirements) == false) {
                    throw new ObjectResolverException($"Unable to locate all dependencies for {typeof(TConcrete)})");
                }
            }

            m_Instance = m_Configuration(factory(dependencies));

            m_IsResolved = true;
            ArrayPool<object>.Release(dependencies);
            return m_Instance;
        }

        public void Dispose() {
            ApplicationRegistry.Remove<TAbstract>();
        }
    }
}