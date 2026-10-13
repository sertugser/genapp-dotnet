using GenApp.Application.Policies;
using GenApp.Domain.Policies;
using Microsoft.EntityFrameworkCore;

namespace GenApp.Infrastructure.Persistence;

// Replaces the Db2 policy programs LGAPDB01, LGIPDB01, LGUPDB01 and LGDPDB01.
internal sealed class PolicyRepository(GenAppDbContext db, TimeProvider clock) : IPolicyRepository
{
    public void Add(Policy policy, IPolicyDetails details)
    {
        policy.PolicyType = details switch   // lgapdb01.cbl:188-200
        {
            EndowmentPolicy => "E",
            HousePolicy => "H",
            MotorPolicy => "M",
            CommercialPolicy => "C",
            _ => throw new ArgumentException($"Unknown policy type {details.GetType().Name}.", nameof(details)),
        };
        policy.LastChanged = Now();

        // Through the navigation EF Core inserts the policy first and copies its generated number to the type row,
        // all in one SaveChanges (one transaction).
        details.Policy = policy;
        db.Add(details);
    }

    // LGIPDB01: FROM POLICY, <type table> WHERE customer number AND policy number match. The rows stay tracked,
    // so an update can change them and save.
    public async Task<PolicyRecord<TDetails>?> FindAsync<TDetails>(int customerNumber, int policyNumber, CancellationToken cancellationToken = default)
        where TDetails : class, IPolicyDetails
    {
        var details = await db.Set<TDetails>()
            .Include(d => d.Policy)
            .SingleOrDefaultAsync(d => d.PolicyNumber == policyNumber && d.Policy!.CustomerNumber == customerNumber, cancellationToken);

        return details is null ? null : new PolicyRecord<TDetails>(details.Policy!, details);
    }

    // LGUPDB01 (lgupdb01.cbl:278, :322): EF Core sends UPDATE ... WHERE last_changed = <client's value>.
    // Without setting OriginalValue, EF Core would compare against the value it just read and never find a conflict.
    public void MarkUpdated(Policy policy, DateTime lastChangedFromClient)
    {
        var lastChanged = db.Entry(policy).Property(p => p.LastChanged);
        lastChanged.OriginalValue = lastChangedFromClient;
        lastChanged.CurrentValue = Now();
    }

    // LGDPDB01 (lgdpdb01.cbl:189-197): one DELETE on policy; ON DELETE CASCADE removes the type row; deleting a
    // missing policy is not an error. ExecuteDeleteAsync runs at once: a delete is the operation's only write.
    public Task DeleteAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default) =>
        db.Policies
            .Where(p => p.CustomerNumber == customerNumber && p.PolicyNumber == policyNumber)
            .ExecuteDeleteAsync(cancellationToken);

    // ADR 0003 decision 3: the application is the only clock; PostgreSQL keeps microseconds and Npgsql
    // rejects DateTimeKind.Utc for "timestamp without time zone".
    private DateTime Now()
    {
        var now = clock.GetLocalNow().DateTime;
        return new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Unspecified);
    }
}
