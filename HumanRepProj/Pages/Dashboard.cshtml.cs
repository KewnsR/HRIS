using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HumanRepProj.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace HumanRepProj.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly ApplicationDbContext _context;

        public int TotalEmployees { get; set; }
        public int EmployeeChangeFromLastPeriod { get; set; }
        public int TotalDepartments { get; set; }
        public int NewDepartments { get; set; }
        public decimal AttendanceRate { get; set; }
        public decimal AttendanceChangeFromLastPeriod { get; set; }
        public List<string> EmployeeGrowthLabels { get; set; }
        public List<int> EmployeeGrowthCounts { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TimeRange { get; set; } = "6months";

        public DashboardModel(ILogger<DashboardModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            EmployeeGrowthLabels = new List<string>();
            EmployeeGrowthCounts = new List<int>();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username")
                ?? HttpContext.Session.GetString("UserName");

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Session expired or user not logged in.");
                return RedirectToPage("/Login");
            }

            if (!IsCurrentUserAdmin(username))
            {
                _logger.LogWarning("Non-admin user attempted to access admin dashboard: {Username}", username);
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Please use the Employee Login page.";
                return RedirectToPage("/UserLogin");
            }

            await LoadDashboardData();
            _logger.LogInformation("Admin user {Username} accessed the Dashboard.", username);
            return Page();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                DateTime startDate;
                DateTime endDate = currentDate;

                // Set date range based on selected filter
                switch (TimeRange)
                {
                    case "week":
                        startDate = ToUtcStartOfDay(currentDate.AddDays(-6)); // Last 7 days including today
                        break;
                    case "month":
                        startDate = ToUtcMonthStart(currentDate);
                        break;
                    default: // "6months"
                        startDate = ToUtcStartOfDay(currentDate.AddMonths(-6));
                        break;
                }

                // Employee statistics
                TotalEmployees = await _context.Employees
                    .CountAsync(e => e.Status == "Active" && e.DateHired <= endDate);

                var previousPeriodCount = await _context.Employees
                    .CountAsync(e => e.Status == "Active" && e.DateHired < startDate);
                EmployeeChangeFromLastPeriod = TotalEmployees - previousPeriodCount;

                // Department statistics
                TotalDepartments = await _context.Departments.CountAsync();
                NewDepartments = await _context.Departments
                    .CountAsync(d => d.DateCreated >= startDate && d.DateCreated <= endDate);

                // Generate employee growth data
                await GenerateEmployeeGrowthData(currentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                throw; // Re-throw to be handled by the calling method
            }
        }

        private async Task GenerateEmployeeGrowthData(DateTime currentDate)
        {
            try
            {
                _logger.LogInformation($"Generating growth data for {TimeRange} view");
                EmployeeGrowthLabels.Clear();
                EmployeeGrowthCounts.Clear();

                switch (TimeRange)
                {
                    case "week":
                        for (int i = 6; i >= 0; i--)
                        {
                            var targetDate = ToUtcStartOfDay(currentDate.AddDays(-i));
                            EmployeeGrowthLabels.Add(targetDate.ToString("ddd, MMM dd"));
                            var count = await _context.Employees
                                .CountAsync(e => e.Status == "Active" && e.DateHired <= targetDate);
                            EmployeeGrowthCounts.Add(count);
                        }
                        break;
                    case "month":
                        var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                        for (int day = 1; day <= daysInMonth; day++)
                        {
                            var targetDate = new DateTime(currentDate.Year, currentDate.Month, day, 0, 0, 0, DateTimeKind.Utc);
                            if (targetDate > currentDate) break;
                            EmployeeGrowthLabels.Add(targetDate.ToString("MMM dd"));
                            var count = await _context.Employees
                                .CountAsync(e => e.Status == "Active" && e.DateHired <= targetDate);
                            EmployeeGrowthCounts.Add(count);
                        }
                        break;
                    default: // "6months"
                        for (int i = 5; i >= 0; i--)
                        {
                            var targetDate = ToUtcStartOfDay(currentDate.AddMonths(-i));
                            EmployeeGrowthLabels.Add(targetDate.ToString("MMM yyyy"));
                            var count = await _context.Employees
                                .CountAsync(e => e.Status == "Active" && e.DateHired <= targetDate);
                            EmployeeGrowthCounts.Add(count);
                            _logger.LogDebug($"Added data point: {targetDate.ToString("MMM yyyy")} - {count} employees");
                        }
                        break;
                }
                _logger.LogInformation($"Generated {EmployeeGrowthLabels.Count} data points for {TimeRange} view");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating growth data");
                throw;
            }
        }

        public async Task<IActionResult> OnPost6Months()
        {
            return await HandleTimeRangePost("6months");
        }

        public async Task<IActionResult> OnPostThisMonth()
        {
            return await HandleTimeRangePost("month");
        }

        public async Task<IActionResult> OnPostThisWeek()
        {
            return await HandleTimeRangePost("week");
        }

        private async Task<IActionResult> HandleTimeRangePost(string range)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username")
                    ?? HttpContext.Session.GetString("UserName");
                var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (string.IsNullOrWhiteSpace(username))
                {
                    if (isAjax)
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            unauthorized = true,
                            redirectUrl = Url.Page("/Login"),
                            error = "Session expired. Please log in again."
                        });
                    }

                    return RedirectToPage("/Login");
                }

                if (!IsCurrentUserAdmin(username))
                {
                    HttpContext.Session.Clear();
                    TempData["ErrorMessage"] = "Please use the Employee Login page.";
                    if (isAjax)
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            unauthorized = true,
                            redirectUrl = Url.Page("/UserLogin"),
                            error = "Please use the Employee Login page."
                        });
                    }

                    return RedirectToPage("/UserLogin");
                }

                TimeRange = range;
                _logger.LogInformation($"Loading {range} dashboard data...");

                await LoadDashboardData();

                if (isAjax)
                {
                    _logger.LogInformation($"Returning JSON response for {range} data");
                    return new JsonResult(new
                    {
                        success = true,
                        employeeGrowthLabels = EmployeeGrowthLabels,
                        employeeGrowthCounts = EmployeeGrowthCounts,
                        totalEmployees = TotalEmployees,
                        employeeChangeFromLastPeriod = EmployeeChangeFromLastPeriod,
                        totalDepartments = TotalDepartments,
                        newDepartments = NewDepartments,
                        attendanceRate = AttendanceRate,
                        attendanceChangeFromLastPeriod = AttendanceChangeFromLastPeriod,
                        timeRange = TimeRange
                    });
                }

                return await OnGetAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnPost{range}");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new
                    {
                        success = false,
                        error = $"Error loading {range} data",
                        details = ex.Message
                    });
                }

                return RedirectToPage("/Error");
            }
        }

        public IActionResult OnPostLogout()
        {
            _logger.LogInformation("User logged out.");
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }

        private static bool IsCurrentUserAdmin(string? username)
        {
            return !string.IsNullOrWhiteSpace(username)
                && string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime ToUtcStartOfDay(DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static DateTime ToUtcMonthStart(DateTime value)
        {
            return new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}