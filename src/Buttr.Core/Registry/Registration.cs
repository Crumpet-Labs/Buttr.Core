using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal sealed class Registration {
        internal IObjectResolver Resolver { get; set; }
        internal Type PrimaryKey { get; }
        internal Type ConcreteType { get; }
        internal bool IsHidden { get; }
        internal HashSet<Type> Aliases { get; private set; }

        internal Registration(Type primaryKey, Type concreteType, bool isHidden) {
            PrimaryKey = primaryKey;
            ConcreteType = concreteType;
            IsHidden = isHidden;
        }

        internal void AddAlias(Type aliasType) {
            Aliases ??= new HashSet<Type>();
            Aliases.Add(aliasType);
        }
    }
}
