using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Models;
using IT15_Project.Services;
using System.Security.Claims;

namespace IT15_Project.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // MaintenX Business Tables
        public DbSet<Company> Companies { get; set; }
        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<PreventiveSchedule> PreventiveSchedules { get; set; }
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<WorkOrderPart> WorkOrderParts { get; set; }
        public DbSet<WorkOrderCost> WorkOrderCosts { get; set; }

        /// <summary>
        /// Gets the current user's CompanyId from HttpContext
        /// Returns null if user is not authenticated or CompanyId not found
        /// </summary>
        private int? GetCurrentCompanyId()
        {
            if (_httpContextAccessor?.HttpContext?.User == null)
                return null;

            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = Users.Local.FirstOrDefault(u => u.Id == userId);
            if (user != null)
                return user.CompanyId;

            // If not in local cache, query database
            var userFromDb = Users.FirstOrDefault(u => u.Id == userId);
            return userFromDb?.CompanyId;
        }

        /// <summary>
        /// Override SaveChanges to automatically set CompanyId on new entities
        /// </summary>
        public override int SaveChanges()
        {
            SetCompanyIdOnNewEntities();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to automatically set CompanyId on new entities
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetCompanyIdOnNewEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Automatically sets CompanyId on new entities that have a CompanyId property
        /// </summary>
        private void SetCompanyIdOnNewEntities()
        {
            var currentCompanyId = GetCurrentCompanyId();
            if (!currentCompanyId.HasValue)
                return; // Skip if no user context (e.g., during seeding)

            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                // Skip Company entity itself (it doesn't need CompanyId set)
                if (entry.Entity is Company)
                    continue;

                // Check if entity has CompanyId property
                var companyIdProperty = entry.Entity.GetType().GetProperty("CompanyId");
                if (companyIdProperty != null && companyIdProperty.CanWrite)
                {
                    var currentValue = companyIdProperty.GetValue(entry.Entity);
                    
                    // Only set if not already set (allows manual override if needed)
                    if (currentValue == null || (int)currentValue == 0)
                    {
                        companyIdProperty.SetValue(entry.Entity, currentCompanyId.Value);
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ============================================================
            // NOTE: Global Query Filters removed - they don't work well
            // with per-request tenant context. Instead, we rely on:
            // 1. Explicit filtering in controllers (best practice)
            // 2. Automatic CompanyId assignment in SaveChanges
            // 3. Foreign key constraints (after migration)
            // ============================================================

            // ============================================================
            // ENTITY RELATIONSHIPS
            // ============================================================

            // Configure Company relationships
            builder.Entity<Company>()
                .HasMany(c => c.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Company>()
                .HasMany(c => c.Assets)
                .WithOne(a => a.Company)
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Company>()
                .HasMany(c => c.WorkOrders)
                .WithOne(wo => wo.Company)
                .HasForeignKey(wo => wo.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Company>()
                .HasMany(c => c.MaintenanceRequests)
                .WithOne(mr => mr.Company)
                .HasForeignKey(mr => mr.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Company>()
                .HasMany(c => c.Personnel)
                .WithOne(p => p.Company)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

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

            // Configure PreventiveSchedule relationships
            builder.Entity<PreventiveSchedule>()
                .HasOne(ps => ps.Asset)
                .WithMany()
                .HasForeignKey(ps => ps.AssetId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PreventiveSchedule>()
                .HasOne(ps => ps.DefaultTechnician)
                .WithMany()
                .HasForeignKey(ps => ps.DefaultTechnicianId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure MaintenanceLog relationships
            builder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.WorkOrder)
                .WithMany()
                .HasForeignKey(ml => ml.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.Asset)
                .WithMany()
                .HasForeignKey(ml => ml.AssetId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.CompletedByPersonnel)
                .WithMany()
                .HasForeignKey(ml => ml.CompletedByPersonnelId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Part relationships
            builder.Entity<Part>()
                .HasMany(p => p.WorkOrderParts)
                .WithOne(wop => wop.Part)
                .HasForeignKey(wop => wop.PartId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure WorkOrderPart relationships
            builder.Entity<WorkOrderPart>()
                .HasOne(wop => wop.Company)
                .WithMany()
                .HasForeignKey(wop => wop.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<WorkOrderPart>()
                .HasOne(wop => wop.WorkOrder)
                .WithMany()
                .HasForeignKey(wop => wop.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure WorkOrderCost relationships
            builder.Entity<WorkOrderCost>()
                .HasOne(woc => woc.Company)
                .WithMany()
                .HasForeignKey(woc => woc.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<WorkOrderCost>()
                .HasOne(woc => woc.WorkOrder)
                .WithMany()
                .HasForeignKey(woc => woc.WorkOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

}
