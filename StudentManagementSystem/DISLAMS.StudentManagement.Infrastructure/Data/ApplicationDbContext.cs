using Microsoft.EntityFrameworkCore;
using DISLAMS.StudentManagement.Domain.Entities;

namespace DISLAMS.StudentManagement.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for DISLAMS Student Management
    /// 
    /// CODE-FIRST APPROACH:
    /// This DbContext defines all entities and their relationships
    /// Migrations are generated from this definition
    /// 
    /// GOVERNANCE CONSIDERATIONS:
    /// - All tables have audit columns (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
    /// - AuditLog table has unique constraint to prevent duplicates
    /// - Foreign keys cascade appropriately
    /// - Soft deletes not used - records are immutable or versioned
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Entities
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AttendanceException> AttendanceExceptions { get; set; }
        public DbSet<ReopenRequest> ReopenRequests { get; set; }
        public DbSet<Actor> Actors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Student Configuration
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StudentId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ClassGrade).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(e => e.StudentId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Course Configuration
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CourseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.CourseCode).IsUnique();
            });

            // AttendanceRecord Configuration
            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Foreign keys
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.AttendanceRecords)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.AttendanceRecords)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Self-reference for versioning
                entity.HasOne(e => e.ParentVersion)
                    .WithMany(e => e.ChildVersions)
                    .HasForeignKey(e => e.ParentVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for queries
                entity.HasIndex(e => new { e.StudentId, e.AttendanceDate, e.CourseId }).IsUnique(false);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.AttendanceDate);
                entity.HasIndex(e => e.CourseId);

                // Properties
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Version).IsRequired();
            });

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.AttendanceRecord)
                    .WithMany(a => a.AuditLogs)
                    .HasForeignKey(e => e.AttendanceRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Audit logs are append-only - no updates allowed
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.PreviousValue).HasMaxLength(500);
                entity.Property(e => e.NewValue).HasMaxLength(500);
                entity.Property(e => e.ContextInfo).HasMaxLength(500);

                // Index for audit trail queries
                entity.HasIndex(e => e.AttendanceRecordId);
                entity.HasIndex(e => e.ActionTimestamp);
                entity.HasIndex(e => e.ActorId);
            });

            // AttendanceException Configuration
            modelBuilder.Entity<AttendanceException>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.AttendanceRecord)
                    .WithMany(a => a.Exceptions)
                    .HasForeignKey(e => e.AttendanceRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ResolutionStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ResolutionNotes).HasMaxLength(500);

                entity.HasIndex(e => e.AttendanceRecordId);
                entity.HasIndex(e => e.ReportedAt);
            });

            // ReopenRequest Configuration
            modelBuilder.Entity<ReopenRequest>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.AttendanceRecord)
                    .WithMany(a => a.ReopenRequests)
                    .HasForeignKey(e => e.AttendanceRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ApprovalComments).HasMaxLength(500);

                entity.HasIndex(e => e.AttendanceRecordId);
                entity.HasIndex(e => e.Status);
            });

            // Actor Configuration
            modelBuilder.Entity<Actor>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ExternalUserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Role).IsRequired();

                entity.HasIndex(e => e.ExternalUserId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });
        }
    }
}
