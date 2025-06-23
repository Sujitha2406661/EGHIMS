// Controllers/PremiumCalculatorController.cs
using Health_Insurance.Models;
using Health_Insurance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Health_Insurance.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

namespace Health_Insurance.Controllers
{
    [Authorize] // All actions require authentication
    public class PremiumCalculatorController : Controller
    {
        private readonly IPremiumCalculatorService _premiumCalculatorService;
        private readonly ApplicationDbContext _context; // To get employees and policies for dropdowns

        public PremiumCalculatorController(IPremiumCalculatorService premiumCalculatorService, ApplicationDbContext context)
        {
            _premiumCalculatorService = premiumCalculatorService;
            _context = context;
        }

        // GET: /PremiumCalculator/Index
        // Displays the premium calculator form with dynamic dropdowns.
        public async Task<IActionResult> Index()
        {
            // Populate dropdowns for Employees and Policies
            if (User.IsInRole("Employee"))
            {
                var loggedInEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;
                if (loggedInEmployeeIdClaim != null && int.TryParse(loggedInEmployeeIdClaim, out int actualEmployeeId))
                {
                    var employee = await _context.Employees.FindAsync(actualEmployeeId);
                    // For an Employee, pass only their own details for the read-only field
                    ViewBag.EmployeeList = new SelectList(new List<Employee> { employee }, "EmployeeId", "Name", actualEmployeeId); // Pass employee details for dropdown/pre-selection
                    ViewBag.IsEmployee = true; // Flag for the view to render read-only employee field
                    ViewBag.LoggedInEmployeeId = actualEmployeeId; // Pass ID for hidden input
                    ViewBag.LoggedInEmployeeName = employee?.Name; // Pass Name for display
                }
                else
                {
                    // Fallback for an authenticated employee without an EmployeeId claim (shouldn't happen)
                    ViewBag.EmployeeList = new SelectList(new List<Employee>());
                    ViewBag.IsEmployee = true;
                    ViewBag.LoggedInEmployeeId = 0; // Or handle as an error
                    ViewBag.LoggedInEmployeeName = "Unknown Employee (Error)";
                }
            }
            else // Admin or HR personnel will see a dropdown to select employees
            {
                var employees = await _context.Employees.OrderBy(e => e.Name).ToListAsync();
                ViewBag.EmployeeList = new SelectList(employees, "EmployeeId", "Name");
                ViewBag.IsEmployee = false; // Flag for the view to render employee dropdown
            }

            // Always fetch all policies for the policy dropdown
            var policies = await _context.Policies.OrderBy(p => p.PolicyName).ToListAsync();
            ViewBag.PolicyList = new SelectList(policies, "PolicyId", "PolicyName");

            return View(); // This will render Views/PremiumCalculator/Index.cshtml
        }

        // POST: /PremiumCalculator/CalculatePremium - Action to calculate premium via AJAX
        [HttpPost]
        public async Task<IActionResult> CalculatePremium(int employeeId, int policyId)
        {
            // Enforce that an Employee can only calculate premium for themselves
            if (User.IsInRole("Employee"))
            {
                var loggedInEmployeeId = User.FindFirst("EmployeeId")?.Value;
                if (loggedInEmployeeId == null || !int.TryParse(loggedInEmployeeId, out int actualEmployeeId) || actualEmployeeId != employeeId)
                {
                    // Return a Forbidden status if an employee tries to calculate for someone else.
                    return Forbid(); // 403 Forbidden
                }
            }

            var calculatedPremium = await _premiumCalculatorService.CalculatePremiumAsync(employeeId, policyId);
            // Return JSON response for AJAX call.
            return Json(new { premium = calculatedPremium });
        }
    }
}