// Services/ReportService.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace Health_Insurance.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GenerateEmployeeReportAsync()
        {
            return await _context.Employees.Include(e => e.Organization).OrderBy(e => e.Name).ToListAsync();
        }

        public async Task<List<Policy>> GeneratePolicyReportAsync()
        {
            return await _context.Policies.OrderBy(p => p.PolicyName).ToListAsync();
        }

        public async Task<List<Enrollment>> GenerateAllEnrollmentsReportAsync()
        {
            return await _context.Enrollments
                                 .Include(e => e.Employee)
                                 .Include(e => e.Policy)
                                 .OrderBy(e => e.EnrollmentId)
                                 .ToListAsync();
        }

        public async Task<List<Claim>> GenerateAllClaimsReportAsync()
        {
            return await _context.Claims
                                 .Include(c => c.Enrollment)
                                     .ThenInclude(e => e.Employee)
                                 .Include(c => c.Enrollment)
                                     .ThenInclude(e => e.Policy)
                                 .OrderBy(c => c.ClaimId)
                                 .ToListAsync();
        }

        public async Task<byte[]> ExportReportAsync(string reportType, string format)
        {
            format = format?.ToLower();

            if (format == "csv")
            {
                using (var writer = new StringWriter())
                {
                    // Existing CSV generation logic
                    switch (reportType.ToLower())
                    {
                        case "employee":
                            var employees = await GenerateEmployeeReportAsync();
                            writer.WriteLine("EmployeeId,Name,Email,Phone,Address,Designation,OrganizationId,OrganizationName,Username");
                            foreach (var emp in employees)
                            {
                                writer.WriteLine($"{emp.EmployeeId},{EscapeCsv(emp.Name)},{EscapeCsv(emp.Email)},{EscapeCsv(emp.Phone)},{EscapeCsv(emp.Address)},{EscapeCsv(emp.Designation)},{emp.OrganizationId},{EscapeCsv(emp.Organization?.OrganizationName)},{EscapeCsv(emp.Username)}");
                            }
                            break;

                        case "policy":
                            var policies = await GeneratePolicyReportAsync();
                            writer.WriteLine("PolicyId,PolicyName,CoverageAmount,PremiumAmount,PolicyType");
                            foreach (var pol in policies)
                            {
                                writer.WriteLine($"{pol.PolicyId},{EscapeCsv(pol.PolicyName)},{pol.CoverageAmount},{pol.PremiumAmount},{EscapeCsv(pol.PolicyType)}");
                            }
                            break;

                        case "enrollment":
                            var enrollments = await GenerateAllEnrollmentsReportAsync();
                            writer.WriteLine("EnrollmentId,EmployeeName,PolicyName,EnrollmentDate");
                            foreach (var enr in enrollments)
                            {
                                writer.WriteLine($"{enr.EnrollmentId},{EscapeCsv(enr.Employee?.Name)},{EscapeCsv(enr.Policy?.PolicyName)},{enr.EnrollmentDate:yyyy-MM-dd}");
                            }
                            break;

                        case "claim":
                            var claims = await GenerateAllClaimsReportAsync();
                            writer.WriteLine("ClaimId,EnrollmentId,EmployeeName,PolicyName,ClaimAmount,ClaimDate,ClaimReason,ClaimStatus");
                            foreach (var clm in claims)
                            {
                                writer.WriteLine($"{clm.ClaimId},{clm.EnrollmentId},{EscapeCsv(clm.Enrollment?.Employee?.Name)},{EscapeCsv(clm.Enrollment?.Policy?.PolicyName)},{clm.ClaimAmount},{clm.ClaimDate:yyyy-MM-dd},{EscapeCsv(clm.ClaimReason)},{EscapeCsv(clm.ClaimStatus)}");
                            }
                            break;

                        default:
                            throw new ArgumentException("Invalid report type for CSV export.");
                    }
                    return Encoding.UTF8.GetBytes(writer.ToString());
                }
            }
            else if (format == "pdf")
            {
                QuestPDF.Settings.License = LicenseType.Community;

                byte[] pdfBytes;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(36);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        // Header (repeats on every page) - Corrected layout for line and padding
                        page.Header()
                            .Column(column =>
                            {
                                column.Item().Text($"Health Insurance Report - {reportType.ToUpper()} - Generated on {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2)
                                    .AlignCenter();

                                column.Item() // Apply padding to this container item
                                    .PaddingBottom(10f) // NEW FIX: Correctly applies padding to the container
                                    .LineHorizontal(1f); // The line itself
                            });


                        // Content section
                        page.Content()
                            .PaddingVertical(10)
                            .Column(column =>
                            {
                                column.Spacing(10);

                                switch (reportType.ToLower())
                                {
                                    case "employee":
                                        var employees = GenerateEmployeeReportAsync().Result;
                                        column.Item().PaddingBottom(5f).Text("Employee Data").FontSize(12).Bold(); // NEW FIX: Padding applied to item
                                        column.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(1);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(2);
                                            });

                                            table.Header(header =>
                                            {
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("ID").Bold(); // NEW FIX: float literals
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Name").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Email").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Phone").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Organization").Bold();
                                            });

                                            foreach (var emp in employees)
                                            {
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(emp.EmployeeId.ToString()); // NEW FIX: float literals
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(emp.Name);
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(emp.Email);
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(emp.Phone);
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(emp.Organization?.OrganizationName ?? "N/A");
                                            }
                                        });
                                        break;

                                    case "policy":
                                        var policies = GeneratePolicyReportAsync().Result;
                                        column.Item().PaddingBottom(5f).Text("Policy Data").FontSize(12).Bold(); // NEW FIX: Padding applied to item
                                        column.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(1);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(2);
                                            });

                                            table.Header(header =>
                                            {
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("ID").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Name").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Coverage").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Premium").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Type").Bold();
                                            });

                                            foreach (var pol in policies)
                                            {
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(pol.PolicyId.ToString());
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(pol.PolicyName);
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(pol.CoverageAmount.ToString("C"));
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(pol.PremiumAmount.ToString("C"));
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(pol.PolicyType);
                                            }
                                        });
                                        break;

                                    case "enrollment":
                                        var enrollments = GenerateAllEnrollmentsReportAsync().Result;
                                        column.Item().PaddingBottom(5f).Text("Enrollment Data").FontSize(12).Bold(); // NEW FIX: Padding applied to item
                                        column.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(1);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn(1.5f);
                                            });

                                            table.Header(header =>
                                            {
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Enrollment ID").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Employee").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Policy").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Enroll Date").Bold();
                                            });

                                            foreach (var enr in enrollments)
                                            {
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(enr.EnrollmentId.ToString());
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(enr.Employee?.Name ?? "N/A");
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(enr.Policy?.PolicyName ?? "N/A");
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(enr.EnrollmentDate.ToString("yyyy-MM-dd"));
                                            }
                                        });
                                        break;

                                    case "claim":
                                        var claims = GenerateAllClaimsReportAsync().Result;
                                        column.Item().PaddingBottom(5f).Text("Claim Data").FontSize(12).Bold(); // NEW FIX: Padding applied to item
                                        column.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(1);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(1.5f);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn(1.5f);
                                            });

                                            table.Header(header =>
                                            {
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Claim ID").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Employee").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Policy").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Amount").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Claim Date").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Reason").Bold();
                                                header.Cell().BorderBottom(1f).Padding(5f).Text("Status").Bold();
                                            });

                                            foreach (var clm in claims)
                                            {
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.ClaimId.ToString()); // NEW FIX: float literals
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.Enrollment?.Employee?.Name ?? "N/A");
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.Enrollment?.Policy?.PolicyName ?? "N/A");
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.ClaimAmount.ToString("C"));
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.ClaimDate.ToString("yyyy-MM-dd"));
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.ClaimReason);
                                                table.Cell().BorderBottom(0.5f).Padding(5f).Text(clm.ClaimStatus);
                                            }
                                        });
                                        break;

                                    default:
                                        column.Item().Text($"No specific PDF layout defined for '{reportType}'.");
                                        break;
                                }
                            });

                        // Footer (repeats on every page)
                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ").FontSize(9);
                                x.CurrentPageNumber().FontSize(9);
                                x.Span(" of ").FontSize(9);
                                x.TotalPages().FontSize(9);
                            });
                    });
                });

                pdfBytes = document.GeneratePdf();
                return pdfBytes;
            }
            else if (format == "excel")
            {
                throw new NotSupportedException($"Export to {format.ToUpper()} is not yet implemented. Please integrate EPPlus or similar library.");
            }
            else
            {
                throw new ArgumentException("Unsupported export format.");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}