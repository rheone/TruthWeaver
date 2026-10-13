using System.Runtime.CompilerServices;

// TruthWeaver.Predicates.Tests pins the size bound of the internal regular-expression pattern cache. The bound has no
// public surface, because a host never needs to read or change it.
[assembly: InternalsVisibleTo("TruthWeaver.Predicates.Tests")]
