using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HumanRepProj
{
    [Table("LeaveRequests")]
    public class LeaveRequest : BaseEntity
    {
        [Key]
        [Column("LeaveRequestID")]
        public int LeaveRequestID { get; set; }

        [Column("EmployeeID")]
        public int EmployeeID { get; set; }

        [Required]
        [MaxLength(120)]
        [Column("EmployeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        [Column("LeaveType")]
        public string LeaveType { get; set; } = string.Empty;

        [Column("StartDate")]
        public DateTime StartDate { get; set; }

        [Column("EndDate")]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("Reason")]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        [Column("ContactDuringLeave")]
        public string ContactDuringLeave { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("Status")]
        public string Status { get; set; } = "Pending";

        [Column("SubmittedAt")]
        public DateTime SubmittedAt { get; set; }

        [MaxLength(50)]
        [Column("ReviewedBy")]
        public string? ReviewedBy { get; set; }

        [Column("ReviewedAt")]
        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        [Column("DecisionNote")]
        public string? DecisionNote { get; set; }
    }
}