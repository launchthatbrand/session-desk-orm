using Microsoft.EntityFrameworkCore;
using SessionDesk.Orm;

namespace SessionDesk.Orm.EntityFramework;

public sealed class DeskDbContext(DbContextOptions<DeskDbContext> options) : DbContext(options)
{
    public DbSet<Committee> Committees => Set<Committee>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillAction> BillActions => Set<BillAction>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Committee>(committee =>
        {
            committee.ToTable("committees");
            committee.HasKey(row => row.Id);
            committee.Property(row => row.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Bill>(bill =>
        {
            bill.ToTable("bills");
            bill.HasKey(row => row.Id);
            bill.Property(row => row.Number).HasMaxLength(32).IsRequired();
            bill.Property(row => row.Title).HasMaxLength(400).IsRequired();
            bill.Property(row => row.Sponsor).HasMaxLength(200).IsRequired();
            bill.Property(row => row.Status).HasMaxLength(64).IsRequired();
            bill.HasOne<Committee>()
                .WithMany()
                .HasForeignKey(row => row.CommitteeId)
                .OnDelete(DeleteBehavior.Restrict);
            bill.HasMany(row => row.Actions)
                .WithOne()
                .HasForeignKey(row => row.BillId)
                .OnDelete(DeleteBehavior.Cascade);
            bill.HasMany(row => row.Notes)
                .WithOne()
                .HasForeignKey(row => row.BillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BillAction>(action =>
        {
            action.ToTable("bill_actions");
            action.HasKey(row => row.Id);
            action.Property(row => row.Action).HasMaxLength(64).IsRequired();
            action.Property(row => row.Note).HasMaxLength(200);
            action.Property(row => row.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Note>(note =>
        {
            note.ToTable("notes");
            note.HasKey(row => row.Id);
            note.Property(row => row.Body).HasMaxLength(Note.MaxBodyLength).IsRequired();
            note.Property(row => row.CreatedAt).IsRequired();
        });
    }
}
