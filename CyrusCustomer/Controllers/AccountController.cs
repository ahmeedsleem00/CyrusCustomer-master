using CyrusCustomer.DAL;
using CyrusCustomer.Domain;
using CyrusCustomer.Domain.Models;
using CyrusCustomer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CyrusCustomer.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(ApplicationDbContext context,
                SignInManager<IdentityUser> signInManager,
                UserManager<IdentityUser> userManager)
        {
            this._context = context;
            this._signInManager = signInManager;
            this._userManager = userManager;
        }

            [HttpGet]
        [Authorize]
            //[Authorize(Roles = "Admin")]
            public IActionResult Register()
            {
            var user = User.Identity.Name;
            if (user != "admin@Cyrus.com")
            {
                return Unauthorized(); // or RedirectToAction("AccessDenied", "Account");
            }

            return View();
            }

            [HttpPost]
        //[Authorize(Roles = "Admin")]
        [Authorize]
            public async Task<IActionResult> Register(RegisterViewModel model)
            {
            if (ModelState.IsValid)
            {
                //var user = new IdentityUser { UserName = model.Email, Email = model.Email };

                //var result = await _userManager.CreateAsync(user, model.Password);


                //if (result.Succeeded)
                //{
                //    await _signInManager.SignInAsync(user, isPersistent: false);
                //    return RedirectToAction("Index", "Customer");
                //}
                //foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); }

                //var user = User.Identity.Name;
                //if (user != "admin@Cyrus.com")
                //{
                //    return Unauthorized(); // or RedirectToAction("AccessDenied", "Account");
                //}

                // Logic to create a new user
                // Example:
                //var credential = new Credential
                //{
                //    Email = model.Email,
                //    Password = model.Password, // Make sure to hash this
                //    Name = model.Name,
                //    //CustomerId = model.CustomerId // This should be set accordingly
                //};

                //_context.Credentials.Add(credential);
                //await _context.SaveChangesAsync();

                //return RedirectToAction("Index", "Home");

                if (ModelState.IsValid)
                {
                    // Check if a user with the same email already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError(string.Empty, "A user with this email already exists.");
                        return View(model);
                    }

                    // Create a new IdentityUser (or your custom ApplicationUser class)
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        PasswordHash = model.Password
                        // Add any additional fields from your model if necessary (e.g., Name)
                    };

                    // Hash the password and create the user in Identity
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        //// Optionally, assign a role to the user (e.g., 'User')
                        //await _userManager.AddToRoleAsync(user, "User");

                        // Redirect the admin to a relevant page (e.g., customer list)
                        return RedirectToAction("Index", "Customer");
                    }
                    else
                    {
                        // Add errors to the model state to display in the view
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
            }
            return RedirectToAction("Index", "Home");
            //return View(model);

            }
        
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Email and Password are required.");
                return View(model); // Return the same view with an error message
            }   

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Customer");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { "Admin", "user" };


            foreach (var roleName in roleNames)
            {
                var roleExist =   await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                   var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"Error creating role {roleName}: {error.Description}");
                        }
                    }
                }
            }
            var adminUser = await userManager.FindByEmailAsync("admin@Cyrus.com");
            if(adminUser == null)
            {
                adminUser = new ApplicationUser()
                {
                    UserName = "Admin",
                    Email = "admin@Cyrus.com",

                };
                var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
                var userResult = await userManager.CreateAsync(new ApplicationUser { UserName = "Admin", Email= "admin@Cyrus.com" }, "Cyrus@2024");
                if (userResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    // Handle failure if needed
                    foreach (var error in userResult.Errors)
                    {
                        // Log or display the error
                        Console.WriteLine($"Error creating user {adminUser.UserName}: {error.Description}");
                    }
                }
            }

        }



    }
}
