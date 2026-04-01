using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HumanRepProj.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Performance must be between 0 and 100.")]
        public decimal Performance { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive value.")]
        public decimal Budget { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        // Nullable Manager relationship
        public int? ManagerID { get; set; }

        [ForeignKey("ManagerID")]
        [ValidateNever]
        public virtual Employee? Manager { get; set; }

        // Navigation property for employees
        [ValidateNever]
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}