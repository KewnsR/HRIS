using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HumanRepProj.Data;
using HumanRepProj.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HumanRepProj.Pages.Attendance
{
    public class RecordAttendanceModel : PageModel
    {
        private readonly ILogger<RecordAttendanceModel> _logger;
        private readonly ApplicationDbContext _context;

        public RecordAttendanceModel(
            ILogger<RecordAttendanceModel> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "Employee";
        public AttendanceRecord? TodayRecord { get; set; }
        public bool AttendanceCompleted => TodayRecord?.TimeIn.HasValue == true && TodayRecord?.TimeOut.HasValue == true;

        public async Task<IActionResult> OnGetAsync()
        {
            var sessionEmployeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (!sessionEmployeeId.HasValue || sessionEmployeeId.Value <= 0)
            {
                _logger.LogWarning("Unauthorized access to RecordAttendance without employee session.");
                return RedirectToPage("/UserLogin");
            }

            EmployeeId = sessionEmployeeId.Value;
            EmployeeName = HttpContext.Session.GetString("FullName") ?? HttpContext.Session.GetString("UserName") ?? "Employee";

            var today = DateTime.Today;
            TodayRecord = await _context.AttendanceRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeID == EmployeeId && a.AttendanceDate.Date == today.Date);

            _logger.LogInformation("Employee {EmployeeId} accessed their attendance page.", EmployeeId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var sessionEmployeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (!sessionEmployeeId.HasValue || sessionEmployeeId.Value <= 0)
            {
                _logger.LogWarning("Unauthorized attendance post without employee session.");
                return RedirectToPage("/UserLogin");
            }

            var employeeId = sessionEmployeeId.Value;

            var today = DateTime.Today;
            var now = DateTime.UtcNow;
            var nowTime = now.TimeOfDay;

            var existingRecord = await _context.AttendanceRecords
                .FirstOrDefaultAsync(r => r.EmployeeID == employeeId && r.AttendanceDate.Date == today.Date);

            if (existingRecord == null)
            {
                var newRecord = new AttendanceRecord
                {
                    EmployeeID = employeeId,
                    AttendanceDate = today,
                    TimeIn = nowTime,
                    Status = "Present",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _context.AttendanceRecords.AddAsync(newRecord);
                _logger.LogInformation("Employee {EmployeeId} checked in via page post.", employeeId);
                TempData["AttendanceMessage"] = "Manual check-in recorded.";
            }
            else
            {
                if (!existingRecord.TimeOut.HasValue)
                {
                    existingRecord.TimeOut = nowTime;
                    existingRecord.UpdatedAt = now;
                    _context.AttendanceRecords.Update(existingRecord);
                    _logger.LogInformation("Employee {EmployeeId} checked out via page post.", employeeId);
                    TempData["AttendanceMessage"] = "Manual check-out recorded.";
                }
                else
                {
                    TempData["AttendanceMessage"] = "You have already completed check-in and check-out today.";
                    return RedirectToPage();
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}