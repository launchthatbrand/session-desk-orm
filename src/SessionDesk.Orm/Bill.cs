namespace SessionDesk.Orm;

public class Bill
{
    public virtual Guid Id { get; set; }
    public virtual Guid CommitteeId { get; set; }
    public virtual string Number { get; set; } = "";
    public virtual string Title { get; set; } = "";
    public virtual string Sponsor { get; set; } = "";
    public virtual string Status { get; set; } = "referred";
    public virtual IList<BillAction> Actions { get; set; } = new List<BillAction>();
    public virtual IList<Note> Notes { get; set; } = new List<Note>();
}
