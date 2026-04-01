using HumanRepProj.Data;
using HumanRepProj.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HumanRepProj.Pages
{
    public class LeaveModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LeaveModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [TempData]
        public string? Message { get; set; }

        public List<LeaveRequest> LeaveRequests { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var guard = AdminSessionGuard.EnsureAdmin(this);
            if (guard != null)
            {
                return guard;
            }

            await LoadRequestsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var guard = AdminSessionGuard.EnsureAdmin(this);
            if (guard != null)
            {
                return guard;
            }

            var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.LeaveRequestID == id);
            if (request != null)
            {
                request.Status = "Approved";
                request.ReviewedBy = AdminSessionGuard.GetUsername(HttpContext) ?? "admin";
                request.ReviewedAt = DateTime.UtcNow;
                request.DecisionNote = "Approved by admin.";
                await _context.SaveChangesAsync();
                Message = "Leave request approved successfully.";
            }
            else
            {
                Message = "Leave request was not found.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var guard = AdminSessionGuard.EnsureAdmin(this);
            if (guard != null)
            {
                return guard;
            }

            var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.LeaveRequestID == id);
            if (request != null)
            {
                request.Status = "Rejected";
                request.ReviewedBy = AdminSessionGuard.GetUsername(HttpContext) ?? "admin";
                request.ReviewedAt = DateTime.UtcNow;
                request.DecisionNote = "Rejected by admin.";
                await _context.SaveChangesAsync();
                Message = "Leave request rejected successfully.";
            }
            else
            {
                Message = "Leave request was not found.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var guard = AdminSessionGuard.EnsureAdmin(this);
            if (guard != null)
            {
                return guard;
            }

            var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.LeaveRequestID == id);
            if (request != null)
            {
                _context.LeaveRequests.Remove(request);
                await _context.SaveChangesAsync();
                Message = "Leave request deleted successfully.";
            }
            else
            {
                Message = "Leave request was not found.";
            }

            return RedirectToPage();
        }

        private async Task LoadRequestsAsync()
        {
            LeaveRequests = await _context.LeaveRequests
                .AsNoTracking()
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        }
    }
}
