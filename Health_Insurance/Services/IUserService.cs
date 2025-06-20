using Health_Insurance.Models;
using System.Threading.Tasks;

namespace Health_Insurance.Services 
{
    // Interface defining the contract for the User (Authentication) Service
    public interface IUserService
    {
        // Method to authenticate a user, now including the expected LoginType
        // Returns the authenticated Employee or Admin object, or null if authentication fails.
        // The returned object will indicate the user's role.
        Task<object> AuthenticateUserAsync(string username, string password, string loginType); // Added loginType parameter

        // Method to hash a password for secure storage
        string HashPassword(string password);

        // Method to verify a password against a stored hash
        bool VerifyPassword(string password, string hashedPassword);
    }
}
