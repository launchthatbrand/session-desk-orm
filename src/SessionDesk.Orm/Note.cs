namespace SessionDesk.Orm;

public class Note
{
    public const int MaxBodyLength = 200;

    public virtual Guid Id { get; set; }
    public virtual Guid BillId { get; set; }
    public virtual string Body { get; set; } = "";
    public virtual DateTime CreatedAt { get; set; }
}
