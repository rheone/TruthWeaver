namespace TruthWeaver.Analysis;

/// <summary>Which K3 values a tree can take over every assignment of its terms.</summary>
/// <param name="CanBeTrue">Some assignment makes the tree <c>True</c>.</param>
/// <param name="CanBeFalse">Some assignment makes the tree <c>False</c>.</param>
/// <param name="CanBeUnknown">Some assignment makes the tree <c>Unknown</c>.</param>
internal readonly record struct ValueProfile(bool CanBeTrue, bool CanBeFalse, bool CanBeUnknown);
