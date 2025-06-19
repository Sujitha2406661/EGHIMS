// Controllers/ClaimController.cs
using Health_Insurance.Data; // Ensure namespace is correct for your DbContext
using Health_Insurance.Models; // Ensure namespace is correct for your Models
using Health_Insurance.Services; // Ensure namespace is correct for your Services
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Needed for .ToListAsync(), .Include()
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Still needed for ClaimTypes, ClaimsPrincipal, etc.
using System; // For generic Exception handling

namespace Health_Insurance.Controllers
{
    // All actions in this controller require authentication by default.
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly ApplicationDbContext _context;

        public ClaimController(IClaimService claimService, ApplicationDbContext context)
        {
            _claimService = claimService;
            _context = context;
        }

        // GET: /Claim/ListAllClaims or /Claim
        // Accessible to all authenticated users.
        // Admins see all claims. Employees see only their own claims.
        public async Task<IActionResult> ListAllClaims(int? employeeId = null) // Make employeeId nullable
        {
            IEnumerable<Health_Insurance.Models.Claim> claims; // Explicitly use your model's Claim

            if (User.IsInRole("Admin"))
            {
                // Admin sees all claims
                claims = await _claimService.ListAllClaimsAsync();
            }
            else if (User.IsInRole("Employee"))
            {
                System.Security.Claims.Claim employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int actualEmployeeId))
                {
                    return Forbid(); // EmployeeId claim missing or invalid
                }

                // Employee sees only their own claims, ensure the parameter matches logged-in user
                if (employeeId == null || employeeId.Value != actualEmployeeId)
                {
                    // If employeeId is not provided or doesn't match, use the logged-in employeeId
                    employeeId = actualEmployeeId;
                }
                else if (employeeId.Value != actualEmployeeId)
                {
                    // Attempt by an Employee to view someone else's claims by passing a different employeeId
                    return Forbid();
                }

                // Filter claims by the authenticated employee's ID
                claims = (await _claimService.ListAllClaimsAsync()).Where(c => c.Enrollment?.EmployeeId == employeeId.Value); // Added null conditional for Enrollment
            }
            else
            {
                // Should not happen with [Authorize] but as a fallback
                return Forbid();
            }

            return View(claims);
        }

        // GET: /Claim/SubmitClaim
        // Accessible to all authenticated users.
        public async Task<IActionResult> SubmitClaim()
        {
            if (User.IsInRole("Employee"))
            {
                System.Security.Claims.Claim employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int actualEmployeeId))
                {
                    return Forbid(); // EmployeeId claim missing or invalid
                }

                var enrollments = await _context.Enrollments
                                                    .Include(e => e.Policy)
                                                    .Where(e => e.EmployeeId == actualEmployeeId) // Filter by logged-in employee
                                                    .ToListAsync();

                ViewBag.EnrollmentList = new SelectList(enrollments.Select(e => new {
                    EnrollmentId = e.EnrollmentId,
                    DisplayText = $"Enrollment #{e.EnrollmentId} - {e.Policy?.PolicyName ?? "Unknown Policy"}"
                }), "EnrollmentId", "DisplayText");
            }
            else if (User.IsInRole("Admin"))
            {
                var enrollments = await _context.Enrollments
                                                    .Include(e => e.Employee)
                                                    .Include(e => e.Policy)
                                                    .ToListAsync();
                ViewBag.EnrollmentList = new SelectList(enrollments.Select(e => new {
                    EnrollmentId = e.EnrollmentId,
                    DisplayText = $"Enrollment #{e.EnrollmentId} - {e.Employee?.Name ?? "Unknown Employee"} ({e.Policy?.PolicyName ?? "Unknown Policy"})"
                }), "EnrollmentId", "DisplayText");
            }
            else
            {
                return Forbid(); // Not an Admin or Employee
            }

            return View();
        }

        // POST: /Claim/SubmitClaim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim([Bind("ClaimId,EnrollmentId,ClaimAmount,ClaimReason,ClaimDate")] Health_Insurance.Models.Claim claim)
        {
            // CRITICAL FIX: Set ClaimStatus to SUBMITTED before ModelState.IsValid check
            // Because ClaimStatus is [Required] in the model, and not bound from the form.
            claim.ClaimStatus = "SUBMITTED";

            // Validate that the submitted EnrollmentId belongs to the logged-in employee if they are an Employee
            if (User.IsInRole("Employee"))
            {
                System.Security.Claims.Claim employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int actualEmployeeId))
                {
                    ModelState.AddModelError(string.Empty, "Employee ID not found in claims.");
                    return await RePopulateSubmitClaimDropdown(claim);
                }

                var enrollment = await _context.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.EnrollmentId == claim.EnrollmentId);
                if (enrollment == null || enrollment.EmployeeId != actualEmployeeId)
                {
                    ModelState.AddModelError("EnrollmentId", "You can only submit claims for your own enrollments.");
                    return await RePopulateSubmitClaimDropdown(claim);
                }
            }

            // --- NEW VALIDATION: Claim Amount vs. Policy Coverage Amount ---
            // Fetch the enrollment *with* its policy details to get coverage amount
            var claimEnrollment = await _context.Enrollments
                                                .Include(e => e.Policy)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(e => e.EnrollmentId == claim.EnrollmentId);

            if (claimEnrollment == null)
            {
                // This scenario should be caught by the EnrollmentId validation above,
                // but as a safeguard, if policy details can't be fetched.
                ModelState.AddModelError("EnrollmentId", "Selected enrollment or its policy details not found.");
            }
            else if (claim.ClaimAmount > claimEnrollment.Policy?.CoverageAmount)
            {
                // Add a ModelState error if ClaimAmount exceeds CoverageAmount
                ModelState.AddModelError("ClaimAmount", $"Claim Amount cannot exceed the policy's Coverage Amount of {claimEnrollment.Policy?.CoverageAmount:C}.");
            }
            // --- END NEW VALIDATION ---


            if (ModelState.IsValid) // This will now also check the new ClaimAmount validation
            {
                var success = await _claimService.SubmitClaimAsync(claim);

                if (success)
                {
                    return RedirectToAction(nameof(ListAllClaims));
                }
                else
                {
                    ViewBag.ErrorMessage = "Claim submission failed. Please check the Enrollment ID.";
                    return await RePopulateSubmitClaimDropdown(claim);
                }
            }
            // If ModelState is not valid (due to any validation, including the new one), re-populate dropdowns and return view
            return await RePopulateSubmitClaimDropdown(claim);
        }

        // Helper to repopulate dropdown for SubmitClaim view
        private async Task<IActionResult> RePopulateSubmitClaimDropdown(Health_Insurance.Models.Claim claim) // Explicitly qualify your model's Claim
        {
            if (User.IsInRole("Employee"))
            {
                System.Security.Claims.Claim employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int actualEmployeeId))
                {
                    return Forbid();
                }
                var enrollments = await _context.Enrollments
                                                    .Include(e => e.Policy)
                                                    .Where(e => e.EmployeeId == actualEmployeeId)
                                                    .ToListAsync();
                ViewBag.EnrollmentList = new SelectList(enrollments.Select(e => new {
                    EnrollmentId = e.EnrollmentId,
                    DisplayText = $"Enrollment #{e.EnrollmentId} - {e.Policy?.PolicyName ?? "Unknown Policy"}"
                }), "EnrollmentId", "DisplayText", claim.EnrollmentId);
            }
            else if (User.IsInRole("Admin"))
            {
                var enrollments = await _context.Enrollments
                                                    .Include(e => e.Employee)
                                                    .Include(e => e.Policy)
                                                    .ToListAsync();
                ViewBag.EnrollmentList = new SelectList(enrollments.Select(e => new {
                    EnrollmentId = e.EnrollmentId,
                    DisplayText = $"Enrollment #{e.EnrollmentId} - {e.Employee?.Name ?? "Unknown Employee"} ({e.Policy?.PolicyName ?? "Unknown Policy"})"
                }), "EnrollmentId", "DisplayText", claim.EnrollmentId);
            }
            return View("SubmitClaim", claim);
        }


        // GET: /Claim/GetClaimDetails/5
        // Accessible to all authenticated users. Admin can view any. Employee can view their own.
        public async Task<IActionResult> GetClaimDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _claimService.GetClaimDetailsAsync(id.Value);

            if (claim == null)
            {
                return NotFound();
            }

            // Enforce that an Employee can only view their own claims
            if (User.IsInRole("Employee"))
            {
                System.Security.Claims.Claim employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int actualEmployeeId) || claim.Enrollment.EmployeeId != actualEmployeeId)
                {
                    return Forbid(); // Attempt by an Employee to view someone else's claim
                }
            }

            return View(claim);
        }

        // GET: /Claim/UpdateClaimStatus/5
        // Restricted to Admin users only.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateClaimStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _claimService.GetClaimDetailsAsync(id.Value);

            if (claim == null)
            {
                return NotFound();
            }

            var statuses = new List<string> { "SUBMITTED", "APPROVED", "REJECTED" };
            ViewBag.Statuses = new SelectList(statuses, claim.ClaimStatus);

            return View(claim);
        }

        // POST: /Claim/UpdateClaimStatus/5
        // Restricted to Admin users only.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateClaimStatus(int id, string ClaimStatus) // Changed parameter to string ClaimStatus
        {
            try
            {
                // Fetch the existing claim from the database
                var existingClaim = await _context.Claims.FindAsync(id);

                if (existingClaim == null)
                {
                    return NotFound();
                }

                // Update ONLY the status
                existingClaim.ClaimStatus = ClaimStatus; // Update status with the incoming value

                var success = await _claimService.UpdateClaimStatusAsync(id, ClaimStatus);

                if (success)
                {
                    return RedirectToAction(nameof(GetClaimDetails), new { id = id }); // Redirect back to details, or ListAllClaims
                }
                else
                {
                    ViewBag.ErrorMessage = "Failed to update claim status. Please check the Claim ID or status.";
                    var statuses = new List<string> { "SUBMITTED", "APPROVED", "REJECTED" };
                    ViewBag.Statuses = new SelectList(statuses, ClaimStatus); // Pass the attempted status back
                    var claimToReturn = await _claimService.GetClaimDetailsAsync(id);
                    return View(claimToReturn);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClaimExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using _logger)
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                var statuses = new List<string> { "SUBMITTED", "APPROVED", "REJECTED" };
                ViewBag.Statuses = new SelectList(statuses, ClaimStatus);
                var claimToReturn = await _claimService.GetClaimDetailsAsync(id);
                return View(claimToReturn);
            }
        }

        // Helper method to check if a claim exists
        private bool ClaimExists(int id)
        {
            return _context.Claims.Any(e => e.ClaimId == id);
        }
    }
}
