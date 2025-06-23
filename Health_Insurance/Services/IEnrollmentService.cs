// Services/IEnrollmentService.cs
using Health_Insurance.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Health_Insurance.Services
{
    // Interface defining the contract for the Enrollment Service
    public interface IEnrollmentService
    {
        // Method to get a list of all available policies
        Task<IEnumerable<Policy>> GetAllPoliciesAsync();

        // Method to get a list of policies an employee is enrolled in
        Task<IEnumerable<Enrollment>> GetEnrolledPoliciesByEmployeeIdAsync(int employeeId);

        // Method to handle the enrollment of an employee into a policy
        Task<bool> EnrollEmployeeInPolicyAsync(int employeeId, int policyId);

        // Method to handle the cancellation of an employee's enrollment
        Task<bool> CancelEnrollmentAsync(int enrollmentId);
    }
}

