namespace TruthWeaver.Generators.Tests;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// The value equality of the array that the generator pipeline uses to detect an unchanged model. The array type is
/// internal to the generator, so these tests reach it through reflection.
/// </summary>
public sealed class EquatableArrayTests
{
    /// <summary>Two arrays with the same elements are equal, have the same hash code and compare equal with the operators.</summary>
    [Fact]
    public void Equals_SameElementsInSameOrder_AreEqualWithEqualHashCodes_Test()
    {
        Subject first = Subject.Of("a", "b");
        Subject second = Subject.Of("a", "b");

        Assert.True(first.Equal(second));
        Assert.True(first.EqualsObject(second.Instance));
        Assert.True(first.OperatorEqual(second));
        Assert.False(first.OperatorNotEqual(second));
        Assert.Equal(first.HashCode(), second.HashCode());
    }

    /// <summary>Arrays that differ in an element, in order or in length are not equal.</summary>
    [Theory]
    [InlineData("a,c")]
    [InlineData("b,a")]
    [InlineData("a")]
    [InlineData("a,b,c")]
    public void Equals_DifferentElements_AreNotEqual_Test(string others)
    {
        Subject first = Subject.Of("a", "b");
        Subject second = Subject.Of(others.Split(','));

        Assert.False(first.Equal(second));
        Assert.False(first.EqualsObject(second.Instance));
        Assert.False(first.OperatorEqual(second));
        Assert.True(first.OperatorNotEqual(second));
    }

    /// <summary>An object that is not an array of the same type is not equal to the array.</summary>
    [Fact]
    public void Equals_ObjectOfAnotherType_IsNotEqual_Test()
    {
        Subject subject = Subject.Of("a");

        Assert.False(subject.EqualsObject("a"));
        Assert.False(subject.EqualsObject(null));
    }

    /// <summary>A default array has no elements, equals an empty array and can be enumerated.</summary>
    [Fact]
    public void Items_DefaultInstance_IsEmptyAndEqualToAnEmptyArray_Test()
    {
        Subject defaulted = Subject.Default();
        Subject empty = Subject.Of();

        Assert.Equal(0, defaulted.Count());
        Assert.True(defaulted.Equal(empty));
        Assert.Empty(defaulted.Enumerate());
    }

    /// <summary>Enumerating the array, typed or untyped, gives the elements in order, and the count is the number of elements.</summary>
    [Fact]
    public void GetEnumerator_Elements_AreGivenInOrder_Test()
    {
        Subject subject = Subject.Of("a", "b", "c");

        Assert.Equal(3, subject.Count());
        Assert.Equal(["a", "b", "c"], subject.Enumerate());
        Assert.Equal(["a", "b", "c"], subject.EnumerateUntyped());
    }

    /// <summary>One closed instance of the internal array type over strings, with its members.</summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "The test project runs on the full runtime; the generator type is internal and reachable only by reflection."
    )]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2055",
        Justification = "The test project runs on the full runtime; the generator type is internal and reachable only by reflection."
    )]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2077",
        Justification = "The test project runs on the full runtime; the generator type is internal and reachable only by reflection."
    )]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2080",
        Justification = "The test project runs on the full runtime; the generator type is internal and reachable only by reflection."
    )]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "The test project runs on the full runtime; the generator type is internal and reachable only by reflection."
    )]
    private sealed class Subject
    {
        private static readonly Type Closed = typeof(PredicateRegistrationGenerator)
            .Assembly.GetTypes()
            .Single(t => t.Name == "EquatableArray`1")
            .MakeGenericType(typeof(string));

        private Subject(object instance)
        {
            this.Instance = instance;
        }

        public object Instance { get; }

        public static Subject Of(params string[] items)
        {
            return new Subject(Activator.CreateInstance(Closed, ImmutableArray.Create(items))!);
        }

        public static Subject Default()
        {
            return new Subject(Activator.CreateInstance(Closed)!);
        }

        public bool Equal(Subject other)
        {
            return (bool)Closed.GetMethod("Equals", [Closed])!.Invoke(this.Instance, [other.Instance])!;
        }

        public bool EqualsObject(object? other)
        {
            return (bool)Closed.GetMethod("Equals", [typeof(object)])!.Invoke(this.Instance, [other])!;
        }

        public bool OperatorEqual(Subject other)
        {
            return (bool)Closed.GetMethod("op_Equality")!.Invoke(null, [this.Instance, other.Instance])!;
        }

        public bool OperatorNotEqual(Subject other)
        {
            return (bool)Closed.GetMethod("op_Inequality")!.Invoke(null, [this.Instance, other.Instance])!;
        }

        public int HashCode()
        {
            return (int)Closed.GetMethod("GetHashCode", Type.EmptyTypes)!.Invoke(this.Instance, null)!;
        }

        public int Count()
        {
            return (int)Closed.GetProperty("Count")!.GetValue(this.Instance)!;
        }

        public string[] Enumerate()
        {
            IEnumerable<string> typed = (IEnumerable<string>)this.Instance;
            return [.. typed];
        }

        public string[] EnumerateUntyped()
        {
            List<string> items = [];
            IEnumerator enumerator = ((IEnumerable)this.Instance).GetEnumerator();
            while (enumerator.MoveNext())
            {
                items.Add((string)enumerator.Current);
            }

            return [.. items];
        }
    }
}
