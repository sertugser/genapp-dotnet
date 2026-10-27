namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// Poliçe türünün adres parçası (AD §2.2-2.5), <c>policy.policy_type</c> harfi (AD:136: konut <c>H</c>, motor <c>M</c>,
/// hayat <c>E</c>, ticari <c>C</c>) ve tür tablosu (DS:15-18).
/// </summary>
public sealed record PoliceTuru(string Yol, string Harf, string TurTablosu)
{
    public static readonly PoliceTuru Motor = new("motor-policies", "M", Sema.Motor.Tablo);
    public static readonly PoliceTuru Konut = new("house-policies", "H", Sema.House.Tablo);
    public static readonly PoliceTuru Hayat = new("endowment-policies", "E", Sema.Endowment.Tablo);
    public static readonly PoliceTuru Ticari = new("commercial-policies", "C", Sema.Commercial.Tablo);
}
