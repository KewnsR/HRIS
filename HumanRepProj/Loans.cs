using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace HumanRepProj
{

    [Table("Loans")] // Maps to the database table "positions"
    public class Loans : BaseEntity
    {
        [Key]
        [Column("LoanID")]
        public int LoanID { get; set; }
        [Column("EmployeeID")]
        public int EmployeeID { get; set; }
        [Column("LoanType")]
        public string LoanType { get; set; }
        [Column("LoanAmount")]
        public decimal LoanAmount { get; set; } //Amount of the Employee Loaning
        [Column("LoanStatus")]
        public string LoanStatus { get; set; }
        [Column("DateIssued")]
        public DateTime DateIssued { get; set; }
        [Column("DueDate")]
        public DateTime DueDate { get; set; }

        [Column("PaidLoan")] //How much did paid it can be 0 
        public decimal PaidLoan { get; set; }
        [Column("LoanTerm")]
        public decimal LoanTerm { get; set; }

        [Column("ReviewedBy")]
        public string? ReviewedBy { get; set; }

        [Column("ReviewedAt")]
        public DateTime? ReviewedAt { get; set; }

        [Column("DecisionNote")]
        public string? DecisionNote { get; set; }


    }
}
