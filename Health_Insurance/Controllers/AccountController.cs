// Controllers/AccountController.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Health_Insurance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Health_Insurance.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        public AccountController(IUserService userService, ApplicationDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Account"); // Admin goes to Employee management
                }
                else if (User.IsInRole("Employee"))
                {
                    var employeeIdClaim = User.FindFirst("EmployeeId");
                    if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
                    {
                        return RedirectToAction("EnrolledPolicies", "Enrollment", new { employeeId = employeeId });
                    }
                    return RedirectToAction("Index", "Home");
                }
              
                else if (User.IsInRole("HR"))
                {
                    // HR personnel might also manage employees, so redirect them to Employee/Index
                    //return RedirectToAction("Index", "Employee");
                    return RedirectToAction("HRDashboard", "Account");
                }
                
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userService.AuthenticateUserAsync(model.Username, model.Password, model.LoginType);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password, or incorrect login type selected.");
                return View(model);
            }

//here  claims is a built in thig to check the user authentication ,not in perspective of the policy claims,for generating cookie
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.Name, model.Username),
            };

            string userRole = string.Empty;
            int userId = 0; // To store AdminId, EmployeeId, or HRId

            if (user is Admin adminUser)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new System.Security.Claims.Claim("AdminId", adminUser.AdminId.ToString()));
                userRole = "Admin";
                userId = adminUser.AdminId;
            }
            else if (user is Employee employeeUser)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "Employee"));
                claims.Add(new System.Security.Claims.Claim("EmployeeId", employeeUser.EmployeeId.ToString()));
                userRole = "Employee";
                userId = employeeUser.EmployeeId;
            }
            // --- NEW: Handle HR claims and role ---
            else if (user is HR hrUser)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "HR"));
                claims.Add(new System.Security.Claims.Claim("HRId", hrUser.HRId.ToString())); // Custom claim for HRId
                userRole = "HR";
                userId = hrUser.HRId;
            }
            // --- END NEW ---

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = System.DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect based on role after successful login
            if (userRole == "Admin")
            {
                return RedirectToAction("Dashboard", "Account");
            }
            else if (userRole == "Employee")
            {
                return RedirectToAction("EnrolledPolicies", "Enrollment", new { employeeId = userId });
            }
            
            else if (userRole == "HR")
            {
                return RedirectToAction("HRDashboard", "Account"); // HR also goes to Employee management initially
            }
            
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "HR,Admin")] // Both HR and Admin can access this dashboard view
        public async Task<IActionResult> HRDashboard()
        {
            ViewData["Title"] = "HR Dashboard";

            var viewModel = new HRDashboardViewModel();

            // Fetch dashboard summary data
            viewModel.TotalEmployees = await _context.Employees.CountAsync();
            viewModel.TotalOrganizations = await _context.Organizations.CountAsync();

            // --- IMPORTANT: Choose ONE of these options for ActivePoliciesCount ---

            // Option 1: Count ALL policies in the system (simplest)
            // If "Active Policies" simply means all policies that are currently defined in your database.
            viewModel.ActivePoliciesCount = await _context.Policies.CountAsync();

            // --- End of ActivePoliciesCount options ---


            viewModel.TotalClaimsSubmitted = await _context.Claims.CountAsync();
            viewModel.PendingClaimsCount = await _context.Claims.CountAsync(c => c.ClaimStatus == "Pending"); // Assuming "Pending" status

            // Fetch recent enrollments
            viewModel.RecentEnrollments = await _context.Enrollments
                                                        .Include(e => e.Employee) // Load Employee data
                                                        .Include(e => e.Policy)   // Load Policy data
                                                        .OrderByDescending(e => e.EnrollmentDate)
                                                        .Take(5) // Get top 5 recent enrollments
                                                        .ToListAsync();

            // Fetch recent claims
            viewModel.RecentClaims = await _context.Claims
                                            .Include(c => c.Enrollment)
                                                .ThenInclude(en => en.Employee) // Load Employee via Enrollment
                                            .Include(c => c.Enrollment)
                                                .ThenInclude(en => en.Policy)   // Load Policy via Enrollment
                                            .OrderByDescending(c => c.ClaimDate)
                                            .Take(5) // Get top 5 recent claims
                                            .ToListAsync();

            return View(viewModel);
        }









        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }


        [Authorize(Roles = "Admin")] // Only Admins can access this dashboard
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Admin Dashboard"; // Set title for the layout
            return View(); // This will look for Views/Account/Dashboard.cshtml
        }


        /// NEW: Employee Dashboard MVC Action
        [Authorize(Roles = "Employee")] // Only Employees can access their dashboard
        public IActionResult EmpDashboard()
        {
            ViewData["Title"] = "Employee Dashboard";
            // Explicitly tells ASP.NET Core to look for Views/Account/EmpDashboard.cshtml
            return View();
        }




        [Authorize(Roles = "Admin,HR")]
        [HttpGet("/api/admin/policies")]
        public async Task<JsonResult> GetPoliciesApi() // Marked as async Task<JsonResult>
        {
            var policies = await _context.Policies.ToListAsync();

            // Select only the properties needed by the JavaScript frontend
            // Ensure property names match what the JS expects (e.g., 'id', 'name', 'premium', 'enrolled')
            var policiesForFrontend = policies.Select(p => new
            {
                id = p.PolicyId, // Assuming PolicyId is your primary key
                name = p.PolicyName, // Assuming PolicyName is the property for policy name
                type = p.PolicyType, // Assuming PolicyType property
                
                coverage = $"₹{p.CoverageAmount.ToString("N0")}", // Format currency in C# if needed, or handle in JS
                premium = p.PremiumAmount.ToString(), // Send as string to match JS's parsing logic (remove '₹')
               
                
            }).ToList();

            return Json(policiesForFrontend);
        
        }


 // API Endpoint for Employees Data - Fetches from DB based on provided model
        [HttpGet("/api/admin/employees")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<JsonResult> GetEmployeesApi()
        {
            // Include Organization for potentially displaying organization name
            var employees = await _context.Employees
                                        .Include(e => e.Organization) // Assuming navigation property 'Organization' exists
                                        .ToListAsync();

            var employeesForFrontend = employees.Select(e => new
            {
                id = e.EmployeeId,
                name = e.Name,
                email = e.Email ?? "N/A",      // Email can be null
                phone = e.Phone ?? "N/A",      // Phone can be null
                address = e.Address ?? "N/A",  // Address can be null
                department = e.Designation ?? "N/A", // 'Designation' from DB maps to 'department' in frontend
                organizationId = e.OrganizationId,
                organizationName = e.Organization != null ? e.Organization.OrganizationName : "N/A", // Get Org Name
                username = e.Username ?? "N/A", // Username can be null (though [Required] on model)
                // 'policyStatus' and 'dependents' are not directly available in Employee model.
                // These would need to be fetched/calculated from 'Enrollment' and 'Dependent' tables respectively.
                // For now, they are not included in the projection.
            }).ToList();

            return Json(employeesForFrontend);
        }

        // API Endpoint for Claims Data - Fetches from DB based on provided model
        [HttpGet("/api/admin/claims")]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetClaimsApi()
        {
            // Include Enrollment and then Employee and Policy to get related data
            var claims = await _context.Claims
                                       .Include(c => c.Enrollment)
                                           .ThenInclude(e => e.Employee) // To get Employee Name
                                       .Include(c => c.Enrollment)
                                           .ThenInclude(e => e.Policy) // To get Policy Type
                                       .ToListAsync();

            var claimsForFrontend = claims.Select(c => new
            {
                id = c.ClaimId,
                employeeName = c.Enrollment?.Employee?.Name ?? "N/A", // Safely get employee name
                policyType = c.Enrollment?.Policy?.PolicyType ?? "N/A", // Safely get policy type from Policy model
                claimDate = c.ClaimDate.ToString("yyyy-MM-dd"), // Format date for JS
                requestedAmount = c.ClaimAmount, // ClaimAmount maps to requestedAmount
                // Your Claim model does NOT have an 'ApprovedAmount' property.
                // This is a placeholder logic. You might need to add 'ApprovedAmount' to your Claim model
                // and database if you need to store this explicitly.
                approvedAmount = (c.ClaimStatus?.ToUpper() == "APPROVED" ? c.ClaimAmount : 0),
                status = c.ClaimStatus ?? "N/A" // ClaimStatus maps to status
            }).ToList();

            return Json(claimsForFrontend);
        }

        // API Endpoint for Claims Trend Data - Fetches and processes from DB based on provided model
        [HttpGet("/api/admin/claims-trend")]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetClaimsTrendApi()
        {
            // Fetch claims from the last 6 months (adjust as needed)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var claims = await _context.Claims
                                       .Where(c => c.ClaimDate >= sixMonthsAgo)
                                       .ToListAsync();

            // Group claims by month and status
            var claimsTrend = claims
                .GroupBy(c => new { c.ClaimDate.Year, c.ClaimDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"), // e.g., "Jan", "Feb"
                    approved = g.Count(c => c.ClaimStatus?.ToUpper() == "APPROVED"),
                    submitted = g.Count(c => c.ClaimStatus?.ToUpper() == "SUBMITTED"),
                    rejected = g.Count(c => c.ClaimStatus?.ToUpper() == "REJECTED")
                })
                .ToList();

            // Ensure we have data for the last 6 months even if no claims occurred
            var currentMonth = DateTime.UtcNow;
            var fullTrend = new List<object>();
            for (int i = 5; i >= 0; i--) // Go back 5 months from current, plus current (total 6)
            {
                var monthDate = currentMonth.AddMonths(-i);
                var monthName = monthDate.ToString("MMM");
                var existingMonthData = claimsTrend.FirstOrDefault(t => (string)t.GetType().GetProperty("month").GetValue(t) == monthName);

                if (existingMonthData == null)
                {
                    fullTrend.Add(new { month = monthName, approved = 0, submitted = 0, rejected = 0 });
                }
                else
                {
                    fullTrend.Add(existingMonthData);
                }
            }
            return Json(fullTrend);
        }

        // API Endpoint for Organizations Data - Fetches from DB based on provided model
        [HttpGet("/api/admin/organizations")]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetOrganizationsApi()
        {
            var organizations = await _context.Organizations.ToListAsync();

            var organizationsForFrontend = organizations.Select(o => new
            {
                id = o.OrganizationId,
                name = o.OrganizationName,
                contactPerson = o.ContactPerson ?? "N/A", // Can be null
                contactEmail = o.ContactEmail ?? "N/A",   // Can be null
                // 'employeesCount' and 'policiesEnrolled' are not direct properties.
                // These would require additional queries/joins to calculate from Employees and Enrollments.
                // For now, they are excluded from the projection.
            }).ToList();

            return Json(organizationsForFrontend);
        }


        // Inside HrManagerController.cs (or a new API controller)
        // Make sure you have _context (ApplicationDbContext) and _userManager (UserManager<ApplicationUser>) injected

        // Inside HrManagerController.cs (or a new API controller)
        // Make sure you have _context (ApplicationDbContext) and _userManager (UserManager<ApplicationUser>) injected

        [HttpGet("/api/admin/employee-enrollment-chart-data")]
        [Authorize(Roles = "Admin,HR")]// Or [Authorize(Roles = "Administrator,HRManager")] depending on who sees the dashboard
        public async Task<IActionResult> GetEmployeeEnrollmentChartData()
        {
            int enrolledCount = 0;
            int notEnrolledCount = 0;

            // Get all Employee entities from your database
            // Assuming your ApplicationDbContext has a DbSet<Employee> named 'Employees'
            var allEmployees = await _context.Employees.ToListAsync();

            foreach (var employee in allEmployees)
            {
                // Check if this Employee has ANY active enrollment in the Enrollment table
                // We now check the 'Status' field for "ACTIVE" (case-sensitive)
                var hasActiveEnrollment = await _context.Enrollments
                                                        .AnyAsync(e => e.EmployeeId == employee.EmployeeId && e.Status == "ACTIVE");

                if (hasActiveEnrollment)
                {
                    enrolledCount++;
                }
                else
                {
                    notEnrolledCount++;
                }
            }

            return Json(new { enrolled = enrolledCount, notEnrolled = notEnrolledCount });
        }



        [HttpGet("/api/dashboard/hr-employees")]
        [Authorize(Roles = "HR,Admin")] // Both HR and Admin can access employee data
        public async Task<JsonResult> GetHrEmployeesApi()
        {
            var employees = await _context.Employees
                                        .Include(e => e.Organization) // Include Organization for its name
                                        .ToListAsync();

            var employeesForFrontend = employees.Select(e => new
            {
                id = e.EmployeeId,
                name = e.Name,
                email = e.Email ?? "N/A",
                phone = e.Phone ?? "N/A",
                address = e.Address ?? "N/A",
                designation = e.Designation ?? "N/A", // Maps to 'department' in JS
                organizationId = e.OrganizationId,
                organizationName = e.Organization?.OrganizationName ?? "N/A", // Safely get organization name
                username = e.Username ?? "N/A"
                // HireDate, Status, Gender are not available in your provided Employee model.
            }).ToList();

            return Json(employeesForFrontend);
        }

        // API Endpoint for HR Claims Data (Replicated from Admin, authorized for HR)
        [HttpGet("/api/dashboard/hr-claims")]
        [Authorize(Roles = "HR,Admin")] // HR also needs to see claims
        public async Task<JsonResult> GetHrClaimsApi()
        {
            var claims = await _context.Claims
                                    .Include(c => c.Enrollment)
                                        .ThenInclude(e => e.Employee)
                                    .Include(c => c.Enrollment)
                                        .ThenInclude(e => e.Policy)
                                    .ToListAsync();

            var claimsForFrontend = claims.Select(c => new
            {
                id = c.ClaimId,
                employeeName = c.Enrollment?.Employee?.Name ?? "N/A",
                policyType = c.Enrollment?.Policy?.PolicyType ?? "N/A",
                claimDate = c.ClaimDate.ToString("yyyy-MM-dd"), // Format date for JS
                requestedAmount = c.ClaimAmount,
                // If Claim model does not have 'ApprovedAmount', this is a derived value.
                approvedAmount = (c.ClaimStatus?.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) == true ? c.ClaimAmount : 0),
                status = c.ClaimStatus ?? "N/A"
            }).ToList();

            return Json(claimsForFrontend);
        }

        // API Endpoint for HR Organizations Data (Replicated from Admin, authorized for HR)
        [HttpGet("/api/dashboard/hr-organizations")]
        [Authorize(Roles = "HR,Admin")] // HR also needs to see organizations
        public async Task<JsonResult> GetHrOrganizationsApi()
        {
            var organizations = await _context.Organizations.ToListAsync();

            var organizationsForFrontend = organizations.Select(o => new
            {
                id = o.OrganizationId,
                name = o.OrganizationName,
                contactPerson = o.ContactPerson ?? "N/A",
                contactEmail = o.ContactEmail ?? "N/A"
            }).ToList();

            return Json(organizationsForFrontend);
        }

        // API Endpoint for HR Enrollments Data (New for HR Dashboard)
        [HttpGet("/api/dashboard/hr-enrollments")]
        [Authorize(Roles = "HR,Admin")] // HR needs access to enrollments
        public async Task<JsonResult> GetHrEnrollmentsApi()
        {
            var enrollments = await _context.Enrollments
                                            .Include(e => e.Employee) // Include Employee for name
                                            .Include(e => e.Policy)   // Include Policy for name and type
                                            .ToListAsync();

            var enrollmentsForFrontend = enrollments.Select(e => new
            {
                id = e.EnrollmentId,
                employeeName = e.Employee?.Name ?? "N/A",
                policyName = e.Policy?.PolicyName ?? "N/A",
                policyType = e.Policy?.PolicyType ?? "N/A",
                enrollmentDate = e.EnrollmentDate.ToString("yyyy-MM-dd"), // Format date for JS
                status = e.Status ?? "N/A"
            }).ToList();

            return Json(enrollmentsForFrontend);
        }

        // --- Shared API Endpoint (Used by both Admin and HR Overviews) ---
        // IMPORTANT: Ensure this method appears ONLY ONCE in your AccountController.cs
        // If you have a duplicate in another section (e.g., Admin APIs), remove that duplicate.
        [HttpGet("/api/dashboard/employee-enrollment-chart-data")]
        [Authorize(Roles = "Admin,HR")] // Accessible by both Admin and HR
        public async Task<IActionResult> GetEmployeeEnrollmentChartData1()
        {
            var totalEmployees = await _context.Employees.CountAsync();

            // Count employees with an 'ACTIVE' enrollment status
            var enrolledEmployeeIds = await _context.Enrollments
                                                    .Where(e => e.Status != null && e.Status.ToUpper() == "ACTIVE")
                                                    .Select(e => e.EmployeeId)
                                                    .Distinct()
                                                    .ToListAsync();
            var enrolledCount = enrolledEmployeeIds.Count;

            // Employees not having an 'ACTIVE' enrollment (or no enrollment at all)
            var notEnrolledCount = totalEmployees - enrolledCount;

            return Json(new { enrolled = enrolledCount, notEnrolled = notEnrolledCount });
        }





        //Employee api's



        // API: Get Enrolled Policies for a specific Employee
        // URL: /api/employee/enrolled-policies/{employeeId}



        [HttpGet("/api/employee/enrolled-policies/{employeeId}")]
        [Authorize(Roles = "Employee")]
        public async Task<JsonResult> GetEnrolledPoliciesApi(int employeeId)
        {
            // IMPORTANT: Authorize check if the logged-in employeeId matches the requested employeeId
            var loggedInEmployeeIdClaim = User.FindFirst("EmployeeId");
            if (loggedInEmployeeIdClaim == null || !int.TryParse(loggedInEmployeeIdClaim.Value, out int loggedInEmployeeId) || loggedInEmployeeId != employeeId)
            {
                return Json(new { error = "Unauthorized access to employee data." });
            }

            var enrollments = await _context.Enrollments
                                            .Where(e => e.EmployeeId == employeeId)
                                            .Include(e => e.Policy)
                                            .OrderByDescending(e => e.EnrollmentDate) // Order by recent enrollments
                                            .ToListAsync();

            var enrolledPoliciesForFrontend = enrollments.Select(e => new
            {
                enrollmentId = e.EnrollmentId,
                policyId = e.PolicyId,
                policyName = e.Policy?.PolicyName ?? "N/A",
                policyType = e.Policy?.PolicyType ?? "N/A",
                coverageAmount = e.Policy?.CoverageAmount.ToString("N0") ?? "N/A",
                premiumAmount = e.Policy?.PremiumAmount.ToString("N0") ?? "N/A",
                enrollmentDate = e.EnrollmentDate.ToString("yyyy-MM-dd"),
                status = e.Status ?? "N/A"
            }).ToList();

            return Json(enrolledPoliciesForFrontend);
        }

        // API: Get Claims for a specific Employee
        // URL: /api/employee/claims/{employeeId}
        [HttpGet("/api/employee/claims/{employeeId}")]
        [Authorize(Roles = "Employee")]
        public async Task<JsonResult> GetEmployeeClaimsApi(int employeeId)
        {
            // IMPORTANT: Authorize check if the logged-in employeeId matches the requested employeeId
            var loggedInEmployeeIdClaim = User.FindFirst("EmployeeId");
            if (loggedInEmployeeIdClaim == null || !int.TryParse(loggedInEmployeeIdClaim.Value, out int loggedInEmployeeId) || loggedInEmployeeId != employeeId)
            {
                return Json(new { error = "Unauthorized access to claims data." });
            }

            // Fetch claims through enrollments to link to the specific employee
            var claims = await _context.Claims
                                    .Include(c => c.Enrollment)
                                        .ThenInclude(e => e.Employee)
                                    .Include(c => c.Enrollment)
                                        .ThenInclude(e => e.Policy)
                                    .Where(c => c.Enrollment.EmployeeId == employeeId)
                                    .OrderByDescending(c => c.ClaimDate) // Order by recent claims
                                    .ToListAsync();

            var claimsForFrontend = claims.Select(c => new
            {
                id = c.ClaimId,
                enrollmentId = c.EnrollmentId,
                policyName = c.Enrollment?.Policy?.PolicyName ?? "N/A",
                policyType = c.Enrollment?.Policy?.PolicyType ?? "N/A",
                claimDate = c.ClaimDate.ToString("yyyy-MM-dd"),
                requestedAmount = c.ClaimAmount,
                approvedAmount = (c.ClaimStatus?.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) == true ? c.ClaimAmount : 0), // Derived
                status = c.ClaimStatus ?? "N/A"
            }).ToList();

            return Json(claimsForFrontend);
        }

        // API: Get Available Policies (for enrollment)
        // URL: /api/employee/available-policies
        [HttpGet("/api/employee/available-policies")]
        [Authorize(Roles = "Employee")]
        public async Task<JsonResult> GetAvailablePoliciesApi()
        {
            // All policies are considered "available" unless specific logic dictates otherwise
            var policies = await _context.Policies.ToListAsync();

            var availablePoliciesForFrontend = policies.Select(p => new
            {
                id = p.PolicyId,
                name = p.PolicyName,
                type = p.PolicyType,
                coverageAmount = p.CoverageAmount.ToString("N0"),
                premiumAmount = p.PremiumAmount.ToString("N0")
            }).ToList();

            return Json(availablePoliciesForFrontend);
        }

        // API: Submit New Enrollment
        // URL: /api/employee/submit-enrollment
        [HttpPost("/api/employee/submit-enrollment")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> SubmitEnrollmentApi([FromBody] EnrollmentSubmissionModel model)
        {
            // IMPORTANT: Authorize check if the logged-in employeeId matches the submitted employeeId
            var loggedInEmployeeIdClaim = User.FindFirst("EmployeeId");
            if (loggedInEmployeeIdClaim == null || !int.TryParse(loggedInEmployeeIdClaim.Value, out int loggedInEmployeeId) || loggedInEmployeeId != model.EmployeeId)
            {
                return Unauthorized(new { message = "Unauthorized to create enrollment for this employee ID." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if employee is already actively enrolled in this policy
            var existingEnrollment = await _context.Enrollments
                                                .AnyAsync(e => e.EmployeeId == model.EmployeeId && e.PolicyId == model.PolicyId && e.Status.ToUpper() == "ACTIVE");
            if (existingEnrollment)
            {
                return BadRequest(new { message = "Employee is already actively enrolled in this policy." });
            }

            var newEnrollment = new Health_Insurance.Models.Enrollment // Fully qualify Enrollment
            {
                EmployeeId = model.EmployeeId,
                PolicyId = model.PolicyId,
                EnrollmentDate = DateTime.UtcNow, // Set current date
                Status = "PENDING" // Or "ACTIVE" if enrollments are automatic upon submission
            };

            _context.Enrollments.Add(newEnrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrollment submitted successfully!", enrollmentId = newEnrollment.EnrollmentId });
        }

        // Model for Enrollment Submission (internal to this controller, no separate file needed)
        // This is a simple DTO to receive data from the frontend form.
        public class EnrollmentSubmissionModel
        {
            [Required]
            public int EmployeeId { get; set; }
            [Required]
            public int PolicyId { get; set; }
            // No EnrollmentDate needed from frontend, we set it to UtcNow
            // No Status needed from frontend, we set it to "PENDING" or "ACTIVE"
        }


        // API: Submit New Claim
        // URL: /api/employee/submit-claim
        [HttpPost("/api/employee/submit-claim")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> SubmitClaimApi([FromBody] ClaimSubmissionModel model)
        {
            // IMPORTANT: Authorize check if the logged-in employeeId matches the owner of the enrollment
            var loggedInEmployeeIdClaim = User.FindFirst("EmployeeId");
            if (loggedInEmployeeIdClaim == null || !int.TryParse(loggedInEmployeeIdClaim.Value, out int loggedInEmployeeId))
            {
                return Unauthorized(new { message = "Unauthorized access." });
            }

            // Verify the enrollment belongs to the logged-in employee
            var enrollment = await _context.Enrollments
                                            .FirstOrDefaultAsync(e => e.EnrollmentId == model.EnrollmentId && e.EmployeeId == loggedInEmployeeId && e.Status.ToUpper() == "ACTIVE");
            if (enrollment == null)
            {
                return BadRequest(new { message = "Invalid or inactive enrollment for this employee." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newClaim = new Health_Insurance.Models.Claim // Fully qualify Claim
            {
                EnrollmentId = model.EnrollmentId,
                ClaimAmount = model.ClaimAmount,
                ClaimDate = DateTime.UtcNow, // Set current date
                ClaimStatus = "SUBMITTED" // Initial status
            };

            _context.Claims.Add(newClaim);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Claim submitted successfully!", claimId = newClaim.ClaimId });
        }

        // Model for Claim Submission (internal to this controller, no separate file needed)
        public class ClaimSubmissionModel
        {
            [Required]
            public int EnrollmentId { get; set; } // The ID of the policy enrollment the claim is against
            [Required]
            [Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be a positive number.")]
            public decimal ClaimAmount { get; set; }
            // No ClaimDate needed from frontend, we set it to UtcNow
            // No ClaimStatus needed from frontend, we set it to "SUBMITTED"
        }

        // API: Get Policy Details for Premium Calculation/Display
        // URL: /api/employee/policy-details/{policyId}
        [HttpGet("/api/employee/policy-details/{policyId}")]
        [Authorize(Roles = "Employee")]
        public async Task<JsonResult> GetPolicyDetailsApi(int policyId)
        {
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.PolicyId == policyId);

            if (policy == null)
            {
                return Json(new { error = "Policy not found." });
            }

            return Json(new
            {
                id = policy.PolicyId,
                name = policy.PolicyName,
                type = policy.PolicyType,
                coverageAmount = policy.CoverageAmount.ToString("N0"),
                premiumAmount = policy.PremiumAmount.ToString("N0")
            });
        }

    }
}

