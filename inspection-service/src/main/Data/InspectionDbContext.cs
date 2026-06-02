using FactoryPulse.Inspection.Models;
using Microsoft.EntityFrameworkCore;

namespace FactoryPulse.Inspection.Data;

public class InspectionDbContext : DbContext
{
    public InspectionDbContext(DbContextOptions<InspectionDbContext> options) : base(options) { }

    public DbSet<Equipment>  Equipment  { get; set; }
    public DbSet<Models.Inspection> Inspections { get; set; }
    public DbSet<AuditLog>   AuditLogs  { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Status).HasDefaultValue(EquipmentStatus.Online);
        });

        modelBuilder.Entity<Models.Inspection>(e =>
        {
            e.HasOne(x => x.Equipment)
             .WithMany(x => x.Inspections)
             .HasForeignKey(x => x.EquipmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.Timestamp);
        });
    }
}
