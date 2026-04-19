using System;
using System.Collections;
using System.Collections.Generic;

namespace Buttr.Core {
    public readonly struct RegistrationEnumerable<T> : IEnumerable<T> {
        private readonly List<Registration> m_Registrations;

        internal RegistrationEnumerable(List<Registration> registrations) {
            m_Registrations = registrations;
        }

        public Enumerator GetEnumerator() => new(m_Registrations);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T> {
            private readonly List<Registration> m_Registrations;
            private readonly Type m_Target;
            private int m_Index;
            private T m_Current;

            internal Enumerator(List<Registration> registrations) {
                m_Registrations = registrations;
                m_Target = typeof(T);
                m_Index = -1;
                m_Current = default;
            }

            public T Current => m_Current;
            object IEnumerator.Current => m_Current;

            public bool MoveNext() {
                if (m_Registrations == null) return false;

                while (++m_Index < m_Registrations.Count) {
                    var registration = m_Registrations[m_Index];
                    if (registration.IsHidden) continue;
                    if (m_Target.IsAssignableFrom(registration.ConcreteType) == false) continue;

                    m_Current = (T)registration.Resolver.Resolve();
                    return true;
                }

                m_Current = default;
                return false;
            }

            public void Reset() {
                m_Index = -1;
                m_Current = default;
            }

            public void Dispose() { }
        }
    }
}
