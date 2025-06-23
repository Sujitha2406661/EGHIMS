// Services/IReportService.cs
using Health_Insurance.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Health_Insurance.Services
{
    // Interface defining the contract for the Reporting Service.
    public interface IReportService
    {
        // Method to get data for an Employee Report.
        // Returns a list of Employee objects, potentially with related data.
        Task<List<Employee>> GenerateEmployeeReportAsync();

        // Method to get data for a Policy Report.
        // Returns a list of Policy objects, potentially with related data.
        Task<List<Policy>> GeneratePolicyReportAsync();

        // Method to get data for a list of all enrollments
        Task<List<Enrollment>> GenerateAllEnrollmentsReportAsync();

        // Method to get data for a list of all claims
        Task<List<Claim>> GenerateAllClaimsReportAsync();

        // Method to export report data to different formats CSV & PDF
        // The return type can be a byte array for file content, or stream.
        Task<byte[]> ExportReportAsync(string reportType, string format);
    }
}
