// Program.cs
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Health_Insurance.Data;
using Health_Insurance.Models;
using Health_Insurance.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQL Server database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Ensure you have the service for UseMigrationsEndPoint to function.
// This is typically added when you scaffold Identity or if you manually add it.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// Configure Authentication (Cookie-based)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Path for access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie expiration
        options.SlidingExpiration = true; // Renew cookie on activity
    });

// Configure Authorization policies (if needed later)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee"));
    options.AddPolicy("HROnly", policy => policy.RequireRole("HR")); // Explicitly add HR policy
});


// Register your custom services for Dependency Injection
// User Service
builder.Services.AddScoped<IUserService, UserService>();
// Claim Service
builder.Services.AddScoped<IClaimService, ClaimService>();
// Enrollment Service
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
// Premium Calculator Service
builder.Services.AddScoped<IPremiumCalculatorService, PremiumCalculatorService>();
// Register Report Service
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) // Seeding logic should primarily run in development
{
    app.UseMigrationsEndPoint(); // If you use EF Core Migrations UI

    // --- Start Initial Database Seeding for Development ---
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>(); // Get logger for output

            // Apply any pending database migrations
            logger.LogInformation("Applying pending database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied.");

            // Seed Admin User if not exists
            if (!context.Admins.Any())
            {
                logger.LogInformation("Seeding initial Admin user...");
                var adminUser = new Admin
                {
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Username = "admin@example.com",
                    Name = "System Administrator" // Add other properties as per your Admin model
                };
                context.Admins.Add(adminUser);
                context.SaveChanges();
                logger.LogInformation("Admin user 'admin' seeded successfully.");
            }
            else
            {
                logger.LogInformation("Admin user already exists. Skipping admin seeding.");
            }

            // Seed HR User if not exists (assuming you have an HR DbSet in your DbContext)
            if (!context.HRs.Any()) // Assuming 'HRs' is the DbSet for HR users
            {
                logger.LogInformation("Seeding initial HR user...");
                var hrUser = new HR
                {
                    Username = "hr",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("HR@123"),
                    Email = "hr@example.com",
                    Name = "Human Resources" // Add other properties as per your HR model
                };
                context.HRs.Add(hrUser);
                context.SaveChanges();
                logger.LogInformation("HR user 'hr' seeded successfully.");
            }
            else
            {
                logger.LogInformation("HR user already exists. Skipping HR seeding.");
            }

            // You can add similar logic for initial Employee users if needed

        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
            // In a production environment, you might want to rethrow the exception
            // or perform more robust error reporting.
        }
    }
    // --- End Initial Database Seeding ---
}
else // Production environment configuration
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable Authentication middleware
app.UseAuthentication();
// Enable Authorization middleware
app.UseAuthorization();

// Map controllers to routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Changed default route to Account/Login

// --- TEMPORARY CODE TO GENERATE PASSWORD HASH ---
// Run the application, check the console output for the hashed password,
// then STOP the application and REMOVE this block.
//string plainTextPasswordToHash = "Admin@123"; // <<< CHANGE THIS TO YOUR DESIRED PASSWORD
//string hashedPasswordForManualInsert = BCrypt.Net.BCrypt.HashPassword(plainTextPasswordToHash);
//Console.WriteLine($"\n=======================================================");
//Console.WriteLine($"TEMPORARY HASH GENERATOR:");
//Console.WriteLine($"Plain text password: '{plainTextPasswordToHash}'");
//Console.WriteLine($"Generated Hashed Password: '{hashedPasswordForManualInsert}'");
//Console.WriteLine($"\n>> Copy this hash and use it for manual database insertion. <<");
//Console.WriteLine($"\n=======================================================");
// --- END TEMPORARY CODE ---


app.Run();