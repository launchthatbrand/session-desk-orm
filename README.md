# Session Desk ORM

Same bill repository, two ORMs, one Postgres database. This is the data-layer comparison for [Session Desk](https://session-desk.apps.onpoint.host). The live desk uses Entity Framework Core only. Nothing here is deployed.

`IBillRepository` loads a bill with its actions, lists bills for one committee, adds a note, and records an action together with a note. `SessionDesk.Orm.NHibernate` and `SessionDesk.Orm.EntityFramework` each implement that interface. xUnit runs the same four cases against both, with Postgres started by Testcontainers.

## What moved

NHibernate keeps an `ISession` open for one operation and commits an `ITransaction`. The bill's action and note collections are lazy bags. The child row owns `bill_id`, and the collection is inverse, so adding an action is a save of `BillAction`, not a mutation of the parent's bag. `GetBillWithActionsAsync` calls `NHibernateUtil.Initialize` before the session closes. Without that, reading `Actions` later would throw because the proxy has nowhere to load from.

Entity Framework Core uses a `DbContext` for the same operation. `Include` is the explicit load. `HasMany` with `HasForeignKey(action => action.BillId)` is the same owning side as the inverse bag. `RecordActionAsync` begins a transaction, adds the action and the note, and commits. A note body longer than 200 characters fails the column check. The transaction rolls back, so the action is not left behind either.

Schema comes from each mapper. NHibernate runs `SchemaExport` from mapping-by-code. EF Core runs `EnsureCreated` from the fluent model, with snake_case names. The tests then insert the same two fictional bills with SQL, which only works if those two schemas agree.

## Run

Docker has to be running.

```bash
dotnet test
```
