using HumanRepProj.Controllers;
using HumanRepProj.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HumanRepProj.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSetsmodelBuilder.Entity<LoanHistory>().ToTable("LoanHistory");
        
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<Loans> Loans { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<FaceData> FaceData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureApplicationUser(modelBuilder);
            ConfigureDepartment(modelBuilder);
            ConfigureEmployee(modelBuilder);
            ConfigureAttendanceRecord(modelBuilder);
            ConfigureLeaveRequest(modelBuilder);

            modelBuilder.Entity<Loans>().ToTable("Loans");



            modelBuilder.Entity<Loans>()
        .Property(l => l.LoanAmount)
        .HasColumnType("decimal(18,2)");            

            modelBuilder.Entity<Loans>()
         .Property(l => l.PaidLoan)
         .HasColumnType("decimal(18,2)");

                modelBuilder.Entity<Loans>()
            .Property(l => l.ReviewedBy)
            .HasMaxLength(50);

                modelBuilder.Entity<Loans>()
            .Property(l => l.ReviewedAt)
            .HasColumnType("timestamp with time zone");

                modelBuilder.Entity<Loans>()
            .Property(l => l.DecisionNote)
            .HasMaxLength(500);

        }

        private void ConfigureApplicationUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Logins");
                entity.HasKey(u => u.LoginID);
                entity.Property(u => u.LoginID).HasColumnName("LoginID").ValueGeneratedOnAdd();
                entity.Property(u => u.EmployeeID).HasColumnName("EmployeeID");
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50).HasColumnName("Username");
                entity.Property(u => u.Password).IsRequired().HasMaxLength(255).HasColumnName("Password");
                entity.Property(u => u.LastLogin).HasColumnType("timestamp with time zone").HasColumnName("LastLogin");
                entity.Property(u => u.FailedAttempts).HasColumnName("FailedAttempts").HasDefaultValue(0);
                entity.Property(u => u.IsLocked).HasColumnName("IsLocked").HasDefaultValue(false);
                entity.Property(u => u.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(u => u.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(u => u.Username).IsUnique().HasDatabaseName("IX_Logins_Username");

                entity.HasOne(u => u.Employee)
                      .WithMany()
                      .HasForeignKey(u => u.EmployeeID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments");
                entity.HasKey(d => d.DepartmentID);
                entity.Property(d => d.DepartmentID).HasColumnName("DepartmentID").ValueGeneratedOnAdd();
                entity.Property(d => d.Name).IsRequired().HasColumnName("DepartmentName").HasMaxLength(100);
                entity.Property(d => d.Description).HasMaxLength(500).HasColumnName("Description");
                entity.Property(d => d.Performance).HasColumnType("decimal(18,2)").HasColumnName("Performance");
                entity.Property(d => d.DateCreated).HasColumnType("timestamp with time zone").HasColumnName("DateCreated").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(d => d.Budget).HasColumnType("decimal(18,2)").HasColumnName("Budget");
                entity.Property(d => d.Status).HasMaxLength(50).HasColumnName("Status").HasDefaultValue("Active");
                entity.HasIndex(d => d.Name).IsUnique().HasDatabaseName("IX_Departments_Name");

                entity.HasMany(d => d.Employees)
                      .WithOne(e => e.Department)
                      .HasForeignKey(e => e.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureEmployee(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employees");
                entity.HasKey(e => e.EmployeeID);
                entity.Property(e => e.EmployeeID).HasColumnName("EmployeeID").ValueGeneratedOnAdd();

                // Basic Info
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("FirstName");
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("LastName");

                // Contact Info
                entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("Email").IsRequired(false);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).HasColumnName("PhoneNumber").IsRequired(false);
                entity.Property(e => e.Address).HasMaxLength(500).HasColumnName("Address").IsRequired(false);

                // Employment Details
                entity.Property(e => e.DepartmentID).IsRequired().HasColumnName("DepartmentID");
                entity.Property(e => e.Position).IsRequired().HasMaxLength(100).HasColumnName("Position");
                entity.Property(e => e.Salary).IsRequired().HasColumnType("decimal(18,2)").HasColumnName("Salary");

                // Dates
                entity.Property(e => e.DateOfBirth).HasColumnType("date").HasColumnName("DateOfBirth");
                entity.Property(e => e.DateHired).IsRequired().HasColumnType("date").HasColumnName("DateHired");

                // Status
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasColumnName("Status").HasDefaultValue("Active");
                entity.Property(e => e.EmploymentType).HasMaxLength(20).HasColumnName("EmploymentType").HasDefaultValue("Full-time");

                // Manager relationship
                entity.Property(e => e.ManagerID).HasColumnName("ManagerID").IsRequired(false);

                // Timestamps
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone").HasColumnName("UpdatedAt").ValueGeneratedOnAddOrUpdate().HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relationships
                entity.HasOne(e => e.Manager)
                      .WithMany(e => e.Subordinates)
                      .HasForeignKey(e => e.ManagerID)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                        .HasDatabaseName("IX_Employees_Email");

                entity.HasIndex(e => e.ManagerID)
                      .HasDatabaseName("IX_Employees_ManagerID");


            });
        }

        private void ConfigureAttendanceRecord(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.ToTable("Attendance");
                entity.HasKey(a => a.AttendanceID);
                entity.Property(a => a.AttendanceID).HasColumnName("AttendanceID").ValueGeneratedOnAdd();

                entity.Property(a => a.EmployeeID).IsRequired().HasColumnName("EmployeeID");
                entity.Property(a => a.AttendanceDate).IsRequired().HasColumnType("date").HasColumnName("AttendanceDate");

                entity.Property(a => a.TimeIn).HasColumnType("time").HasColumnName("TimeIn");
                entity.Property(a => a.TimeOut).HasColumnType("time").HasColumnName("TimeOut");
                entity.Property(a => a.Status).HasMaxLength(20).HasColumnName("Status");

                entity.Property(a => a.LunchStartTime).HasColumnType("time").HasColumnName("LunchStartTime");
                entity.Property(a => a.LunchEndTime).HasColumnType("time").HasColumnName("LunchEndTime");

                entity.Property(a => a.CreatedAt).HasColumnType("timestamp with time zone").HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(a => a.UpdatedAt).HasColumnType("timestamp with time zone").HasColumnName("UpdatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(a => a.Employee)
                       .WithMany(e => e.AttendanceRecords)
                       .HasForeignKey(a => a.EmployeeID)
                       .OnDelete(DeleteBehavior.Cascade);
           
        });
        }

        private void ConfigureLeaveRequest(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.ToTable("LeaveRequests");
                entity.HasKey(r => r.LeaveRequestID);

                entity.Property(r => r.EmployeeName).IsRequired().HasMaxLength(120);
                entity.Property(r => r.LeaveType).IsRequired().HasMaxLength(80);
                entity.Property(r => r.Reason).IsRequired().HasMaxLength(500);
                entity.Property(r => r.ContactDuringLeave).IsRequired().HasMaxLength(40);
                entity.Property(r => r.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");

                    entity.Property(r => r.StartDate).HasColumnType("date");
                    entity.Property(r => r.EndDate).HasColumnType("date");

                entity.Property(r => r.SubmittedAt)
                      .HasColumnType("timestamp with time zone")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(r => r.ReviewedBy).HasMaxLength(50);
                entity.Property(r => r.ReviewedAt).HasColumnType("timestamp with time zone");
                entity.Property(r => r.DecisionNote).HasMaxLength(500);

                entity.HasIndex(r => r.EmployeeID).HasDatabaseName("IX_LeaveRequests_EmployeeID");
                entity.HasIndex(r => r.Status).HasDatabaseName("IX_LeaveRequests_Status");
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity &&
                             (e.State == EntityState.Added || e.State == EntityState.Modified));

            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.Entity is BaseEntity entity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedAt = now;
                    }

                    entity.UpdatedAt = now;
                }
            }
        }
    }

    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}