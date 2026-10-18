using System.Runtime.CompilerServices;

// TruthWeaver.Benchmarks benchmarks YamlTreeParser.Parse directly, alongside
// JsonTreeParser.Parse on the equivalent rule text, to attribute how much of the Parse stage is the
// TreeFormatReader both front ends share versus their own cursor.
[assembly: InternalsVisibleTo("TruthWeaver.Benchmarks")]

// TruthWeaver.Yaml.Tests checks YamlNodeCursor directly, for behavior that the public parse API does not show, such as
// the out value of a failed constant read.
[assembly: InternalsVisibleTo("TruthWeaver.Yaml.Tests")]
