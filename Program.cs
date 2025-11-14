using ABCRetailers_ST10436124.Data;
using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ABCRetailers_ST10436124
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register both services for backward compatibility during transition
            builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();
            builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

            // Add HttpClient for Functions API
            builder.Services.AddHttpClient<FunctionsApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Add Entity Framework and AuthDbContext
            builder.Services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSQL")));

            // Add Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "ABCRetailersAuth";
                    options.LoginPath = "/Login/Login";
                    options.AccessDeniedPath = "/Login/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                });

            // Add Authorization with AuthorizationBuilder
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                options.AddPolicy("CustomerOnly", policy =>
                    policy.RequireRole("Customer"));
            });

            // Add this to handle unauthorized access
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Login/Login";
                options.AccessDeniedPath = "/Home/AccessDenied";
            });

            // Logging
            builder.Services.AddLogging();

            var app = builder.Build();

            // Set culture for decimal handling 
            var culture = new CultureInfo("en-ZA");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Add Authentication & Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AuthDbContext>();
                    context.Database.EnsureCreated();

                    // Seed initial admin user if none exists
                    await SeedAdminUser(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while initializing the database.");
                }
            }

            await app.RunAsync();
        }

        private static async Task SeedAdminUser(AuthDbContext context)
        {
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var adminUser = new User
                {
                    Email = "admin@abcretailers.com",
                    Username = "admin",
                    Password = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Admin123!")),
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "Admin",
                    CreatedDate = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}