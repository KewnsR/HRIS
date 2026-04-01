using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HumanRepProj.Data;
using HumanRepProj.Models;
using HumanRepProj.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HumanRepProj.Pages
{
    public class AttendanceModel : PageModel
    {
        private readonly ILogger<AttendanceModel> _logger;
        private readonly ApplicationDbContext _context;

        public AttendanceModel(
            ILogger<AttendanceModel> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Summary Metrics
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public double AttendanceRate { get; set; }

        // Table Data
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this, _logger);
            if (guardResult != null)
            {
                return guardResult;
            }

            _logger.LogInformation("Admin user {Username} accessed the Attendance page.", AdminSessionGuard.GetUsername(HttpContext));

            var today = DateTime.Today;

            // Fetch all employees and today's attendance records
            var totalEmployees = await _context.Employees.CountAsync();
            var todayRecords = await _context.AttendanceRecords
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate == today)
                .ToListAsync();

            // Summary Stats
            PresentCount = todayRecords.Count(r => r.Status == "Present");
            AbsentCount = todayRecords.Count(r => r.Status == "Absent");
            LateCount = todayRecords.Count(r => r.Status == "Late");

            // Attendance Rate (Present / Total Employees)
            AttendanceRate = totalEmployees > 0
                ? Math.Round((double)PresentCount / totalEmployees * 100, 1)
                : 0.0;

            // Attendance Table
            AttendanceRecords = todayRecords;

            return Page();
        }

        // ✅ Logout handler
        public IActionResult OnPostLogout()
        {
            _logger.LogInformation("User logged out.");
            HttpContext.Session.Clear(); // Clear session
            return RedirectToPage("/Login");
        }
    }
}