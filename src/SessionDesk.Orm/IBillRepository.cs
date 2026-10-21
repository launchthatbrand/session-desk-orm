namespace SessionDesk.Orm;

public interface IBillRepository
{
    Task<Bill?> GetBillWithActionsAsync(Guid billId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bill>> ListBillsAsync(Guid committeeId, CancellationToken cancellationToken = default);
    Task AddNoteAsync(Guid billId, string body, CancellationToken cancellationToken = default);
    Task RecordActionAsync(Guid billId, string action, string note, CancellationToken cancellationToken = default);
}
