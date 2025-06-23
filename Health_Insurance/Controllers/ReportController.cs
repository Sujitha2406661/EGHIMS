// Controllers/ReportController.cs
using Health_Insurance.Models;
using Health_Insurance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Health_Insurance.Controllers
{
    // Restrict all actions in this controller to users with the "Admin" or "HR" role.
    [Authorize(Roles = "Admin,HR")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService; // Inject the ReportService

        // Constructor to inject IReportService
        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: /Report/Index (A landing page for reports, or list all report types)
        public IActionResult Index()
        {
            // You can list available reports here
            return View();
        }

        // GET: /Report/GenerateEmployeeReport
        // Generates and displays an HTML table of employee data.
        public async Task<IActionResult> GenerateEmployeeReport()
        {
            var employees = await _reportService.GenerateEmployeeReportAsync();
            return View(employees);
        }

        // GET: /Report/GeneratePolicyReport
        // Generates and displays an HTML table of policy data.
        public async Task<IActionResult> GeneratePolicyReport()
        {
            var policies = await _reportService.GeneratePolicyReportAsync();
            return View(policies);
        }

        // GET: /Report/GenerateEnrollmentReport
        // Generates and displays an HTML table of enrollment data.
        public async Task<IActionResult> GenerateEnrollmentReport()
        {
            var enrollments = await _reportService.GenerateAllEnrollmentsReportAsync();
            return View(enrollments);
        }

        // GET: /Report/GenerateClaimReport
        // Generates and displays an HTML table of claim data.
        public async Task<IActionResult> GenerateClaimReport()
        {
            var claims = await _reportService.GenerateAllClaimsReportAsync();
            return View(claims);
        }


        // GET: /Report/ExportReport?reportType=Employee&format=CSV
        // Handles exporting reports to different formats.
        public async Task<IActionResult> ExportReport(string reportType, string format)
        {
            if (string.IsNullOrEmpty(reportType) || string.IsNullOrEmpty(format))
            {
                return BadRequest("Report type and format are required.");
            }

            try
            {
                byte[] fileBytes = await _reportService.ExportReportAsync(reportType, format);
                string fileName = $"{reportType}_Report.{format.ToLower()}";
                string contentType = string.Empty;

                // Determine content type based on format
                switch (format.ToLower())
                {
                    case "csv":
                        contentType = "text/csv";
                        break;
                    case "excel":
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;
                    case "pdf":
                        contentType = "application/pdf"
                        break;
                    default:
                        return BadRequest("Unsupported format.");
                }

                return File(fileBytes, contentType, fileName);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (System.NotSupportedException ex)
            {
                return StatusCode(501, ex.Message); // 501 Not Implemented
            }
            catch (System.Exception ex)
            {
                // Log the exception for debugging
                return StatusCode(500, $"An error occurred while generating the report: {ex.Message}");
            }
        }
    }
}
