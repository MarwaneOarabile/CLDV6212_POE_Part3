using ABCRetailers_ST10436124.Data;
using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Models.ViewModels;
using ABCRetailers_ST10436124.Services;     
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ABCRetailers_ST10436124.Controllers
{
    public class LoginController(AuthDbContext context, ILogger<LoginController> logger, IFunctionsApi functionsApi) : Controller
    {
        private readonly AuthDbContext _context = context;
        private readonly ILogger<LoginController> _logger = logger;
        private readonly IFunctionsApi _functionsApi = functionsApi;



        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                try
                {
                    // Find user by username or email
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Username);

                    if (user != null && VerifyPassword(model.Password, user.Password))
                    {
                        await SignInUser(user, model.RememberMe);

                        // Update last login
                        user.LastLogin = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("User {Username} logged in successfully", user.Username);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        // Redirect based on role
                        return user.Role == "Admin"
                            ? RedirectToAction("AdminDashboard", "Home")
                            : RedirectToAction("CustomerDashboard", "Home");
                    }

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login for user {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "An error occurred during login.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username or email already exists
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                        return View(model);
                    }

                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email already exists.");
                        return View(model);
                    }

                    // Create new user in Identity
                    var user = new User
                    {
                        Email = model.Email,
                        Username = model.Username,
                        Password = HashPassword(model.Password),
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        ShippingAddress = model.ShippingAddress,
                        Role = "Customer",
                        CreatedDate = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(); // Save to get UserId

                    // FIX: Create corresponding Customer record in Azure Table Storage
                    var customer = new Customer
                    {
                        Name = model.FirstName,
                        Surname = model.LastName,
                        Username = model.Username,
                        Email = model.Email,
                        ShippingAddress = model.ShippingAddress
                    };

                    // Link the Customer to the User
                    customer.RowKey = user.UserId.ToString(); // Use UserId as RowKey for linking

                    await _functionsApi.CreateCustomerAsync(customer); // This was the missing line!

                    // Automatically log in the user after registration
                    await SignInUser(user, false);

                    _logger.LogInformation("New user registered: {Username} with Customer ID: {CustomerId}",
                        user.Username, customer.RowKey);

                    TempData["Success"] = "Registration successful! Welcome to ABC Retailers.";
                    return RedirectToAction("CustomerDashboard", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during user registration for {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "An error occurred during registration.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUser(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("FullName", $"{user.FirstName} {user.LastName}".Trim())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private static string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPassword(string inputPassword, string storedPassword)
        {
            return HashPassword(inputPassword) == storedPassword;
        }
    }
}
