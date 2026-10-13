using GenApp.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GenApp.Infrastructure.Persistence;

// Translates EF Core and PostgreSQL errors into Application exceptions so no EF Core type leaks upwards (ADR 0001).
internal sealed class UnitOfWork(GenAppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException e)
        {
            throw new ConcurrencyConflictException("The record was changed by someone else after it was read.", e);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } pg)
        {
            throw new ReferencedRecordNotFoundException($"Referenced record not found ({pg.ConstraintName}).", e);
        }
    }
}
