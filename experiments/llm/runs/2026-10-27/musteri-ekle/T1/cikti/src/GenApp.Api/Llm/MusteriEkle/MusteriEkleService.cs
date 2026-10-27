using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// LGACUS01 + LGACDB01 + LGACDB02 iş mantığı. LGACVS01'in (VSAM yazımı) karşılığı yoktur;
/// veri zaten customer tablosundadır. Tek bir PostgreSQL transaction'ı kullanılır.
/// </summary>
public sealed class MusteriEkleService
{
    // lgacdb01.cbl: MOVE '5732fec825535eeafb8fac50fee3a8aa' TO D2-CUSTSECR-PASS
    private const string DefaultPassword = "5732fec825535eeafb8fac50fee3a8aa";
    // lgacdb01.cbl: MOVE 'N' TO D2-CUSTSECR-STATE
    private const string DefaultState = "N";
    // lgacdb01.cbl: MOVE '0000' TO D2-CUSTSECR-COUNT
    private const int DefaultPassChanges = 0;

    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public MusteriEkleService(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MusteriEkleResult> AddAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.FirstName ?? "";
        var lastName = request.LastName ?? "";
        var dateOfBirth = request.DateOfBirth!.Value;
        var houseName = request.HouseName ?? "";
        var houseNumber = request.HouseNumber ?? "";
        // Posta kodu büyük harfe çevrilir (eskide lgtestc1.cbl:126-127, yenide API)
        var postcode = (request.Postcode ?? "").ToUpperInvariant();
        var phoneMobile = request.PhoneMobile ?? "";
        var phoneHome = request.PhoneHome ?? "";
        var email = request.Email ?? "";

        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");

        int customerNumber;

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            // INSERT-CUSTOMER: numara PostgreSQL identity ile üretilir (GENACUSTNUM sayacının karşılığı)
            await using (var insert = new NpgsqlCommand(
                """
                INSERT INTO customer
                    (first_name, last_name, date_of_birth, house_name, house_number,
                     postcode, phone_mobile, phone_home, email_address)
                VALUES
                    (@first_name, @last_name, @date_of_birth, @house_name, @house_number,
                     @postcode, @phone_mobile, @phone_home, @email_address)
                RETURNING customer_number
                """, connection, transaction))
            {
                insert.Parameters.Add(new NpgsqlParameter<string>("first_name", NpgsqlDbType.Varchar) { TypedValue = firstName });
                insert.Parameters.Add(new NpgsqlParameter<string>("last_name", NpgsqlDbType.Varchar) { TypedValue = lastName });
                insert.Parameters.Add(new NpgsqlParameter<DateOnly>("date_of_birth", NpgsqlDbType.Date) { TypedValue = dateOfBirth });
                insert.Parameters.Add(new NpgsqlParameter<string>("house_name", NpgsqlDbType.Varchar) { TypedValue = houseName });
                insert.Parameters.Add(new NpgsqlParameter<string>("house_number", NpgsqlDbType.Varchar) { TypedValue = houseNumber });
                insert.Parameters.Add(new NpgsqlParameter<string>("postcode", NpgsqlDbType.Varchar) { TypedValue = postcode });
                insert.Parameters.Add(new NpgsqlParameter<string>("phone_mobile", NpgsqlDbType.Varchar) { TypedValue = phoneMobile });
                insert.Parameters.Add(new NpgsqlParameter<string>("phone_home", NpgsqlDbType.Varchar) { TypedValue = phoneHome });
                insert.Parameters.Add(new NpgsqlParameter<string>("email_address", NpgsqlDbType.Varchar) { TypedValue = email });

                var scalar = await insert.ExecuteScalarAsync(cancellationToken);
                customerNumber = Convert.ToInt32(scalar);
            }

            // LGACDB02 (02ACUS): customer_secure satırı
            await transaction.SaveAsync("customer_secure", cancellationToken);
            try
            {
                await using var secure = new NpgsqlCommand(
                    """
                    INSERT INTO customer_secure
                        (customer_number, customer_pass, state_indicator, pass_changes)
                    VALUES
                        (@customer_number, @customer_pass, @state_indicator, @pass_changes)
                    """, connection, transaction);
                secure.Parameters.Add(new NpgsqlParameter<int>("customer_number", NpgsqlDbType.Integer) { TypedValue = customerNumber });
                secure.Parameters.Add(new NpgsqlParameter<string>("customer_pass", NpgsqlDbType.Varchar) { TypedValue = DefaultPassword });
                secure.Parameters.Add(new NpgsqlParameter<string>("state_indicator", NpgsqlDbType.Varchar) { TypedValue = DefaultState });
                secure.Parameters.Add(new NpgsqlParameter<int>("pass_changes", NpgsqlDbType.Integer) { TypedValue = DefaultPassChanges });
                await secure.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException ex) when (!IsBusy(ex))
            {
                // lgacdb02.cbl '98'i yalnızca D2-RETURN-CODE'a yazar; LGACDB01 bunu okumaz, çağırana '00' döner ve müşteri satırı kalır
                await transaction.RollbackAsync("customer_secure", cancellationToken);
                _logger.LogError(ex,
                    "LGACDB02 CNUM={CustomerNumber} INSERT SECURITY SQLSTATE={SqlState}",
                    customerNumber, ex.SqlState);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            _logger.LogWarning(ex, "LGACDB01 INSERT CUSTOMER: veritabanı kilitlenmesi ya da zaman aşımı");
            return new MusteriEkleResult(MusteriEkleOutcome.DatabaseBusy, null);
        }
        catch (NpgsqlException ex)
        {
            // lgacdb01.cbl INSERT-CUSTOMER: IF SQLCODE NOT EQUAL 0 → MOVE '90' TO CA-RETURN-CODE
            _logger.LogError(ex, "LGACDB01 INSERT CUSTOMER başarısız, dönüş kodu 90");
            return new MusteriEkleResult(MusteriEkleOutcome.DatabaseError, null);
        }

        var customer = new CustomerResponse
        {
            CustomerNumber = customerNumber,
            FirstName = firstName.TrimEnd(),
            LastName = lastName.TrimEnd(),
            DateOfBirth = dateOfBirth,
            HouseName = houseName.TrimEnd(),
            HouseNumber = houseNumber.TrimEnd(),
            Postcode = postcode.TrimEnd(),
            PhoneMobile = phoneMobile.TrimEnd(),
            PhoneHome = phoneHome.TrimEnd(),
            Email = email.TrimEnd(),
        };

        return new MusteriEkleResult(MusteriEkleOutcome.Success, customer);
    }

    private static bool IsBusy(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg &&
                pg.SqlState is PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.LockNotAvailable
                    or PostgresErrorCodes.QueryCanceled
                    or PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }

            if (current is TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
