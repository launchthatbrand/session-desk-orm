using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using SessionDesk.Orm;

namespace SessionDesk.Orm.NHibernate;

public sealed class NHibernateBillRepository : IBillRepository
{
    private readonly ISessionFactory _sessions;

    private NHibernateBillRepository(ISessionFactory sessions)
    {
        _sessions = sessions;
    }

    public static NHibernateBillRepository Create(string connectionString)
    {
        var mapper = new ModelMapper();
        mapper.AddMapping<CommitteeMap>();
        mapper.AddMapping<BillMap>();
        mapper.AddMapping<BillActionMap>();
        mapper.AddMapping<NoteMap>();
        HbmMapping mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

        var configuration = new Configuration();
        configuration.DataBaseIntegration(db =>
        {
            db.ConnectionString = connectionString;
            db.Dialect<PostgreSQL83Dialect>();
            db.Driver<NpgsqlDriver>();
        });
        configuration.AddMapping(mapping);

        new SchemaExport(configuration).Create(useStdOut: false, execute: true);

        return new NHibernateBillRepository(configuration.BuildSessionFactory());
    }

    public Task<Bill?> GetBillWithActionsAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        using var session = _sessions.OpenSession();
        var bill = session.Get<Bill>(billId);
        if (bill is null)
        {
            return Task.FromResult<Bill?>(null);
        }

        NHibernateUtil.Initialize(bill.Actions);
        NHibernateUtil.Initialize(bill.Notes);
        return Task.FromResult<Bill?>(bill);
    }

    public Task<IReadOnlyList<Bill>> ListBillsAsync(Guid committeeId, CancellationToken cancellationToken = default)
    {
        using var session = _sessions.OpenSession();
        IReadOnlyList<Bill> bills = session.Query<Bill>()
            .Where(bill => bill.CommitteeId == committeeId)
            .OrderBy(bill => bill.Number)
            .ToList();
        return Task.FromResult(bills);
    }

    public async Task AddNoteAsync(Guid billId, string body, CancellationToken cancellationToken = default)
    {
        using var session = _sessions.OpenSession();
        using var transaction = session.BeginTransaction();
        if (session.Get<Bill>(billId) is null)
        {
            throw new InvalidOperationException("Bill not found.");
        }

        await session.SaveAsync(new Note
        {
            Id = Guid.NewGuid(),
            BillId = billId,
            Body = body,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RecordActionAsync(Guid billId, string action, string note, CancellationToken cancellationToken = default)
    {
        using var session = _sessions.OpenSession();
        using var transaction = session.BeginTransaction();
        try
        {
            if (session.Get<Bill>(billId) is null)
            {
                throw new InvalidOperationException("Bill not found.");
            }

            await session.SaveAsync(new BillAction
            {
                Id = Guid.NewGuid(),
                BillId = billId,
                Action = action,
                Note = note,
                CreatedAt = DateTime.UtcNow,
            }, cancellationToken);
            await session.SaveAsync(new Note
            {
                Id = Guid.NewGuid(),
                BillId = billId,
                Body = note,
                CreatedAt = DateTime.UtcNow,
            }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
