namespace GenApp.Infrastructure.Tests.Altyapi;

/// <summary>
/// Hep aynı anı döndüren saat. Saat dilimi UTC'dir, böylece yerel saat <see cref="Simdi"/>'nin kendisidir.
/// </summary>
public sealed class SabitSaat(DateTime simdi) : TimeProvider
{
    public DateTime Simdi { get; } = simdi;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Simdi, DateTimeKind.Utc));
}
