using Microsoft.EntityFrameworkCore;
using MsfsFailures.Data.Entities;

namespace MsfsFailures.Data;

public class FleetDbContext : DbContext
{
    public FleetDbContext(DbContextOptions<FleetDbContext> options) : base(options) { }

    public DbSet<Airframe> Airframes => Set<Airframe>();
    public DbSet<ModelRef> ModelRefs => Set<ModelRef>();
    public DbSet<ComponentTemplate> ComponentTemplates => Set<ComponentTemplate>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Consumable> Consumables => Set<Consumable>();
    public DbSet<Accelerator> Accelerators => Set<Accelerator>();
    public DbSet<FailureMode> FailureModes => Set<FailureMode>();
    public DbSet<Squawk> Squawks => Set<Squawk>();
    public DbSet<MaintenanceAction> MaintenanceActions => Set<MaintenanceAction>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<VarBinding> VarBindings => Set<VarBinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- ModelRef ---
        modelBuilder.Entity<ModelRef>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Manufacturer).IsRequired();
            e.Property(x => x.SimMatchRulesJson).IsRequired();
        });

        // --- Airframe ---
        modelBuilder.Entity<Airframe>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Tail).IsRequired();
            e.HasIndex(x => x.Tail).IsUnique();
            e.Property(x => x.Type).IsRequired();

            e.HasOne(x => x.ModelRef)
             .WithMany()
             .HasForeignKey(x => x.ModelRefId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ComponentTemplate ---
        modelBuilder.Entity<ComponentTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.WearCurveJson).IsRequired();

            e.HasOne(x => x.ModelRef)
             .WithMany(m => m.ComponentTemplates)
             .HasForeignKey(x => x.ModelRefId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Component ---
        modelBuilder.Entity<Component>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Condition).IsRequired();

            e.HasOne(x => x.Airframe)
             .WithMany(a => a.Components)
             .HasForeignKey(x => x.AirframeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Template)
             .WithMany(t => t.Components)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Consumable ---
        modelBuilder.Entity<Consumable>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasOne(x => x.Airframe)
             .WithMany(a => a.Consumables)
             .HasForeignKey(x => x.AirframeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Accelerator ---
        modelBuilder.Entity<Accelerator>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Variable).IsRequired();
            e.Property(x => x.FormulaJson).IsRequired();
            e.HasIndex(x => x.Category);
        });

        // --- FailureMode ---
        modelBuilder.Entity<FailureMode>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.SimBindingPayload).IsRequired();

            e.HasOne(x => x.Template)
             .WithMany(t => t.FailureModes)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Squawk ---
        modelBuilder.Entity<Squawk>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).IsRequired();

            e.HasOne(x => x.Airframe)
             .WithMany(a => a.Squawks)
             .HasForeignKey(x => x.AirframeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.FailureMode)
             .WithMany(f => f.Squawks)
             .HasForeignKey(x => x.FailureModeId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.AirframeId, x.Status });
        });

        // --- MaintenanceAction ---
        modelBuilder.Entity<MaintenanceAction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).IsRequired();
            e.Property(x => x.ComponentsTouchedJson).IsRequired();
            e.Property(x => x.Notes).IsRequired();

            e.HasOne(x => x.Airframe)
             .WithMany(a => a.MaintenanceActions)
             .HasForeignKey(x => x.AirframeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.AirframeId);
        });

        // --- Session ---
        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OvertempEventsJson).IsRequired();

            e.HasOne(x => x.Airframe)
             .WithMany(a => a.Sessions)
             .HasForeignKey(x => x.AirframeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.AirframeId);
        });

        // --- VarBinding ---
        modelBuilder.Entity<VarBinding>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.LogicalName).IsRequired();
            e.Property(x => x.Expression).IsRequired();

            e.HasOne(x => x.ModelRef)
             .WithMany(m => m.VarBindings)
             .HasForeignKey(x => x.ModelRefId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ModelRefId, x.LogicalName });
        });
    }
}
