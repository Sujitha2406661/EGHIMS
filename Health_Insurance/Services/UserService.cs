// Services/UserService.cs
using Health_Insurance.Data;
using Health_Insurance.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Health_Insurance.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string HashPassword(string password)
        {
            Console.WriteLine("hashed password is :");
            Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(password));
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<object> AuthenticateUserAsync(string username, string password, string loginType)
        {
            if (loginType == "Admin")
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
                //Console.WriteLine(password);
                if (admin != null && VerifyPassword(password, admin.PasswordHash))
                {
                    return admin;
                }
            }
            else if (loginType == "Employee")
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Username == username);
                if (employee != null && VerifyPassword(password, employee.PasswordHash))
                {
                    return employee;
                }
            }
            else if (loginType == "HR")
            {
                var hr = await _context.HRs.FirstOrDefaultAsync(h => h.Username == username);
                if (hr != null && VerifyPassword(password, hr.PasswordHash))
                {
                    return hr;
                }
            }
            return null; // Authentication failed for the specified loginType
        }
    }
}

