using Npgsql;
using SessionDesk.Orm;

namespace SessionDesk.Orm.Tests;

[Collection("postgres")]
public abstract class BillRepositoryTests : IAsyncLifetime
{
    public static readonly Guid AppropriationsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid EducationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AppropriationsBillId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid EducationBillId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly SemaphoreSlim SetupGate = new(1, 1);
    private static readonly Dictionary<string, IBillRepository> Ready = new();

    private readonly PostgresFixture _fixture;
    private readonly string _databaseName;

    protected BillRepositoryTests(PostgresFixture fixture, string databaseName)
    {
        _fixture = fixture;
        _databaseName = databaseName;
    }

    protected IBillRepository Repository { get; private set; } = null!;

    protected abstract IBillRepository CreateRepository(string connectionString);

    public async Task InitializeAsync()
    {
        await SetupGate.WaitAsync();
        try
        {
            if (Ready.TryGetValue(_databaseName, out var existing))
            {
                Repository = existing;
                return;
            }

            var connectionString = await _fixture.CreateDatabaseAsync(_databaseName);
            Repository = CreateRepository(connectionString);
            await SeedAsync(connectionString);
            Ready[_databaseName] = Repository;
        }
        finally
        {
            SetupGate.Release();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetBill_includes_actions()
    {
        var bill = await Repository.GetBillWithActionsAsync(AppropriationsBillId);

        Assert.NotNull(bill);
        Assert.Equal("SB 100", bill.Number);
        Assert.Contains(bill.Actions, action => action.Action == "bill.referred");
    }

    [Fact]
    public async Task ListBills_stays_inside_the_committee()
    {
        var bills = await Repository.ListBillsAsync(AppropriationsId);

        Assert.Equal([AppropriationsBillId], bills.Select(bill => bill.Id).ToArray());
    }

    [Fact]
    public async Task AddNote_persists_the_body()
    {
        await Repository.AddNoteAsync(AppropriationsBillId, "Staff asked for a fiscal note.");

        var bill = await Repository.GetBillWithActionsAsync(AppropriationsBillId);

        Assert.NotNull(bill);
        Assert.Contains(bill.Notes, note => note.Body == "Staff asked for a fiscal note.");
    }

    [Fact]
    public async Task RecordAction_rolls_back_the_note_when_the_write_fails()
    {
        var oversized = new string('x', Note.MaxBodyLength + 1);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            Repository.RecordActionAsync(AppropriationsBillId, "bill.note_added", oversized));

        var bill = await Repository.GetBillWithActionsAsync(AppropriationsBillId);
        Assert.NotNull(bill);
        Assert.DoesNotContain(bill.Actions, action => action.Action == "bill.note_added");
        Assert.DoesNotContain(bill.Notes, note => note.Body == oversized);
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO committees (id, name) VALUES
              (@appropriations, 'Appropriations'),
              (@education, 'Education');
            INSERT INTO bills (id, committee_id, number, title, sponsor, status) VALUES
              (@appropriationsBill, @appropriations, 'SB 100', 'Sample appropriations bill', 'Senator Example', 'referred'),
              (@educationBill, @education, 'SB 200', 'Sample education bill', 'Senator Example', 'referred');
            INSERT INTO bill_actions (id, bill_id, action, note, created_at) VALUES
              (@action, @appropriationsBill, 'bill.referred', 'Referred to Appropriations', TIMESTAMPTZ '2026-01-15 12:00:00+00');
            """,
            connection);
        command.Parameters.AddWithValue("appropriations", AppropriationsId);
        command.Parameters.AddWithValue("education", EducationId);
        command.Parameters.AddWithValue("appropriationsBill", AppropriationsBillId);
        command.Parameters.AddWithValue("educationBill", EducationBillId);
        command.Parameters.AddWithValue("action", Guid.Parse("55555555-5555-5555-5555-555555555555"));
        await command.ExecuteNonQueryAsync();
    }
}

public sealed class NHibernateBillRepositoryTests(PostgresFixture fixture)
    : BillRepositoryTests(fixture, "nh")
{
    protected override IBillRepository CreateRepository(string connectionString) =>
        SessionDesk.Orm.NHibernate.NHibernateBillRepository.Create(connectionString);
}

public sealed class EntityFrameworkBillRepositoryTests(PostgresFixture fixture)
    : BillRepositoryTests(fixture, "ef")
{
    protected override IBillRepository CreateRepository(string connectionString) =>
        SessionDesk.Orm.EntityFramework.EntityFrameworkBillRepository.Create(connectionString);
}
