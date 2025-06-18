// Controllers/EnrollmentController.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Health_Insurance.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Needed for User.FindFirst
using System.Collections.Generic; // Needed for List<T>
using System.Linq; // Required for LINQ methods like ToList(), Any()

namespace Health_Insurance.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly IPremiumCalculatorService _premiumCalculatorService; // Keep if other Enrollment actions rely on it
        private readonly ApplicationDbContext _context;

        public EnrollmentController(IEnrollmentService enrollmentService, IPremiumCalculatorService premiumCalculatorService, ApplicationDbContext context)
        {
            _enrollmentService = enrollmentService;
            _premiumCalculatorService = premiumCalculatorService;
            _context = context;
        }

        // GET: /Enrollment/Index
        public async Task<IActionResult> Index()
        {
            var policies = await _enrollmentService.GetAllPoliciesAsync();

            if (User.IsInRole("Employee"))
            {
                var loggedInEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
                if (loggedInEmployeeIdClaim != null && int.TryParse(loggedInEmployeeIdClaim, out int actualEmployeeId))
                {
                    ViewBag.IsEmployee = true;
                    ViewBag.LoggedInEmployeeId = actualEmployeeId;
                    var employee = await _context.Employees.FindAsync(actualEmployeeId);
                    ViewBag.LoggedInEmployeeName = employee?.Name;
                }
                else
                {
                    ViewBag.IsEmployee = true;
                    ViewBag.LoggedInEmployeeId = 0;
                    ViewBag.LoggedInEmployeeName = "Unknown Employee (Error)";
                }
            }
            else if (User.IsInRole("Admin") || User.IsInRole("HR")) // Admins and HR can enroll multiple employees
            {
                ViewBag.IsEmployee = false;
                var employees = await _context.Employees.OrderBy(e => e.Name).ToListAsync();
                ViewBag.EmployeeList = new SelectList(employees, "EmployeeId", "Name");
            }
            else // Other roles or unhandled
            {
                ViewBag.IsEmployee = false;
                ViewBag.EmployeeList = new SelectList(new List<Employee>());
            }

            return View(policies);
        }

        // POST: /Enrollment/Enroll
        // MODIFIED: Now accepts a list of employee IDs for multi-enrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Parameters: policyId (for the specific policy), employeeIds (array of selected employee IDs)
        public async Task<IActionResult> Enroll(int policyId, [FromForm] IEnumerable<int> employeeIds)
        {
            // If no employee IDs are selected, return a bad request or error message.
            if (employeeIds == null || !employeeIds.Any())
            {
                return BadRequest("No employees selected for enrollment.");
            }

            // For Employee role, ensure they can only enroll themselves.
            // This is a crucial server-side check for privilege escalation.
            if (User.IsInRole("Employee"))
            {
                var loggedInEmployeeId = User.FindFirst("EmployeeId")?.Value;
                if (loggedInEmployeeId == null || !int.TryParse(loggedInEmployeeId, out int actualEmployeeId) || employeeIds.Count() != 1 || employeeIds.First() != actualEmployeeId)
                {
                    // An employee can only enroll themselves, and only one at a time via this route.
                    // If they attempt multi-enroll or for another ID, deny.
                    return Forbid();
                }
            }

            // List to hold results of individual enrollments
            var enrollmentResults = new List<string>();
            bool overallSuccess = true;

            // Loop through each selected employee ID and attempt enrollment
            foreach (var empId in employeeIds)
            {
                var success = await _enrollmentService.EnrollEmployeeInPolicyAsync(empId, policyId);
                if (success)
                {
                    var employee = await _context.Employees.FindAsync(empId);
                    enrollmentResults.Add($"Successfully enrolled {employee?.Name ?? $"Employee ID {empId}"} into Policy ID {policyId}.");
                }
                else
                {
                    var employee = await _context.Employees.FindAsync(empId);
                    enrollmentResults.Add($"Failed to enroll {employee?.Name ?? $"Employee ID {empId}"} into Policy ID {policyId}. (Already enrolled or invalid data)");
                    overallSuccess = false; // Mark overall process as failed if any single enrollment fails
                }
            }

            // For AJAX calls, return a JSON response instead of a redirect.
            // This allows the frontend JavaScript to display individual messages.
            return Json(new { success = overallSuccess, messages = enrollmentResults });
        }


        // GET: /Enrollment/EnrolledPolicies/5
        public async Task<IActionResult> EnrolledPolicies(int employeeId)
        {
            if (User.IsInRole("Employee"))
            {
                var loggedInEmployeeId = User.FindFirst("EmployeeId")?.Value;
                if (loggedInEmployeeId == null || !int.TryParse(loggedInEmployeeId, out int actualEmployeeId) || actualEmployeeId != employeeId)
                {
                    return Forbid();
                }
            }

            var enrollments = await _enrollmentService.GetEnrolledPoliciesByEmployeeIdAsync(employeeId);

            var employee = await _context.Employees.FindAsync(employeeId);
            ViewBag.EmployeeName = employee?.Name ?? "Unknown Employee";

            return View(enrollments);
        }

        // POST: /Enrollment/CancelEnrollment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEnrollment(int enrollmentId, int employeeId)
        {
            if (User.IsInRole("Employee"))
            {
                var loggedInEmployeeId = User.FindFirst("EmployeeId")?.Value;
                if (loggedInEmployeeId == null || !int.TryParse(loggedInEmployeeId, out int actualEmployeeId) || actualEmployeeId != employeeId)
                {
                    return Forbid();
                }
            }

            var success = await _enrollmentService.CancelEnrollmentAsync(enrollmentId);

            if (success)
            {
                return RedirectToAction("EnrolledPolicies", new { employeeId = employeeId });
            }
            else
            {
                ViewBag.ErrorMessage = "Cancellation failed. Enrollment not found or cannot be cancelled.";
                return RedirectToAction("EnrolledPolicies", new { employeeId = employeeId });
            }
        }
    }
}



