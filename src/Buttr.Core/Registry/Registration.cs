using System;

namespace Buttr.Core {
    internal sealed class Registration {
        internal IObjectResolver Resolver { get; set; }
        internal Type PrimaryKey { get; }
        internal Type ConcreteType { get; }
        internal bool IsHidden { get; }

        internal Registration(Type primaryKey, Type concreteType, bool isHidden) {
            PrimaryKey = primaryKey;
            ConcreteType = concreteType;
            IsHidden = isHidden;
        }
    }
}
