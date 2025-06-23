
namespace Health_Insurance.Models
{


    public class HRDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalClaimsSubmitted { get; set; }
        public int PendingClaimsCount { get; set; }
        public int ActivePoliciesCount { get; set; }

        public List<Enrollment> RecentEnrollments { get; set; } = new List<Enrollment>();
        public List<Claim> RecentClaims { get; set; } = new List<Claim>();
    }
}