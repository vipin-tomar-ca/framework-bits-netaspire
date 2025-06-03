using Microsoft.EntityFrameworkCore;
using IntegrationPlatform.Contracts.Models;

namespace IntegrationPlatform.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AuditTrailEntry> AuditTrailEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuditTrailEntry>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.Operation)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.OperationType)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.RequestPath)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.RequestMethod)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.IpAddress)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.UserId)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.UserName)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.ResponseStatus)
                .IsRequired();

            modelBuilder.Entity<AuditTrailEntry>()
                .Property(a => a.Duration)
                .IsRequired();
        }
    }
} 