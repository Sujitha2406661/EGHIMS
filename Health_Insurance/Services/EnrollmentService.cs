// Services/EnrollmentService.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Health_Insurance.Services
{
    // Implementation of the Enrollment Service interface
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context; // DbContext for database interaction

        // Constructor: Inject the ApplicationDbContext
        public EnrollmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Method to get a list of all available policies from the database
        public async Task<IEnumerable<Policy>> GetAllPoliciesAsync()
        {
            // Retrieve all policies asynchronously
            return await _context.Policies.ToListAsync();
        }

        // Method to get a list of policies an employee is currently enrolled in
        public async Task<IEnumerable<Enrollment>> GetEnrolledPoliciesByEmployeeIdAsync(int employeeId)
        {
            // Retrieve enrollments for a specific employee
            // Include Policy and Employee navigation properties to load related data
            // Filter by employeeId
            return await _context.Enrollments
                .Include(e => e.Policy)
                .Include(e => e.Employee)
                .Where(e => e.EmployeeId == employeeId)
                .ToListAsync();
        }

        // Method to handle the enrollment of an employee into a specific policy
        public async Task<bool> EnrollEmployeeInPolicyAsync(int employeeId, int policyId)
        {
            // Basic validation: Check if employee and policy exist in the database
            var employee = await _context.Employees.FindAsync(employeeId);
            var policy = await _context.Policies.FindAsync(policyId);

            if (employee == null || policy == null)
            {
                // If employee or policy is not found, enrollment cannot proceed
                return false;
            }

            // Basic validation: Check if the employee is already actively enrolled in this policy
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.PolicyId == policyId && e.Status == "ACTIVE");

            if (existingEnrollment != null)
            {
                // If an active enrollment already exists, return false
                return false;
            }

            // Create a new Enrollment entity
            var newEnrollment = new Enrollment
            {
                EmployeeId = employeeId,
                PolicyId = policyId,
                EnrollmentDate = DateTime.UtcNow,
                Status = "ACTIVE" // Set the initial status to ACTIVE
            };

            // Add the new enrollment to the DbContext
            _context.Enrollments.Add(newEnrollment);
            // Save the changes to the database
            var result = await _context.SaveChangesAsync();

            // Check if SaveChangesAsync successfully wrote at least one entity
            return result > 0;
        }

        // Method to handle the cancellation of an employee's enrollment
        public async Task<bool> CancelEnrollmentAsync(int enrollmentId)
        {
            // Find the enrollment record by its ID
            var enrollment = await _context.Enrollments.FindAsync(enrollmentId);

            if (enrollment == null)
            {
                // If enrollment is not found, cancellation cannot proceed
                return false;
            }

            // Optional validation: Only allow cancellation if the current status is ACTIVE
            if (enrollment.Status != "ACTIVE")
            {
                // Cannot cancel an enrollment that is not currently ACTIVE
                return false;
            }

            // Update the status of the enrollment to CANCELLED
            enrollment.Status = "CANCELLED";

            // Mark the entity as modified in the DbContext
            _context.Enrollments.Update(enrollment);
            // Save the changes to the database
            var result = await _context.SaveChangesAsync();

            // Check if SaveChangesAsync successfully wrote at least one entity
            return result > 0;
        }
    }
}

