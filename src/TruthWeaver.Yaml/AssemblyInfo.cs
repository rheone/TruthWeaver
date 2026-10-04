using System.Runtime.CompilerServices;

// TruthWeaver.Benchmarks (k3-hardening ticket 18) benchmarks YamlTreeParser.Parse directly, alongside
// JsonTreeParser.Parse on the equivalent rule text, to attribute how much of the Parse stage is the
// TreeFormatReader both front ends share versus their own cursor.
[assembly: InternalsVisibleTo("TruthWeaver.Benchmarks")]
