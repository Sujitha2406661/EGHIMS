// Services/ClaimService.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Health_Insurance.Services
{
    // Implementation of the Claim Service
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context; // DbContext for database interaction

        // Constructor: Inject the ApplicationDbContext
        public ClaimService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Method to submit a new claim
        public async Task<bool> SubmitClaimAsync(Claim claim)
        {
            // Basic validation: Ensure the associated Enrollment exists
            var enrollment = await _context.Enrollments.FindAsync(claim.EnrollmentId);
            if (enrollment == null)
            {
                // Enrollment not found, cannot submit claim
                return false;
            }

            // Set initial status and date if not already set
            if (string.IsNullOrEmpty(claim.ClaimStatus))
            {
                claim.ClaimStatus = "SUBMITTED"; // Default status
            }
            if (claim.ClaimDate == default(DateTime))
            {
                claim.ClaimDate = DateTime.UtcNow; // Default date
            }


            // Add the new claim to the DbContext
            _context.Claims.Add(claim);
            // Save changes to the database
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        // Method to get details of a specific claim by ID
        public async Task<Claim> GetClaimDetailsAsync(int claimId)
        {
            // Find the claim by ID, include the related Enrollment and Policy for details
            return await _context.Claims
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Policy)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);
        }

        // Method to update the status of a claim
        public async Task<bool> UpdateClaimStatusAsync(int claimId, string newStatus)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
            {
                // Claim not found
                return false;
            }

            // Basic validation for allowed status values (optional, could be more robust)
            var allowedStatuses = new[] { "SUBMITTED", "APPROVED", "REJECTED" };
            if (!allowedStatuses.Contains(newStatus.ToUpper()))
            {
                // Invalid status provided
                return false;
            }

            // Update the claim status
            claim.ClaimStatus = newStatus;

            // Mark the entity as modified
            _context.Claims.Update(claim);
            // Save changes to the database
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        // Method to list all claims
        public async Task<IEnumerable<Claim>> ListAllClaimsAsync()
        {
            // Retrieve all claims, include related Enrollment and Policy for display
            return await _context.Claims
               .Include(c => c.Enrollment)
               .ThenInclude(e => e.Policy)
               .ToListAsync();
        }
    }
}

