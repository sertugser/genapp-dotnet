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

        // LGAPDB01 writes the dates to COMMERCIAL as well: START = issue date, RENEWAL = expiry date and REQUESTDATE =
        // the new POLICY.LASTCHANGED (lgapdb01.cbl:308-321, :520-560; legacy-analysis/use-cases/16-commercial-add.md,
        // note 2). The inquiries read them from there.
        if (details is CommercialPolicy commercial)
        {
            commercial.StartDate = policy.IssueDate;
            commercial.RenewalDate = policy.ExpiryDate;
            commercial.RequestDate = policy.LastChanged;
        }

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

    // The three other commercial inquiries. Read only, so nothing is tracked. The inner join with COMMERCIAL is the
    // legacy type check (17-commercial-inquire.md, note 7); the service checks POLICYTYPE as well. No ORDER BY, as in
    // the legacy cursors; the 21-row limit of 03ICOM (lgipdb01.cbl:906-909) is not defined for the new system.
    public async Task<PolicyRecord<CommercialPolicy>?> FindCommercialAsync(int policyNumber, CancellationToken cancellationToken = default)
    {
        var list = await QueryCommercialAsync(c => c.PolicyNumber == policyNumber, cancellationToken);
        return list.SingleOrDefault();
    }

    public Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> FindCommercialByCustomerAsync(
        int customerNumber, CancellationToken cancellationToken = default) =>
        QueryCommercialAsync(c => c.Policy!.CustomerNumber == customerNumber, cancellationToken);

    // The postcode is compared exactly, as ZIPCODE = CA-B-Postcode (17-commercial-inquire.md, note 14).
    public Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> FindCommercialByPostcodeAsync(
        string postcode, CancellationToken cancellationToken = default) =>
        QueryCommercialAsync(c => c.Zipcode == postcode, cancellationToken);

    private async Task<IReadOnlyList<PolicyRecord<CommercialPolicy>>> QueryCommercialAsync(
        System.Linq.Expressions.Expression<Func<CommercialPolicy, bool>> filter, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await db.CommercialPolicies
                .AsNoTracking()
                .Include(c => c.Policy)
                .Where(filter)
                .ToListAsync(cancellationToken);
            return rows.Select(c => new PolicyRecord<CommercialPolicy>(c.Policy!, c)).ToList();
        }
        catch (Exception e) when (DatabaseErrors.IsDatabaseError(e))
        {
            // A failed FETCH: 500 / 90, or 503 on deadlock and timeout (17-commercial-inquire.md, notes 5 and 6).
            throw DatabaseErrors.Translate(e);
        }
    }

    // Reads only the type column; nothing is tracked.
    public Task<string?> FindTypeAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default) =>
        db.Policies
            .Where(p => p.CustomerNumber == customerNumber && p.PolicyNumber == policyNumber)
            .Select(p => p.PolicyType)
            .SingleOrDefaultAsync(cancellationToken);

    // LGUPDB01 (lgupdb01.cbl:278, :322): EF Core sends UPDATE ... WHERE last_changed = <client's value>.
    // Without setting OriginalValue, EF Core would compare against the value it just read and never find a conflict.
    public void MarkUpdated(Policy policy, DateTime lastChangedFromClient)
    {
        var lastChanged = db.Entry(policy).Property(p => p.LastChanged);
        lastChanged.OriginalValue = lastChangedFromClient;
        lastChanged.CurrentValue = Now();
    }

    // LGDPDB01 (lgdpdb01.cbl:189-199): one DELETE on policy; ON DELETE CASCADE removes the type row. The comment at
    // :196-197 says a missing policy counts as success, but the code at :198-199 returns 90 for SQLCODE 100. The new
    // system keeps the code's behaviour (docs/api-design.md section 3), so the deleted row count is returned and
    // the service maps 0 to 90. ExecuteDeleteAsync runs at once: a delete is the operation's only write.
    public Task<int> DeleteAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken = default) =>
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
