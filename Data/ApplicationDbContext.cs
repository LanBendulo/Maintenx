using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Models;

namespace IT15_Project.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        // MaintenX Business Tables
        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Personnel relationships
            builder.Entity<Personnel>()
                .HasOne(p => p.User)
                .WithOne(u => u.Personnel)
                .HasForeignKey<Personnel>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Work_Order relationships
            builder.Entity<WorkOrder>()
                .HasOne(w => w.Asset)
                .WithMany(a => a.WorkOrders)
                .HasForeignKey(w => w.AssetId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<WorkOrder>()
                .HasOne(w => w.AssignedToPersonnel)
                .WithMany(p => p.AssignedWorkOrders)
                .HasForeignKey(w => w.AssignedTo)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<WorkOrder>()
                .HasOne(w => w.CreatedByPersonnel)
                .WithMany(p => p.CreatedWorkOrders)
                .HasForeignKey(w => w.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Asset relationships
            builder.Entity<Asset>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Assets)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure MaintenanceRequest relationships
            builder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.Asset)
                .WithMany()
                .HasForeignKey(mr => mr.AssetId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.RequestedByPersonnel)
                .WithMany()
                .HasForeignKey(mr => mr.RequestedBy)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<MaintenanceRequest>()
                .HasOne(mr => mr.WorkOrder)
                .WithOne(wo => wo.MaintenanceRequest)
                .HasForeignKey<WorkOrder>(wo => wo.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

}
