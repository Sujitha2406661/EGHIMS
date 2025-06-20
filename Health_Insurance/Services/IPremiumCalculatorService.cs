// Services/IPremiumCalculatorService.cs
using Health_Insurance.Models;
using System.Threading.Tasks;

namespace Health_Insurance.Services
{
    // Interface defining the contract for the Premium Calculator Service
    public interface IPremiumCalculatorService
    {
        // Method to calculate the premium for a specific employee and policy
        // This method will take Employee and Policy objects or IDs as input
        // and return the calculated premium amount.
        Task<decimal> CalculatePremiumAsync(int employeeId, int policyId);
    }
}

