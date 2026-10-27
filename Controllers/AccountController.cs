using Cloud_Kitchen_web_App.Api.DTOs.Requests;
using Cloud_Kitchen_web_App.Api.Services;
using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Data;
using CloudKitchen.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CloudKitchen.Controllers
{
    public class AccountController : Controller
    {
        // ============================================================
        // DEPENDENCIES
        // ============================================================

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly CloudKitchenDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            CloudKitchenDbContext context,
            IEmailSender emailSender,
            IGoogleAuthService googleAuthService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
            _googleAuthService = googleAuthService;
            _configuration = configuration;
            _logger = logger;
        }


        // ============================================================
        // SIGNUP PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }


        // ============================================================
        // CUSTOMER SIGNUP - GET
        // ============================================================

        [HttpGet]
        public IActionResult CustomerSignup()
        {
            return View();
        }


        // ============================================================
        // CUSTOMER SIGNUP - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerSignup(
            CustomerSignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            // --------------------------------------------------------
            // STRICT GOOGLE EMAIL VALIDATION
            // --------------------------------------------------------
            if (!PhoneValidationHelper.IsValidGoogleEmail(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Only valid Google accounts (@gmail.com) are allowed.");

                return View(model);
            }

            // --------------------------------------------------------
            // PAKISTANI MOBILE NUMBER VALIDATION & NORMALIZATION
            // --------------------------------------------------------
            if (!PhoneValidationHelper.TryNormalizePakistaniPhone(model.PhoneNumber, out string normalizedPhone))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567).");

                return View(model);
            }

            // --------------------------------------------------------
            // CHECK EXISTING USER BY EMAIL
            // --------------------------------------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            // --------------------------------------------------------
            // CHECK EXISTING PHONE NUMBER (DUPLICATE CHECK)
            // --------------------------------------------------------
            var altPhone = "+92" + normalizedPhone.Substring(1);
            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone || u.PhoneNumber == altPhone)
                           || await _context.Customers.AnyAsync(c => c.Phone == normalizedPhone || c.Phone == altPhone);

            if (phoneExists)
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "This phone number is already registered with an existing account.");

                return View(model);
            }


            // --------------------------------------------------------
            // MAKE SURE CUSTOMER ROLE EXISTS
            // --------------------------------------------------------

            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole("Customer"));

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    return View(model);
                }
            }


            // --------------------------------------------------------
            // CREATE REAL IDENTITY USER
            // --------------------------------------------------------

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = normalizedPhone,
                FullName = model.FullName.Trim(),
                City = model.City.Trim(),

                UserRole = "Customer",
                SellerStatus = "Approved"
            };


            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            // --------------------------------------------------------
            // ADD CUSTOMER ROLE
            // --------------------------------------------------------

            var customerRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Customer");


            if (!customerRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in customerRoleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            // --------------------------------------------------------
            // CREATE CUSTOMER DATABASE RECORD
            // --------------------------------------------------------

            var customer = new CustomerModel
            {
                Name = model.FullName.Trim(),

                Email = email,

                Phone = normalizedPhone,

                Address = string.Empty,

                ProfilePictureUrl =
                    "/images/profiles/default-avatar.png"
            };


            _context.Customers.Add(customer);


            try
            {
                var customerSaveResult =
                    await _context.SaveChangesAsync();

                if (customerSaveResult <= 0)
                {
                    await _userManager.DeleteAsync(user);

                    ModelState.AddModelError(
                        "",
                        "Customer account could not be created. Please try again.");

                    return View(model);
                }
            }
            catch
            {
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    "",
                    "Customer account could not be created. Please try again.");

                return View(model);
            }


            // --------------------------------------------------------
            // AUTOMATIC LOGIN
            // --------------------------------------------------------

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);


            TempData["Success"] =
                "Welcome to Cloud Kitchen! Your account has been created successfully.";


            return RedirectToAction(
                "Index",
                "Customer");
        }


        // ============================================================
        // SELLER SIGNUP - GET
        // ============================================================

        [HttpGet]
        public IActionResult SellerSignup()
        {
            return View();
        }


        // ============================================================
        // SELLER SIGNUP - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellerSignup(
            SellerSignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            // --------------------------------------------------------
            // STRICT GOOGLE EMAIL VALIDATION
            // --------------------------------------------------------
            if (!PhoneValidationHelper.IsValidGoogleEmail(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Only valid Google accounts (@gmail.com) are allowed.");

                return View(model);
            }

            // --------------------------------------------------------
            // PAKISTANI MOBILE NUMBER VALIDATION & NORMALIZATION
            // --------------------------------------------------------
            if (!PhoneValidationHelper.TryNormalizePakistaniPhone(model.PhoneNumber, out string normalizedPhone))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567).");

                return View(model);
            }

            // --------------------------------------------------------
            // CHECK EXISTING USER BY EMAIL
            // --------------------------------------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            // --------------------------------------------------------
            // CHECK EXISTING PHONE NUMBER (DUPLICATE CHECK)
            // --------------------------------------------------------
            var altPhone = "+92" + normalizedPhone.Substring(1);
            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone || u.PhoneNumber == altPhone)
                           || await _context.Customers.AnyAsync(c => c.Phone == normalizedPhone || c.Phone == altPhone);

            if (phoneExists)
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "This phone number is already registered with an existing account.");

                return View(model);
            }


            // --------------------------------------------------------
            // MAKE SURE SELLER ROLE EXISTS
            // --------------------------------------------------------

            if (!await _roleManager.RoleExistsAsync("Seller"))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole("Seller"));

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    return View(model);
                }
            }


            // --------------------------------------------------------
            // CREATE REAL SELLER IDENTITY USER
            // --------------------------------------------------------

            var seller = new ApplicationUser
            {
                UserName = email,

                Email = email,

                PhoneNumber = normalizedPhone,

                FullName =
                    model.FullName.Trim(),

                City = string.Empty,

                UserRole = "Seller",

                SellerStatus = "Pending"
            };


            var sellerResult =
                await _userManager.CreateAsync(
                    seller,
                    model.Password);


            if (!sellerResult.Succeeded)
            {
                foreach (var error in sellerResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            // --------------------------------------------------------
            // ADD SELLER ROLE
            // --------------------------------------------------------

            var sellerRoleResult =
                await _userManager.AddToRoleAsync(
                    seller,
                    "Seller");


            if (!sellerRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(seller);

                foreach (var error in sellerRoleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            // --------------------------------------------------------
            // LOGIN SELLER
            // --------------------------------------------------------

            await _signInManager.SignInAsync(
                seller,
                isPersistent: false);


            TempData["Success"] =
                "Seller account created successfully. Please complete your kitchen profile.";


            return RedirectToAction(
                nameof(KitchenInformation));
        }


        // ============================================================
        // KITCHEN INFORMATION - GET
        // ============================================================

        [HttpGet]
        public IActionResult KitchenInformation()
        {
            return View();
        }


        // ============================================================
        // SELLER KITCHEN PROFILE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> SellerKitchenProfile(
            string kitchenName,
            string kitchenPhone,
            string description,
            string[] cuisine,
            string city,
            string area,
            string address,
            string openingTime,
            string closingTime,
            decimal deliveryRatePerKm,
            string deliveryAreas,
            IFormFile? kitchenImage,
            bool agreement,
            double lat = 0,
            double lng = 0)
        {
            // --------------------------------------------------------
            // GET CURRENT SELLER
            // --------------------------------------------------------

            var seller =
                await _userManager.GetUserAsync(User);

            if (seller == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }


            // --------------------------------------------------------
            // AGREEMENT VALIDATION
            // --------------------------------------------------------

            if (!agreement)
            {
                ModelState.AddModelError(
                    "",
                    "You must agree to the Seller Agreement.");
            }


            // --------------------------------------------------------
            // KITCHEN NAME
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(kitchenName))
            {
                ModelState.AddModelError(
                    "kitchenName",
                    "Kitchen name is required.");
            }


            // --------------------------------------------------------
            // CITY
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(city))
            {
                ModelState.AddModelError(
                    "city",
                    "City is required.");
            }


            // --------------------------------------------------------
            // ADDRESS
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(address))
            {
                ModelState.AddModelError(
                    "address",
                    "Kitchen address is required.");
            }


            // --------------------------------------------------------
            // DELIVERY RATE
            // --------------------------------------------------------

            if (deliveryRatePerKm < 0)
            {
                ModelState.AddModelError(
                    "deliveryRatePerKm",
                    "Delivery rate cannot be negative.");
            }


            // --------------------------------------------------------
            // DUPLICATE KITCHEN NAME
            // --------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(kitchenName))
            {
                var normalizedKitchenName =
                    kitchenName.Trim().ToLower();

                var kitchenExists =
                    await _context.Kitchens
                        .AnyAsync(k =>
                            k.Name.ToLower() ==
                            normalizedKitchenName);

                if (kitchenExists)
                {
                    ModelState.AddModelError(
                        "kitchenName",
                        "This kitchen name is already registered.");
                }
            }


            // --------------------------------------------------------
            // IMAGE
            // --------------------------------------------------------

            string imagePath =
                "/images/kitchens/default-kitchen.jpg";


            if (kitchenImage != null &&
                kitchenImage.Length > 0)
            {
                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                var extension =
                    Path.GetExtension(
                        kitchenImage.FileName)
                    .ToLowerInvariant();


                if (!allowedExtensions.Contains(
                        extension))
                {
                    ModelState.AddModelError(
                        "kitchenImage",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }


                if (kitchenImage.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "kitchenImage",
                        "Kitchen image must be less than 5 MB.");
                }
            }


            // --------------------------------------------------------
            // VALIDATION FAILED
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View("KitchenInformation");
            }


            // --------------------------------------------------------
            // CHECK EXISTING OWNER/KITCHEN
            // --------------------------------------------------------

            var existingOwner =
                await _context.KitchenOwners
                    .FirstOrDefaultAsync(o =>
                        o.Email.ToLower() ==
                        seller.Email!.ToLower());


            if (existingOwner != null)
            {
                var alreadyHasKitchen =
                    await _context.Kitchens
                        .AnyAsync(k =>
                            k.KitchenOwnerId ==
                            existingOwner.Id);

                if (alreadyHasKitchen)
                {
                    ModelState.AddModelError(
                        "",
                        "You have already submitted a kitchen profile.");

                    return View("KitchenInformation");
                }
            }


            // --------------------------------------------------------
            // CREATE KITCHEN OWNER
            // --------------------------------------------------------

            var kitchenOwner =
                new KitchenOwner
                {
                    Name =
                        seller.FullName,

                    Email =
                        seller.Email ?? string.Empty,

                    Phone =
                        seller.PhoneNumber ?? string.Empty,

                    Address =
                        address.Trim()
                };


            _context.KitchenOwners.Add(
                kitchenOwner);


            await _context.SaveChangesAsync();


            // --------------------------------------------------------
            // SAVE IMAGE
            // --------------------------------------------------------

            if (kitchenImage != null &&
                kitchenImage.Length > 0)
            {
                var extension =
                    Path.GetExtension(
                        kitchenImage.FileName)
                    .ToLowerInvariant();


                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "kitchens");


                if (!Directory.Exists(
                        uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }


                var fileName =
                    Guid.NewGuid().ToString("N")
                    + extension;


                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName);


                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await kitchenImage.CopyToAsync(
                        stream);
                }


                imagePath =
                    "/images/kitchens/"
                    + fileName;
            }


            // --------------------------------------------------------
            // CUISINE
            // --------------------------------------------------------

            var cuisineText =
                cuisine != null &&
                cuisine.Length > 0
                    ? string.Join(", ", cuisine)
                    : string.Empty;


            // --------------------------------------------------------
            // CREATE KITCHEN
            // --------------------------------------------------------

            var kitchen =
                new Kitchen
                {
                    Name =
                        kitchenName.Trim(),

                    Phone =
                        kitchenPhone?.Trim()
                        ?? string.Empty,

                    Description =
                        description?.Trim()
                        ?? string.Empty,

                    Cuisine =
                        cuisineText,

                    City =
                        city?.Trim()
                        ?? string.Empty,

                    Area =
                        area?.Trim()
                        ?? string.Empty,

                    Address =
                        address?.Trim()
                        ?? string.Empty,

                    OpeningTime =
                        openingTime?.Trim()
                        ?? string.Empty,

                    ClosingTime =
                        closingTime?.Trim()
                        ?? string.Empty,

                    DeliveryRatePerKm =
                        deliveryRatePerKm,

                    DeliveryAreas =
                        deliveryAreas?.Trim()
                        ?? string.Empty,

                    Image =
                        imagePath,

                    Rating = 0,

                    DeliveryTime =
                        string.Empty,

                    MinOrder = 0,

                    IsOpen = false,

                    ApprovalStatus =
                        "Pending",

                    RejectionReason =
                        string.Empty,

                    KitchenOwnerId =
                        kitchenOwner.Id
                };


            _context.Kitchens.Add(
                kitchen);

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // NOTIFY ADMIN ABOUT NEW KITCHEN APPROVAL REQUEST
            // --------------------------------------------------------

            var adminNotif = new Notification
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "New Kitchen Approval Request",
                Body = $"Kitchen '{kitchen.Name}' submitted by {seller.FullName} ({seller.Email}) is waiting for admin approval.",
                Time = DateTime.UtcNow,
                Status = "Unread",
                Link = "/Admin/ManageKitchens",
                RecipientType = "Admin",
                RecipientId = 0
            };

            _context.Notifications.Add(adminNotif);
            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // SELLER REMAINS PENDING
            // --------------------------------------------------------

            seller.SellerStatus =
                "Pending";

            await _userManager.UpdateAsync(
                seller);


            // --------------------------------------------------------
            // SUCCESS
            // --------------------------------------------------------

            TempData["Success"] =
                "Your kitchen profile has been submitted successfully. Your account is waiting for admin approval.";


            await _signInManager.SignOutAsync();


            return RedirectToAction(
                nameof(Login));
        }


        // ============================================================
        // LOGIN - GET
        // ============================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // ============================================================
        // LOGIN - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!PhoneValidationHelper.IsValidGoogleEmail(model.Email))
            {
                ModelState.AddModelError("Email", "Only valid Google accounts (@gmail.com) are allowed.");
                return View(model);
            }


            var user =
                await _userManager.FindByEmailAsync(
                    model.Email.Trim());


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }


            // --------------------------------------------------------
            // SELLER APPROVAL
            // --------------------------------------------------------

            var isSeller =
                await _userManager.IsInRoleAsync(
                    user,
                    "Seller");


            if (isSeller &&
                !string.Equals(
                    user.SellerStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "",
                    "Your seller account is waiting for admin approval.");

                return View(model);
            }


            // --------------------------------------------------------
            // REAL IDENTITY LOGIN
            // --------------------------------------------------------

            var loginResult =
                await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);


            if (loginResult.Succeeded)
            {
                // ----------------------------------------------------
                // ADMIN
                // ----------------------------------------------------

                if (await _userManager.IsInRoleAsync(
                        user,
                        "Admin"))
                {
                    return RedirectToAction(
                        "Dashboard",
                        "Admin");
                }


                // ----------------------------------------------------
                // SELLER
                // ----------------------------------------------------

                if (await _userManager.IsInRoleAsync(
                        user,
                        "Seller"))
                {
                    return RedirectToAction(
                        "SellerDashboard",
                        "Seller");
                }


                // ----------------------------------------------------
                // CUSTOMER
                // ----------------------------------------------------

                if (await _userManager.IsInRoleAsync(
                        user,
                        "Customer"))
                {
                    return RedirectToAction(
                        "Index",
                        "Customer");
                }


                // ----------------------------------------------------
                // INVALID ROLE
                // ----------------------------------------------------

                await _signInManager.SignOutAsync();

                ModelState.AddModelError(
                    "",
                    "Your account does not have a valid role.");

                return View(model);
            }


            // --------------------------------------------------------
            // LOCKED OUT
            // --------------------------------------------------------

            if (loginResult.IsLockedOut)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been temporarily locked. Please try again later.");

                return View(model);
            }


            // --------------------------------------------------------
            // LOGIN FAILED
            // --------------------------------------------------------

            ModelState.AddModelError(
                "",
                "Invalid email or password.");

            return View(model);
        }


        // ============================================================
        // GOOGLE LOGIN - POST (AJAX / COOKIE AUTH)
        // ============================================================

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleSignInRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
            {
                return Json(new { success = false, message = "A valid Google ID token is required." });
            }

            GoogleUserInfo googleUser;
            try
            {
                googleUser = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken.Trim());
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token during web login.");
                return Json(new { success = false, message = $"Invalid or expired Google ID token: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Google token during web login.");
                return Json(new { success = false, message = $"Failed to authenticate with Google: {ex.Message}" });
            }

            if (string.IsNullOrWhiteSpace(googleUser.Email))
            {
                return Json(new { success = false, message = "Google account does not contain a valid email address." });
            }

            if (!googleUser.EmailVerified)
            {
                return Json(new { success = false, message = "Google account email is not verified." });
            }

            // 1. Check if user exists by Google login or email
            var user = await _userManager.FindByLoginAsync("Google", googleUser.GoogleId);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(googleUser.Email);
                if (user != null)
                {
                    var logins = await _userManager.GetLoginsAsync(user);
                    if (!logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == googleUser.GoogleId))
                    {
                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleUser.GoogleId, "Google"));
                    }
                }
            }

            // 2. If user does not exist, create new Customer user
            if (user == null)
            {
                var userName = googleUser.Email;
                var fullName = !string.IsNullOrWhiteSpace(googleUser.Name)
                    ? googleUser.Name.Trim()
                    : googleUser.Email.Split('@')[0];

                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = googleUser.Email,
                    FullName = fullName,
                    EmailConfirmed = googleUser.EmailVerified,
                    UserRole = "Customer",
                    SellerStatus = "NotApplicable",
                    City = string.Empty,
                    PhoneNumber = string.Empty
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed to create user account: {errors}" });
                }

                await _userManager.AddToRoleAsync(user, "Customer");
                await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleUser.GoogleId, "Google"));

                // Create Customer record
                var customerModel = new CustomerModel
                {
                    Name = user.FullName,
                    Email = user.Email,
                    Phone = string.Empty,
                    Address = string.Empty,
                    ProfilePictureUrl = !string.IsNullOrWhiteSpace(googleUser.Picture)
                        ? googleUser.Picture
                        : "/images/logo.png"
                };

                _context.Customers.Add(customerModel);
                await _context.SaveChangesAsync();

                // Create CustomerSettings
                var customerSettings = new CustomerSettings
                {
                    CustomerId = customerModel.Id,
                    Language = "English",
                    OrderNotifications = true,
                    EmailNotifications = true
                };

                _context.CustomerSettings.Add(customerSettings);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Sync profile picture if customer record exists
                var rolesList = await _userManager.GetRolesAsync(user);
                if (rolesList.Contains("Customer") || user.UserRole == "Customer")
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
                    if (customer == null)
                    {
                        customer = new CustomerModel
                        {
                            Name = user.FullName,
                            Email = user.Email ?? googleUser.Email,
                            Phone = user.PhoneNumber ?? string.Empty,
                            Address = string.Empty,
                            ProfilePictureUrl = !string.IsNullOrWhiteSpace(googleUser.Picture)
                                ? googleUser.Picture
                                : "/images/logo.png"
                        };
                        _context.Customers.Add(customer);
                        await _context.SaveChangesAsync();

                        var customerSettings = new CustomerSettings
                        {
                            CustomerId = customer.Id,
                            Language = "English",
                            OrderNotifications = true,
                            EmailNotifications = true
                        };

                        _context.CustomerSettings.Add(customerSettings);
                        await _context.SaveChangesAsync();
                    }
                    else if (!string.IsNullOrWhiteSpace(googleUser.Picture) && (string.IsNullOrEmpty(customer.ProfilePictureUrl) || customer.ProfilePictureUrl == "/images/logo.png"))
                    {
                        customer.ProfilePictureUrl = googleUser.Picture;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // 3. Check Seller approval status
            var isSeller = await _userManager.IsInRoleAsync(user, "Seller");
            if (isSeller && !string.Equals(user.SellerStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Your seller account is waiting for admin approval." });
            }

            // 4. Sign in user to Identity Cookie session
            await _signInManager.SignInAsync(user, isPersistent: true);

            // 5. Determine redirect URL
            string redirectUrl = Url.Action("Index", "Customer") ?? "/Customer/Index";
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                redirectUrl = Url.Action("Dashboard", "Admin") ?? "/Admin/Dashboard";
            }
            else if (isSeller)
            {
                redirectUrl = Url.Action("SellerDashboard", "Seller") ?? "/Seller/SellerDashboard";
            }

            return Json(new { success = true, redirectUrl = redirectUrl });
        }


        // ============================================================
        // FORGOT PASSWORD - GET
        // ============================================================

        [HttpGet]
        public IActionResult Forgot()
        {
            return View();
        }


        // ============================================================
        // FORGOT PASSWORD - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forgot(
            string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Please enter your email address.");

                return View();
            }


            email = email.Trim();


            // --------------------------------------------------------
            // FIND REAL USER FROM IDENTITY DATABASE
            // --------------------------------------------------------

            var user =
                await _userManager.FindByEmailAsync(
                    email);


            // --------------------------------------------------------
            // SECURITY:
            // DON'T REVEAL WHETHER ACCOUNT EXISTS
            // --------------------------------------------------------

            if (user == null)
            {
                _logger.LogWarning("Forgot password requested for email '{Email}' but no such user was found.", email);
                TempData["Success"] =
                    "If an account exists with this email, password reset instructions will be sent.";

                return RedirectToAction(
                    nameof(ForgotConfirmation));
            }
            
            _logger.LogInformation("Forgot password requested for email '{Email}', user found.", email);


            // --------------------------------------------------------
            // GENERATE REAL IDENTITY RESET TOKEN
            // --------------------------------------------------------

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(
                    user);


            // --------------------------------------------------------
            // CREATE REAL RESET URL
            // --------------------------------------------------------

            var resetLink =
                Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new
                    {
                        email = user.Email,
                        token = token
                    },
                    Request.Scheme);


            if (string.IsNullOrWhiteSpace(resetLink))
            {
                _logger.LogError("Password reset link could not be created for user '{Email}'.", email);
                ModelState.AddModelError(
                    "",
                    "Password reset link could not be created. Please try again.");

                return View();
            }

            _logger.LogInformation("Reset link generated for '{Email}'.", email);


            // --------------------------------------------------------
            // ENCODE VALUES FOR HTML EMAIL
            // --------------------------------------------------------

            var safeResetLink =
                WebUtility.HtmlEncode(resetLink);

            var safeName =
                WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(user.FullName)
                        ? "Cloud Kitchen User"
                        : user.FullName);


            // --------------------------------------------------------
            // REAL HTML EMAIL
            // --------------------------------------------------------

            var htmlMessage = $@"
<!DOCTYPE html>

<html>
<head>
    <meta charset=""UTF-8"">
    <title>Cloud Kitchen Password Reset</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background:#f5f5f5;
    font-family:Arial,Helvetica,sans-serif;
"">

    <div style=""
        max-width:600px;
        margin:40px auto;
        background:#ffffff;
        border-radius:10px;
        padding:35px;
        box-sizing:border-box;
    "">

        <h2 style=""
            margin-top:0;
            color:#5D4037;
        "">
            Cloud Kitchen
        </h2>

        <p style=""
            color:#333333;
            font-size:16px;
        "">
            Hello {safeName},
        </p>

        <p style=""
            color:#333333;
            font-size:15px;
            line-height:1.6;
        "">
            We received a request to reset the password
            for your Cloud Kitchen account.
        </p>

        <p style=""
            color:#333333;
            font-size:15px;
            line-height:1.6;
        "">
            Click the button below to create a new password.
        </p>

        <div style=""
            text-align:center;
            margin:30px 0;
        "">

            <a href=""{safeResetLink}""
               style=""
                    display:inline-block;
                    background:#5D4037;
                    color:#ffffff;
                    text-decoration:none;
                    padding:14px 28px;
                    border-radius:6px;
                    font-size:16px;
                    font-weight:bold;
               "">
                Reset Password
            </a>

        </div>

        <p style=""
            color:#666666;
            font-size:13px;
            line-height:1.5;
        "">
            If you did not request a password reset,
            you can safely ignore this email.
        </p>

        <p style=""
            color:#999999;
            font-size:12px;
            margin-top:30px;
        "">
            Cloud Kitchen
        </p>

    </div>

</body>
</html>
";


            // Store reset link for testing / direct access
            TempData["DevResetLink"] = resetLink;

            // --------------------------------------------------------
            // SEND REAL EMAIL THROUGH SMTP
            // --------------------------------------------------------

            try
            {
                _logger.LogInformation("Attempting to send password reset email to '{Email}'...", user.Email);
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Cloud Kitchen - Reset Your Password",
                    htmlMessage);
                _logger.LogInformation("Successfully sent password reset email to '{Email}'.", user.Email);
            }
            catch (Exception ex)
            {
                var fullExceptionStr = ex.ToString();
                _logger.LogError(ex, "Failed to send password reset email to '{Email}'. Full Exception: {ExceptionDetail}", user.Email, fullExceptionStr);
                
                _logger.LogWarning("Email delivery failed for '{Email}'. Proceeding to ForgotConfirmation with reset link available for developer.", user.Email);
            }


            // --------------------------------------------------------
            // SUCCESS
            // --------------------------------------------------------

            TempData["Success"] =
                "Password reset instructions have been sent to your email address.";


            return RedirectToAction(
                nameof(ForgotConfirmation));
        }


        // ============================================================
        // FORGOT PASSWORD CONFIRMATION
        // ============================================================

        [HttpGet]
        public IActionResult ForgotConfirmation()
        {
            return View();
        }


        // ============================================================
        // RESET PASSWORD - GET
        // ============================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string email,
            string token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(
                    nameof(Forgot));
            }


            var model =
                new ResetPasswordViewModel
                {
                    Email = email,
                    Token = token
                };


            return View(model);
        }


        // ============================================================
        // RESET PASSWORD - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            // --------------------------------------------------------
            // MODEL VALIDATION
            // Includes:
            // Password required
            // ConfirmPassword required
            // Passwords MUST MATCH
            // --------------------------------------------------------

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // --------------------------------------------------------
            // FIND REAL USER FROM DATABASE
            // --------------------------------------------------------

            var user =
                await _userManager.FindByEmailAsync(
                    model.Email.Trim());


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid password reset request.");

                return View(model);
            }


            // --------------------------------------------------------
            // REAL IDENTITY PASSWORD RESET
            // --------------------------------------------------------

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);


            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Your password has been reset successfully. You can now login.";

                return RedirectToAction(
                    nameof(Login));
            }


            // --------------------------------------------------------
            // IDENTITY ERRORS
            // --------------------------------------------------------

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }


            return View(model);
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }


        // ============================================================
        // ACCESS DENIED
        // ============================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // ============================================================
        // CUSTOMER SIGNUP VIEW MODEL
        // ============================================================

        public class CustomerSignupViewModel
        {
            [Required(
                ErrorMessage =
                "Full name is required.")]
            [MaxLength(100)]
            public string FullName { get; set; }
                = string.Empty;


            [Required(ErrorMessage = "Google Email address (@gmail.com) is required.")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|googlemail\.com)$", ErrorMessage = "Only valid Google accounts (@gmail.com) are allowed.")]
            public string Email { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Phone number is required.")]
            [RegularExpression(@"^(\+92|92|0)?3[0-5][0-9]{8}$", ErrorMessage = "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567).")]
            [MaxLength(20)]
            public string PhoneNumber { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Please confirm your password.")]
            [DataType(DataType.Password)]
            [Compare(
                "Password",
                ErrorMessage =
                "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Please select your city.")]
            public string City { get; set; }
                = string.Empty;
        }


        // ============================================================
        // SELLER SIGNUP VIEW MODEL
        // ============================================================

        public class SellerSignupViewModel
        {
            [Required(
                ErrorMessage =
                "Full name is required.")]
            [MaxLength(100)]
            public string FullName { get; set; }
                = string.Empty;


            [Required(ErrorMessage = "Google Email address (@gmail.com) is required.")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|googlemail\.com)$", ErrorMessage = "Only valid Google accounts (@gmail.com) are allowed.")]
            public string Email { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Phone number is required.")]
            [RegularExpression(@"^(\+92|92|0)?3[0-5][0-9]{8}$", ErrorMessage = "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567).")]
            [MaxLength(20)]
            public string PhoneNumber { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Please confirm your password.")]
            [DataType(DataType.Password)]
            [Compare(
                "Password",
                ErrorMessage =
                "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
                = string.Empty;
        }


        // ============================================================
        // LOGIN VIEW MODEL
        // ============================================================

        public class LoginViewModel
        {
            [Required(ErrorMessage = "Google Email address is required.")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|googlemail\.com)$", ErrorMessage = "Only valid Google accounts (@gmail.com) are allowed.")]
            public string Email { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
                = string.Empty;


            public bool RememberMe { get; set; }
        }


        // ============================================================
        // RESET PASSWORD VIEW MODEL
        // ============================================================

        public class ResetPasswordViewModel
        {
            [Required(ErrorMessage = "Email address is required.")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address (e.g., name@example.com).")]
            public string Email { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
                = string.Empty;


            [Required(
                ErrorMessage =
                "Please confirm your password.")]
            [DataType(DataType.Password)]
            [Compare(
                "Password",
                ErrorMessage =
                "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
                = string.Empty;


            [Required]
            public string Token { get; set; }
                = string.Empty;
        }
    }
}