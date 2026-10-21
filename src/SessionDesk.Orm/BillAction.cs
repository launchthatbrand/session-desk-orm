namespace SessionDesk.Orm;

public class BillAction
{
    public virtual Guid Id { get; set; }
    public virtual Guid BillId { get; set; }
    public virtual string Action { get; set; } = "";
    public virtual string? Note { get; set; }
    public virtual DateTime CreatedAt { get; set; }
}
