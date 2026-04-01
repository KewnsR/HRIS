using HumanRepProj.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class AttendanceRecord
{
    [Key]
    public int AttendanceID { get; set; }

    [Required]
    public int EmployeeID { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? TimeIn { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? TimeOut { get; set; }

    [StringLength(20)]
    public string Status { get; set; } // Present, Absent, Late, etc.

    [DataType(DataType.Time)]
    public TimeSpan? LunchStartTime { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? LunchEndTime { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: each AttendanceRecord belongs to 1 Employee
    [ForeignKey("EmployeeID")]
    public virtual Employee Employee { get; set; }
}
    