using HumanRepProj.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HumanRepProj.Data.Configurations
{
    public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
    {
        public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
        {
            builder.ToTable("Attendance");

            builder.HasKey(a => a.AttendanceID);
            builder.Property(a => a.AttendanceID)
                .HasColumnName("AttendanceID")
                .ValueGeneratedOnAdd();

            // Required fields
            builder.Property(a => a.EmployeeID).IsRequired();
            builder.Property(a => a.AttendanceDate)
                .IsRequired()
                .HasColumnType("date");

            // Optional fields
            builder.Property(a => a.TimeIn).HasColumnType("time");
            builder.Property(a => a.TimeOut).HasColumnType("time");
            builder.Property(a => a.Status).HasMaxLength(20);
            builder.Property(a => a.LunchStartTime).HasColumnType("time");
            builder.Property(a => a.LunchEndTime).HasColumnType("time");
            

            // Timestamps
            builder.Property(a => a.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(a => a.UpdatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            builder.HasOne(a => a.Employee)
                .WithMany(e => e.AttendanceRecords)
                .HasForeignKey(a => a.EmployeeID)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(a => new { a.EmployeeID, a.AttendanceDate })
                .IsUnique()
                .HasDatabaseName("IX_Attendance_Employee_Date");
        }
    }
}