// Controllers/EmployeeController.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Health_Insurance.Services; // Add this using statement for IUserService
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System; // For DateTime

namespace Health_Insurance.Controllers
{
    // Restrict all actions in this controller to users with the "Admin" or "HR" role.
    [Authorize(Roles = "Admin,HR")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService; // Inject IUserService for password hashing

        // Constructor: Inject ApplicationDbContext AND IUserService
        public EmployeeController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService; // Initialize the injected service
        }

        // GET: Employee
        // Displays a list of all employees.
        public async Task<IActionResult> Index()
        {
            // Include Organization for displaying organization name
            var applicationDbContext = _context.Employees.Include(e => e.Organization);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Employee/Details/5
        // Displays details of a specific employee.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employee/Create
        // Displays the form for creating a new employee.
        public IActionResult Create()
        {
            // Populate dropdown for Organizations
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName");
            return View();
        }

        // POST: Employee/Create
        // Handles the form submission for creating a new employee.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Bind properties from the form, excluding EmployeeId and CreateDate (as they are set internally)
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address,Designation,OrganizationId,Username,PasswordHash")] Employee employee)
        {
            // Manual validation for PasswordHash as it's part of the Employee model, but handled here.
            if (string.IsNullOrEmpty(employee.PasswordHash))
            {
                ModelState.AddModelError(nameof(employee.PasswordHash), "Password is required.");
            }

            // Check if OrganizationId is valid before proceeding
            // This is important because the dropdown might submit an empty string if nothing is selected.
            if (employee.OrganizationId <= 0)
            {
                ModelState.AddModelError(nameof(employee.OrganizationId), "Please select a valid organization.");
            }


            if (ModelState.IsValid) // Checks all Data Annotations (including the new ones for Name/Phone) and manual checks
            {
                // Hash the password using the injected IUserService
                employee.PasswordHash = _userService.HashPassword(employee.PasswordHash);

                // --- NEW: Set CreateDate to the current UTC time automatically ---
                employee.CreateDate = DateTime.UtcNow; // Using UtcNow is generally best practice for timestamps
                // --- END NEW ---

                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repopulate OrganizationId dropdown if ModelState is invalid and view is returned
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", employee.OrganizationId);
            return View(employee);
        }

        // GET: Employee/Edit/5
        // Displays the form for editing an existing employee.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Map Employee model to EmployeeEditViewModel for the view
            var viewModel = new EmployeeEditViewModel
            {
                EmployeeId = employee.EmployeeId,
                Name = employee.Name,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address,
                Designation = employee.Designation,
                OrganizationId = employee.OrganizationId,
                Username = employee.Username,
                Password = null // Do NOT populate password for security
            };

            // Repopulate OrganizationId dropdown
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", employee.OrganizationId);
            return View(viewModel);
        }

        // POST: Employee/Edit/5
        // Handles the form submission for editing an employee.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeEditViewModel viewModel)
        {
            if (id != viewModel.EmployeeId)
            {
                return NotFound();
            }

            // If the Password field in the ViewModel is empty, remove it from ModelState validation
            // as it's optional for edits.
            if (ModelState.ContainsKey(nameof(viewModel.Password)) && string.IsNullOrEmpty(viewModel.Password))
            {
                ModelState.Remove(nameof(viewModel.Password));
            }

            if (ModelState.IsValid)
            {
                // Retrieve the existing employee entity from the database
                var employeeToUpdate = await _context.Employees.FindAsync(id);

                if (employeeToUpdate == null)
                {
                    return NotFound();
                }

                // Update properties from the ViewModel to the existing employee entity
                employeeToUpdate.Name = viewModel.Name;
                employeeToUpdate.Email = viewModel.Email;
                employeeToUpdate.Phone = viewModel.Phone;
                employeeToUpdate.Address = viewModel.Address;
                employeeToUpdate.Designation = viewModel.Designation;
                employeeToUpdate.OrganizationId = viewModel.OrganizationId;
                employeeToUpdate.Username = viewModel.Username;

                // Only update PasswordHash if a new password was provided in the ViewModel, using IUserService
                if (!string.IsNullOrEmpty(viewModel.Password))
                {
                    employeeToUpdate.PasswordHash = _userService.HashPassword(viewModel.Password);
                }
                // CreateDate is NOT updated here, as it represents the initial creation timestamp.

                try
                {
                    _context.Update(employeeToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employeeToUpdate.EmployeeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Repopulate OrganizationId dropdown if ModelState is invalid and view is returned
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "OrganizationId", "OrganizationName", viewModel.OrganizationId);
            return View(viewModel); // Return the view with validation errors
        }


        // GET: Employee/Delete/5
        // Displays the confirmation page for deleting an employee.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        // Handles the deletion of an employee....
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}
