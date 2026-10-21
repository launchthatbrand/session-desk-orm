using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using SessionDesk.Orm;

namespace SessionDesk.Orm.NHibernate;

public sealed class CommitteeMap : ClassMapping<Committee>
{
    public CommitteeMap()
    {
        Table("committees");
        Id(committee => committee.Id, map => map.Generator(Generators.Assigned));
        Property(committee => committee.Name, map =>
        {
            map.Column("name");
            map.Length(200);
            map.NotNullable(true);
        });
    }
}

public sealed class BillMap : ClassMapping<Bill>
{
    public BillMap()
    {
        Table("bills");
        Id(bill => bill.Id, map => map.Generator(Generators.Assigned));
        Property(bill => bill.CommitteeId, map =>
        {
            map.Column("committee_id");
            map.NotNullable(true);
        });
        Property(bill => bill.Number, map =>
        {
            map.Column("number");
            map.Length(32);
            map.NotNullable(true);
        });
        Property(bill => bill.Title, map =>
        {
            map.Column("title");
            map.Length(400);
            map.NotNullable(true);
        });
        Property(bill => bill.Sponsor, map =>
        {
            map.Column("sponsor");
            map.Length(200);
            map.NotNullable(true);
        });
        Property(bill => bill.Status, map =>
        {
            map.Column("status");
            map.Length(64);
            map.NotNullable(true);
        });
        Bag(bill => bill.Actions, collection =>
        {
            collection.Table("bill_actions");
            collection.Key(key => key.Column("bill_id"));
            collection.Inverse(true);
            collection.Cascade(Cascade.None);
            collection.Lazy(CollectionLazy.Lazy);
        }, relation => relation.OneToMany());
        Bag(bill => bill.Notes, collection =>
        {
            collection.Table("notes");
            collection.Key(key => key.Column("bill_id"));
            collection.Inverse(true);
            collection.Cascade(Cascade.None);
            collection.Lazy(CollectionLazy.Lazy);
        }, relation => relation.OneToMany());
    }
}

public sealed class BillActionMap : ClassMapping<BillAction>
{
    public BillActionMap()
    {
        Table("bill_actions");
        Id(action => action.Id, map => map.Generator(Generators.Assigned));
        Property(action => action.BillId, map =>
        {
            map.Column("bill_id");
            map.NotNullable(true);
        });
        Property(action => action.Action, map =>
        {
            map.Column("action");
            map.Length(64);
            map.NotNullable(true);
        });
        Property(action => action.Note, map =>
        {
            map.Column("note");
            map.Length(200);
        });
        Property(action => action.CreatedAt, map =>
        {
            map.Column("created_at");
            map.NotNullable(true);
        });
    }
}

public sealed class NoteMap : ClassMapping<Note>
{
    public NoteMap()
    {
        Table("notes");
        Id(note => note.Id, map => map.Generator(Generators.Assigned));
        Property(note => note.BillId, map =>
        {
            map.Column("bill_id");
            map.NotNullable(true);
        });
        Property(note => note.Body, map =>
        {
            map.Column("body");
            map.Length(Note.MaxBodyLength);
            map.NotNullable(true);
        });
        Property(note => note.CreatedAt, map =>
        {
            map.Column("created_at");
            map.NotNullable(true);
        });
    }
}
