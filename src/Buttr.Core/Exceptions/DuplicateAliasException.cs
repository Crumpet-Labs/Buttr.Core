using System;

namespace Buttr.Core {
    public sealed class DuplicateAliasException : Exception {
        public DuplicateAliasException(string message) : base(message) { }
        public DuplicateAliasException(string message, Exception innerException) : base(message, innerException) { }
    }
}
