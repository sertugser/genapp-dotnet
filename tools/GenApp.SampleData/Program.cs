using GenApp.Infrastructure;
using GenApp.Infrastructure.Persistence;
using GenApp.SampleData;
using Microsoft.Extensions.DependencyInjection;

// Loads the Db2 sample data of base/cntl/db2cre.jcl into PostgreSQL and checks the row counts (#51).
//   ConnectionStrings__GenApp=<connection string> dotnet run --project tools/GenApp.SampleData [-- --jcl <path>]
// The schema must exist (EF Core migrations). The exit code is 1 if a count does not match db2cre.jcl.

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__GenApp")
    ?? throw new InvalidOperationException("Connection string 'GenApp' is not configured (ConnectionStrings__GenApp).");
var jclPath = JclPath(args);

var inserts = Db2InsertReader.Read(jclPath);

// Same DbContext configuration as the API (Npgsql, snake_case names).
await using var services = new ServiceCollection().AddInfrastructure(connectionString).BuildServiceProvider();
await using var scope = services.CreateAsyncScope();
var loader = new SampleDataLoader(scope.ServiceProvider.GetRequiredService<GenAppDbContext>());

var counts = await loader.LoadAsync(inserts, CancellationToken.None);

Console.WriteLine($"Kaynak: {Path.GetFullPath(jclPath)}");
Console.WriteLine();
Console.WriteLine("| Tablo | db2cre.jcl | Bu çalıştırmada eklenen | Veritabanında (örnek numaralar) | Tablodaki toplam | Sonuç |");
Console.WriteLine("|---|---:|---:|---:|---:|---|");
foreach (var count in counts)
{
    Console.WriteLine(
        $"| `{count.Table}` | {count.Expected} | {count.Inserted} | {count.SampleRows} | {count.AllRows} | " +
        $"{(count.Matches ? "aynı" : "FARKLI")} |");
}

foreach (var skipped in inserts.Where(i => !SampleDataLoader.Tables.Contains(i.Table)).GroupBy(i => i.Table))
{
    Console.WriteLine();
    Console.WriteLine($"`{skipped.Key}`: db2cre.jcl'de {skipped.Count()} satır var; #51'in kapsamında olmadığı için yüklenmedi.");
}

return counts.All(count => count.Matches) ? 0 : 1;

// --jcl <path>, or base/cntl/db2cre.jcl in the current directory or one of its parents (the repository root).
static string JclPath(string[] args)
{
    var index = Array.IndexOf(args, "--jcl");
    if (index >= 0 && index + 1 < args.Length)
    {
        return args[index + 1];
    }

    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, "base", "cntl", "db2cre.jcl");
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new FileNotFoundException("base/cntl/db2cre.jcl was not found; give its path with --jcl.");
}
