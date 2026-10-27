using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// LGICDB01 karşılığı: customer tablosundan tek müşteri okur.
/// </summary>
public sealed class MusteriSorgulaRepository
{
    private const string Sql = """
        SELECT first_name,
               last_name,
               date_of_birth,
               house_name,
               house_number,
               postcode,
               phone_mobile,
               phone_home,
               email_address
          FROM customer
         WHERE customer_number = @customerNumber
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<MusteriSorgulaRepository> _logger;

    public MusteriSorgulaRepository(IConfiguration configuration, ILogger<MusteriSorgulaRepository> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MusteriSorgulaResult> GetCustomerAsync(int customerNumber, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(Sql, connection);
            command.Parameters.AddWithValue("customerNumber", customerNumber);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                // SQLCODE 100 -> '01'
                return MusteriSorgulaResult.NotFound();
            }

            var customer = new MusteriSorgulaResponse
            {
                CustomerNumber = customerNumber,
                FirstName = ReadText(reader, 0),
                LastName = ReadText(reader, 1),
                DateOfBirth = reader.IsDBNull(2) ? null : reader.GetFieldValue<DateOnly>(2),
                HouseName = ReadText(reader, 3),
                HouseNumber = ReadText(reader, 4),
                Postcode = ReadText(reader, 5),
                PhoneMobile = ReadText(reader, 6),
                PhoneHome = ReadText(reader, 7),
                Email = ReadText(reader, 8),
            };

            // SQLCODE 0 -> '00'
            return MusteriSorgulaResult.Found(customer);
        }
        catch (PostgresException ex) when (IsBusy(ex.SqlState))
        {
            // Eskide -913 -> '01' (lgicdb01.cbl); API tasarımı Karar 8: 503 + "90".
            _logger.LogWarning(ex, "LGICUS01 CNUM={CustomerNumber} veritabanı meşgul SQLSTATE={SqlState}",
                customerNumber, ex.SqlState);
            return MusteriSorgulaResult.Busy();
        }
        catch (NpgsqlException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning(ex, "LGICUS01 CNUM={CustomerNumber} veritabanı zaman aşımı", customerNumber);
            return MusteriSorgulaResult.Busy();
        }
        catch (NpgsqlException ex)
        {
            // When Other -> '90' + WRITE-ERROR-MESSAGE (LGSTSQ yerine ILogger)
            _logger.LogError(ex, "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER veritabanı hatası", customerNumber);
            return MusteriSorgulaResult.DatabaseError();
        }
    }

    private static bool IsBusy(string sqlState) => sqlState is
        PostgresErrorCodes.DeadlockDetected or
        PostgresErrorCodes.LockNotAvailable or
        PostgresErrorCodes.QueryCanceled or
        PostgresErrorCodes.SerializationFailure;

    // Boş metin "" döner, null dönmez; sondaki boşluklar kırpılır (API tasarımı, 1).
    private static string ReadText(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).TrimEnd();
}
