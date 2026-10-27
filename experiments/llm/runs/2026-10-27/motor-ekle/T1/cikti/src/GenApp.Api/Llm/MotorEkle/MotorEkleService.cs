namespace GenApp.Llm.MotorEkle;

/// <summary>
/// LGAPOL01 + LGAPDB01 (01AMOT) karşılığı. LGAPVS01'in VSAM yazımının karşılığı yoktur;
/// veri zaten PostgreSQL'e yazılır.
/// </summary>
public sealed class MotorEkleService
{
    private readonly MotorEkleRepository _repository;

    public MotorEkleService(MotorEkleRepository repository)
    {
        _repository = repository;
    }

    public async Task<MotorEkleResult> AddAsync(int customerNumber, MotorEkleRequest request, CancellationToken ct)
    {
        var data = new NewMotorPolicy(
            CustomerNumber: customerNumber,
            IssueDate: request.IssueDate!.Value,
            ExpiryDate: request.ExpiryDate!.Value,
            BrokerId: request.BrokerId,
            BrokersReference: request.BrokersReference ?? string.Empty,
            Payment: request.Payment,
            Make: request.Make ?? string.Empty,
            Model: request.Model ?? string.Empty,
            Value: request.Value,
            RegistrationNumber: request.RegistrationNumber ?? string.Empty,
            Colour: request.Colour ?? string.Empty,
            Cc: (short)request.Cc,
            ManufactureDate: request.ManufactureDate!.Value,
            Premium: request.Premium,
            Accidents: request.Accidents);

        return await _repository.InsertAsync(data, ct);
    }
}

public sealed record NewMotorPolicy(
    int CustomerNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    long BrokerId,
    string BrokersReference,
    int Payment,
    string Make,
    string Model,
    int Value,
    string RegistrationNumber,
    string Colour,
    short Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);
