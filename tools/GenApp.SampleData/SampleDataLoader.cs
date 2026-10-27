using System.Globalization;
using GenApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace GenApp.SampleData;

// Row counts of one table after the load. SampleRows counts only the keys of db2cre.jcl; AllRows also counts rows
// added through the API (numbers from 1000001 on).
internal sealed record TableCount(string Table, int Expected, int Inserted, int SampleRows, int AllRows)
{
    public bool Matches => SampleRows == Expected;
}

// Loads the sample rows into PostgreSQL. Table and column names come from the EF Core model of GenAppDbContext
// (docs/database-schema.md: same table names, column names in snake_case), so the tool and the API always agree on them.
internal sealed class SampleDataLoader(GenAppDbContext db)
{
    // #51: customers, policies and the four policy type tables, in foreign key order. customer_secure has sample rows
    // too but is not part of the task; claim has none (docs/database-schema.md, "claim tablosu").
    public static readonly IReadOnlyList<string> Tables = ["customer", "policy", "motor", "house", "endowment", "commercial"];

    public async Task<IReadOnlyList<TableCount>> LoadAsync(IReadOnlyList<Db2Insert> inserts, CancellationToken cancellationToken)
    {
        var rows = inserts
            .Where(insert => Tables.Contains(insert.Table))
            .Select(insert => (insert.Table, Entity: CreateEntity(insert)))
            .ToList();

        var inserted = Tables.ToDictionary(table => table, _ => 0);
        foreach (var (table, entity) in rows)
        {
            // A row whose number is already there is skipped, so running the tool again neither duplicates nor
            // changes anything. The API cannot create these numbers: its identity columns start at 1000001.
            if (await db.FindAsync(entity.GetType(), [KeyOf(entity)], cancellationToken) is null)
            {
                db.Add(entity);
                inserted[table]++;
            }
        }

        // One SaveChanges is one transaction: either all new rows are written or none (ADR 0003, decision 2).
        // EF Core orders the inserts by the foreign keys: customer, policy, then the type rows.
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var counts = new List<TableCount>();
        foreach (var table in Tables)
        {
            var keys = rows.Where(row => row.Table == table).Select(row => KeyOf(row.Entity)).ToArray();
            var entityType = EntityType(table);
            var keyColumn = KeyProperty(entityType).GetColumnName();

            var sampleRows = await CountAsync(
                $"SELECT count(*)::int AS \"Value\" FROM \"{table}\" WHERE \"{keyColumn}\" = ANY(@keys)",
                cancellationToken, new NpgsqlParameter("keys", keys));
            var allRows = await CountAsync($"SELECT count(*)::int AS \"Value\" FROM \"{table}\"", cancellationToken);

            counts.Add(new TableCount(table, keys.Length, inserted[table], sampleRows, allRows));
        }

        return counts;
    }

    private object CreateEntity(Db2Insert insert)
    {
        var entityType = EntityType(insert.Table);
        var entity = Activator.CreateInstance(entityType.ClrType)!;

        for (var i = 0; i < insert.Columns.Count; i++)
        {
            // Db2 column names match the C# property names apart from case (phonehome → PhoneHome).
            var property = entityType.GetProperties()
                .SingleOrDefault(p => p.Name.Equals(insert.Columns[i], StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"db2cre.jcl:{insert.Line}: {insert.Table}.{insert.Columns[i]} has no column in the new schema.");

            property.PropertyInfo!.SetValue(entity, Convert(property, insert.Values[i], insert.Line));
        }

        return entity;
    }

    // Text values are written as they are: no trimming, so ' ' and ' 12b' stay as in db2cre.jcl. The API trims the end
    // of texts when it answers (ADR 0002, decision 6).
    private static object Convert(IProperty property, string value, int line)
    {
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        var culture = CultureInfo.InvariantCulture;

        if (type == typeof(string)) return value;
        if (type == typeof(int)) return int.Parse(value, culture);
        if (type == typeof(short)) return short.Parse(value, culture);
        if (type == typeof(DateOnly)) return DateOnly.ParseExact(value, "yyyy-MM-dd", culture);
        // Kind stays Unspecified: Npgsql rejects Utc for "timestamp without time zone" (docs/database-schema.md).
        if (type == typeof(DateTime)) return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", culture);

        throw new NotSupportedException($"db2cre.jcl:{line}: no conversion for {property.Name} ({type.Name}).");
    }

    private IEntityType EntityType(string table) =>
        db.Model.GetEntityTypes().Single(e => e.GetTableName() == table);

    // All six tables have a single integer key: customer_number or policy_number.
    private static IProperty KeyProperty(IEntityType entityType) =>
        entityType.FindPrimaryKey()!.Properties.Single();

    private int KeyOf(object entity) =>
        (int)KeyProperty(db.Model.FindEntityType(entity.GetType())!).PropertyInfo!.GetValue(entity)!;

    private async Task<int> CountAsync(string sql, CancellationToken cancellationToken, params object[] parameters) =>
        (await db.Database.SqlQueryRaw<int>(sql, parameters).ToListAsync(cancellationToken)).Single();
}
