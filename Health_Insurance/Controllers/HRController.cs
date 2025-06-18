// Controllers/HRController.cs
using Health_Insurance.Data; // Ensure this namespace is correct for your DbContext
using Health_Insurance.Models; // Ensure this namespace is correct for your Models
using Health_Insurance.Services; // Ensure this namespace is correct for IUserService
using Microsoft.AspNetCore.Authorization; // For [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For ToListAsync, FindAsync, Any, Include
using System.Linq; // For Any
using System.Threading.Tasks;

namespace Health_Insurance.Controllers
{
    // Restrict all actions in this controller to users with the "Admin" role.
    [Authorize(Roles = "Admin")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService; // Inject IUserService for password hashing

        // Constructor: Inject ApplicationDbContext and IUserService
        public HRController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: /HR/Index
        // Displays a list of all HR personnel.
        public async Task<IActionResult> Index()
        {
            return View(await _context.HRs.ToListAsync());
        }

        // GET: /HR/Details/5
        // Displays details of a specific HR personnel.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hr = await _context.HRs.FirstOrDefaultAsync(m => m.HRId == id);
            if (hr == null)
            {
                return NotFound();
            }

            return View(hr);
        }

        // GET: /HR/Create
        // Displays the form for creating a new HR personnel.
        public IActionResult Create()
        {
            return View();
        }

        // POST: /HR/Create
        // Handles the form submission for creating a new HR personnel.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Username,PasswordHash")] HR hr)
        {
            // Hash the password before saving a new HR personnel
            if (!string.IsNullOrEmpty(hr.PasswordHash))
            {
                hr.PasswordHash = _userService.HashPassword(hr.PasswordHash);
            }
            else
            {
                // Add a model error if password is required for new HR creation but is empty
                ModelState.AddModelError(nameof(hr.PasswordHash), "Password is required for new HR personnel.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(hr);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hr);
        }

        // GET: /HR/Edit/5
        // Displays the form for editing an existing HR personnel.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hr = await _context.HRs.FindAsync(id);
            if (hr == null)
            {
                return NotFound();
            }

            // Map the HR model to the HREditViewModel for the view
            var viewModel = new HREditViewModel
            {
                HRId = hr.HRId,
                Name = hr.Name,
                Email = hr.Email,
                Phone = hr.Phone,
                Username = hr.Username,
                Password = null // Do NOT populate password for security
            };

            return View(viewModel);
        }

        // POST: /HR/Edit/5
        // Handles the form submission for editing an HR personnel.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HREditViewModel viewModel)
        {
            if (id != viewModel.HRId)
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
                // Retrieve the existing HR entity from the database
                var hrToUpdate = await _context.HRs.FindAsync(id);

                if (hrToUpdate == null)
                {
                    return NotFound();
                }

                // Update properties from the ViewModel to the existing HR entity
                hrToUpdate.Name = viewModel.Name;
                hrToUpdate.Email = viewModel.Email;
                hrToUpdate.Phone = viewModel.Phone;
                hrToUpdate.Username = viewModel.Username;

                // Only update PasswordHash if a new password was provided in the ViewModel
                if (!string.IsNullOrEmpty(viewModel.Password))
                {
                    hrToUpdate.PasswordHash = _userService.HashPassword(viewModel.Password);
                }
                // If viewModel.Password is null/empty, the existing hrToUpdate.PasswordHash remains unchanged.

                try
                {
                    _context.Update(hrToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HRExists(hrToUpdate.HRId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // Re-throw for proper error handling
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(); // Return the view with validation errors
        }

        // GET: /HR/Delete/5
        // Displays the confirmation page for deleting an HR personnel.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hr = await _context.HRs.FirstOrDefaultAsync(m => m.HRId == id);
            if (hr == null)
            {
                return NotFound();
            }

            return View(hr);
        }

        // POST: /HR/Delete/5
        // Handles the deletion of an HR personnel.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hr = await _context.HRs.FindAsync(id);
            if (hr != null)
            {
                _context.HRs.Remove(hr);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if an HR personnel exists.
        private bool HRExists(int id)
        {
            return _context.HRs.Any(e => e.HRId == id);
        }
    }
}