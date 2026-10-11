namespace TruthWeaver.Tests;

using System.Reflection;

/// <summary>
/// Replaces <see cref="TimeZoneInfo.Local"/> for the life of the returned scope. The runtime exposes no setter, so the
/// cached local zone is swapped by reflection; the test fails loudly when the runtime layout changes.
/// </summary>
internal static class HostTimeZone
{
    /// <summary>Makes the zone with the given id the host's local zone until the scope is disposed.</summary>
    /// <param name="zoneId">A system time-zone id.</param>
    /// <returns>A scope that restores the previous zone.</returns>
    public static IDisposable Use(string zoneId)
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        TimeZoneInfo previous = TimeZoneInfo.Local;
        SetLocal(zone);
        Assert.Equal(zoneId, TimeZoneInfo.Local.Id);
        return new Scope(previous);
    }

    private static void SetLocal(TimeZoneInfo zone)
    {
        FieldInfo cachedField =
            typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TimeZoneInfo.s_cachedData is missing.");
        object cached = cachedField.GetValue(null)!;
        FieldInfo localField =
            cached.GetType().GetField("_localTimeZone", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CachedData._localTimeZone is missing.");
        localField.SetValue(cached, zone);
    }

    private sealed class Scope(TimeZoneInfo previous) : IDisposable
    {
        public void Dispose()
        {
            SetLocal(previous);
        }
    }
}
