using Microsoft.EntityFrameworkCore;
using SessionDesk.Orm;

namespace SessionDesk.Orm.EntityFramework;

public sealed class EntityFrameworkBillRepository : IBillRepository
{
    private readonly string _connectionString;

    private EntityFrameworkBillRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static EntityFrameworkBillRepository Create(string connectionString)
    {
        var repository = new EntityFrameworkBillRepository(connectionString);
        using var db = repository.Open();
        db.Database.EnsureCreated();
        return repository;
    }

    public async Task<Bill?> GetBillWithActionsAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        await using var db = Open();
        return await db.Bills
            .Include(bill => bill.Actions)
            .Include(bill => bill.Notes)
            .SingleOrDefaultAsync(bill => bill.Id == billId, cancellationToken);
    }

    public async Task<IReadOnlyList<Bill>> ListBillsAsync(Guid committeeId, CancellationToken cancellationToken = default)
    {
        await using var db = Open();
        return await db.Bills
            .Where(bill => bill.CommitteeId == committeeId)
            .OrderBy(bill => bill.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task AddNoteAsync(Guid billId, string body, CancellationToken cancellationToken = default)
    {
        await using var db = Open();
        if (!await db.Bills.AnyAsync(bill => bill.Id == billId, cancellationToken))
        {
            throw new InvalidOperationException("Bill not found.");
        }

        db.Notes.Add(new Note
        {
            Id = Guid.NewGuid(),
            BillId = billId,
            Body = body,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordActionAsync(Guid billId, string action, string note, CancellationToken cancellationToken = default)
    {
        await using var db = Open();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await db.Bills.AnyAsync(bill => bill.Id == billId, cancellationToken))
            {
                throw new InvalidOperationException("Bill not found.");
            }

            var createdAt = DateTime.UtcNow;
            db.BillActions.Add(new BillAction
            {
                Id = Guid.NewGuid(),
                BillId = billId,
                Action = action,
                Note = note,
                CreatedAt = createdAt,
            });
            db.Notes.Add(new Note
            {
                Id = Guid.NewGuid(),
                BillId = billId,
                Body = note,
                CreatedAt = createdAt,
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private DeskDbContext Open()
    {
        var options = new DbContextOptionsBuilder<DeskDbContext>()
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new DeskDbContext(options);
    }
}
