using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Cloud_Kitchen_web_App.Api.DTOs.Requests;
using Cloud_Kitchen_web_App.Api.DTOs.Responses;
using Cloud_Kitchen_web_App.Api.Services;
using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Models;
using CloudKitchen.Data;
using CloudKitchen.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Cloud_Kitchen_web_App.Api.Controllers
{
    [Route("api/[controller]")]
    [Route("api/auth")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly CloudKitchenDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly ILogger<AuthApiController> _logger;
        private readonly IHostEnvironment _env;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            CloudKitchenDbContext context,
            IEmailSender emailSender,
            IGoogleAuthService googleAuthService,
            ILogger<AuthApiController> logger,
            IHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _context = context;
            _emailSender = emailSender;
            _googleAuthService = googleAuthService;
            _logger = logger;
            _env = env;
        }

        // ============================================================
        // GOOGLE SIGN-IN API
        // ============================================================
        [HttpPost("google")]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "A valid Google ID token is required."
                });
            }

            GoogleUserInfo googleUser;
            try
            {
                googleUser = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken.Trim());
            }
            catch (InvalidJwtException ex)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = $"Invalid or expired Google ID token: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = $"Invalid or expired Google token: {ex.Message}"
                });
            }

            if (string.IsNullOrWhiteSpace(googleUser.Email))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Google account does not contain a valid email address."
                });
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

            // 2. If user does not exist, create new user
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
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = $"Failed to create user account: {errors}"
                    });
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
                // Sync profile picture and ensure customer record exists if role is Customer
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

            // 3. Generate JWT access and refresh tokens
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshTokenHash = _jwtService.HashRefreshToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? user.UserRole;

            string? sellerStatus = null;
            int? kitchenId = null;
            if (role == "Seller")
            {
                var kitchen = await _context.Kitchens.Include(k => k.KitchenOwner).FirstOrDefaultAsync(k => k.KitchenOwner.Email == user.Email);
                sellerStatus = kitchen?.ApprovalStatus ?? user.SellerStatus;
                kitchenId = kitchen?.Id;

                if (!string.IsNullOrEmpty(sellerStatus) && !string.Equals(user.SellerStatus, sellerStatus, StringComparison.OrdinalIgnoreCase))
                {
                    user.SellerStatus = sellerStatus;
                    await _userManager.UpdateAsync(user);
                }

                if (!string.Equals(sellerStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(sellerStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        var reason = !string.IsNullOrWhiteSpace(kitchen?.RejectionReason)
                            ? $" Rejection reason: {kitchen.RejectionReason}"
                            : string.Empty;
                        return StatusCode(403, new AuthResponse
                        {
                            Success = false,
                            Message = $"Your seller account has been rejected by the administrator.{reason}",
                            Role = role,
                            SellerStatus = "Rejected"
                        });
                    }

                    return StatusCode(403, new AuthResponse
                    {
                        Success = false,
                        Message = "Your seller account is waiting for admin approval. Please wait for an administrator to approve your kitchen on the web portal.",
                        Role = role,
                        SellerStatus = "Pending"
                    });
                }
            }

            int? customerId = null;
            if (role == "Customer")
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
                customerId = customer?.Id;
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? googleUser.Email,
                FullName = user.FullName,
                Name = user.FullName,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Role = role,
                SellerStatus = sellerStatus,
                GoogleId = googleUser.GoogleId,
                Picture = googleUser.Picture,
                KitchenId = kitchenId,
                CustomerId = customerId
            };

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Google Sign-In successful.",
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = role,
                SellerStatus = sellerStatus,
                User = userDto
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            request.Email = request.Email?.Trim() ?? string.Empty;

            if (!PhoneValidationHelper.IsValidGoogleEmail(request.Email))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Only valid Google accounts (@gmail.com) are allowed." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid email or password." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return StatusCode(403, new AuthResponse { Success = false, Message = "Account is locked out due to multiple failed attempts." });

            if (!result.Succeeded)
                return Unauthorized(new AuthResponse { Success = false, Message = "Invalid email or password." });

            var accessToken = await _jwtService.GenerateAccessTokenAsync(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshTokenHash = _jwtService.HashRefreshToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days validity

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? user.UserRole;

            string? sellerStatus = null;
            if (role == "Seller")
            {
                var kitchen = await _context.Kitchens.Include(k => k.KitchenOwner).FirstOrDefaultAsync(k => k.KitchenOwner.Email == user.Email);
                if (kitchen != null)
                {
                    sellerStatus = kitchen.ApprovalStatus;
                }
                else
                {
                    sellerStatus = user.SellerStatus;
                }

                if (!string.IsNullOrEmpty(sellerStatus) && !string.Equals(user.SellerStatus, sellerStatus, StringComparison.OrdinalIgnoreCase))
                {
                    user.SellerStatus = sellerStatus;
                    await _userManager.UpdateAsync(user);
                }

                if (!string.Equals(sellerStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(sellerStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        var reason = !string.IsNullOrWhiteSpace(kitchen?.RejectionReason)
                            ? $" Rejection reason: {kitchen.RejectionReason}"
                            : string.Empty;
                        return StatusCode(403, new AuthResponse
                        {
                            Success = false,
                            Message = $"Your seller account has been rejected by the administrator.{reason}",
                            Role = role,
                            SellerStatus = "Rejected"
                        });
                    }

                    return StatusCode(403, new AuthResponse
                    {
                        Success = false,
                        Message = "Your seller account is waiting for admin approval. Please wait for an administrator to approve your kitchen on the web portal.",
                        Role = role,
                        SellerStatus = "Pending"
                    });
                }
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Login successful.",
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = role,
                SellerStatus = sellerStatus
            });
        }

        [HttpGet("seller-status")]
        public async Task<IActionResult> CheckSellerStatus([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required." });

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
                return NotFound(new { success = false, message = "No account found with this email." });

            var isSeller = await _userManager.IsInRoleAsync(user, "Seller") || user.UserRole == "Seller";
            if (!isSeller)
                return BadRequest(new { success = false, message = "This account is not a seller account." });

            var kitchen = await _context.Kitchens.Include(k => k.KitchenOwner).FirstOrDefaultAsync(k => k.KitchenOwner.Email == user.Email);
            var status = kitchen?.ApprovalStatus ?? user.SellerStatus ?? "Pending";

            return Ok(new
            {
                success = true,
                email = user.Email,
                kitchenName = kitchen?.Name ?? string.Empty,
                status = status,
                isApproved = string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase),
                isPending = string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase),
                isRejected = string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase),
                rejectionReason = kitchen?.RejectionReason ?? string.Empty
            });
        }

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            request.Email = request.Email?.Trim() ?? string.Empty;

            if (!PhoneValidationHelper.IsValidGoogleEmail(request.Email))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Only valid Google accounts (@gmail.com) are allowed." });
            }

            if (!PhoneValidationHelper.TryNormalizePakistaniPhone(request.Phone, out string normalizedPhone))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567)." });
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new AuthResponse { Success = false, Message = "Email is already registered." });

            var altPhone = "+92" + normalizedPhone.Substring(1);
            var phoneInUse = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone || u.PhoneNumber == altPhone)
                          || await _context.Customers.AnyAsync(c => c.Phone == normalizedPhone || c.Phone == altPhone);
            if (phoneInUse)
            {
                return BadRequest(new AuthResponse { Success = false, Message = "This phone number is already registered with an existing account." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = normalizedPhone,
                City = request.City,
                UserRole = "Customer",
                SellerStatus = "NotApplicable"
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new AuthResponse { Success = false, Message = $"Registration failed: {errors}" });
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            // Create related CustomerModel
            var customerModel = new CustomerModel
            {
                Name = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address
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

            return Ok(new AuthResponse { Success = true, Message = "Customer registered successfully." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (request == null)
                return BadRequest("Invalid client request");

            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return BadRequest("Invalid access token or refresh token");

            string email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || user.RefreshTokenHash == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid access token or refresh token");

            var providedHash = _jwtService.HashRefreshToken(request.RefreshToken);
            if (user.RefreshTokenHash != providedHash)
                return BadRequest("Invalid access token or refresh token");

            var newAccessToken = await _jwtService.GenerateAccessTokenAsync(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshTokenHash = _jwtService.HashRefreshToken(newRefreshToken);
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? user.UserRole;

            string? sellerStatus = null;
            if (role == "Seller")
            {
                var kitchen = await _context.Kitchens.Include(k => k.KitchenOwner).FirstOrDefaultAsync(k => k.KitchenOwner.Email == user.Email);
                if (kitchen != null)
                {
                    sellerStatus = kitchen.ApprovalStatus;
                }
                else
                {
                    sellerStatus = user.SellerStatus;
                }
            }

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Token = newAccessToken,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Role = role,
                SellerStatus = sellerStatus
            });
        }

        [HttpPost("register-seller")]
        public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.Email = request.Email?.Trim() ?? string.Empty;

            if (!PhoneValidationHelper.IsValidGoogleEmail(request.Email))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Only valid Google accounts (@gmail.com) are allowed." });
            }

            if (!PhoneValidationHelper.TryNormalizePakistaniPhone(request.PhoneNumber, out string normalizedPhone))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567)." });
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new AuthResponse { Success = false, Message = "Email is already registered." });

            var altPhone = "+92" + normalizedPhone.Substring(1);
            var phoneInUse = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone || u.PhoneNumber == altPhone)
                          || await _context.Customers.AnyAsync(c => c.Phone == normalizedPhone || c.Phone == altPhone);
            if (phoneInUse)
            {
                return BadRequest(new AuthResponse { Success = false, Message = "This phone number is already registered with an existing account." });
            }

            var normalizedKitchenName = request.KitchenName.Trim().ToLower();
            var kitchenExists = await _context.Kitchens.AnyAsync(k => k.Name.ToLower() == normalizedKitchenName);
            if (kitchenExists)
                return BadRequest(new AuthResponse { Success = false, Message = "This kitchen name is already registered." });

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName.Trim(),
                PhoneNumber = normalizedPhone,
                City = request.City.Trim(),
                UserRole = "Seller",
                SellerStatus = "Pending"
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new AuthResponse { Success = false, Message = $"Registration failed: {errors}" });
            }

            await _userManager.AddToRoleAsync(user, "Seller");

            var kitchenOwner = new KitchenOwner
            {
                Name = request.FullName.Trim(),
                Email = request.Email,
                Phone = request.PhoneNumber.Trim(),
                Address = request.Address.Trim()
            };

            _context.KitchenOwners.Add(kitchenOwner);
            await _context.SaveChangesAsync();

            string imagePath = "/images/kitchens/default-kitchen.jpg";
            if (!string.IsNullOrEmpty(request.ImageBase64))
            {
                try
                {
                    string b64 = request.ImageBase64.Contains(",") ? request.ImageBase64.Split(',')[1] : request.ImageBase64;
                    var bytes = Convert.FromBase64String(b64);
                    var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "images", "kitchens");
                    if (!System.IO.Directory.Exists(uploadsFolder)) System.IO.Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString("N") + ".jpg";
                    var filePath = System.IO.Path.Combine(uploadsFolder, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    imagePath = "/images/kitchens/" + fileName;
                }
                catch { }
            }

            var kitchen = new Kitchen
            {
                Name = request.KitchenName.Trim(),
                Phone = request.KitchenPhone?.Trim() ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty,
                Cuisine = request.Cuisine?.Trim() ?? string.Empty,
                City = request.City?.Trim() ?? string.Empty,
                Area = request.Area?.Trim() ?? string.Empty,
                Address = request.Address?.Trim() ?? string.Empty,
                Lat = request.Lat,
                Lng = request.Lng,
                OpeningTime = request.OpeningTime?.Trim() ?? string.Empty,
                ClosingTime = request.ClosingTime?.Trim() ?? string.Empty,
                DeliveryRatePerKm = request.DeliveryRatePerKm,
                DeliveryAreas = request.DeliveryAreas?.Trim() ?? string.Empty,
                Image = imagePath,
                Rating = 0,
                DeliveryTime = string.Empty,
                MinOrder = 0,
                IsOpen = false,
                ApprovalStatus = "Pending",
                RejectionReason = string.Empty,
                KitchenOwnerId = kitchenOwner.Id
            };

            _context.Kitchens.Add(kitchen);
            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = "New Kitchen Approval Request",
                Body = $"Kitchen '{kitchen.Name}' submitted by {user.FullName} ({user.Email}) is waiting for admin approval.",
                Time = DateTime.UtcNow,
                Status = "Unread",
                Link = "/Admin/ManageKitchens",
                RecipientType = "Admin",
                RecipientId = 0
            });
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse { Success = true, Message = "Seller account created successfully. Your account is Pending approval." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = request.Email?.Trim() ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Print token to console ONLY in Development for easy debugging
                if (_env.IsDevelopment())
                {
                    Console.WriteLine($"==================================================");
                    Console.WriteLine($"[DEV DEBUG] Password Reset Token for {email}:");
                    Console.WriteLine(token);
                    Console.WriteLine($"==================================================");
                    _logger.LogInformation("[DEV DEBUG] Password Reset Token for {Email}: {Token}", email, token);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var resetLink = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
                var safeResetLink = WebUtility.HtmlEncode(resetLink);
                var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(user.FullName) ? "Cloud Kitchen User" : user.FullName.Trim());
                var safeToken = WebUtility.HtmlEncode(token);

                var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Cloud Kitchen Password Reset</title>
</head>
<body style=""margin:0; padding:0; background-color:#F5F5F5; font-family:'Segoe UI', Arial, sans-serif;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#F5F5F5; padding:30px 15px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width:600px; background:#FFFFFF; border-radius:12px; overflow:hidden; box-shadow:0 4px 14px rgba(0,0,0,0.08);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background:linear-gradient(135deg, #5D4037 0%, #3E2723 100%); padding:28px; text-align:center;"">
                            <h1 style=""margin:0; color:#F5A623; font-size:26px; font-weight:bold; letter-spacing:1px;"">Cloud Kitchen</h1>
                            <p style=""margin:6px 0 0 0; color:#FFFFFF; font-size:13px; opacity:0.9;"">Password Reset Instructions</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding:32px 30px; color:#333333;"">
                            <p style=""margin-top:0; font-size:16px; font-weight:600; color:#5D4037;"">Hello {safeName},</p>
                            <p style=""font-size:14px; line-height:1.6; color:#555555;"">
                                We received a request to reset your Cloud Kitchen account password. You can reset it easily using either the Web Portal link or the Mobile App code below:
                            </p>
                            
                            <!-- Option 1: Web Reset Button -->
                            <div style=""text-align:center; margin:28px 0 24px 0;"">
                                <a href=""{safeResetLink}"" target=""_blank"" style=""display:inline-block; background-color:#F5A623; color:#FFFFFF; font-size:15px; font-weight:bold; text-decoration:none; padding:14px 32px; border-radius:8px; box-shadow:0 3px 8px rgba(245,166,35,0.4);"">
                                    Reset Password (Web)
                                </a>
                            </div>

                            <!-- Option 2: Mobile App Reset Code -->
                            <div style=""margin:20px 0; padding:16px; background-color:#FFF8E1; border:1px dashed #F5A623; border-radius:8px; text-align:center;"">
                                <div style=""font-size:12px; font-weight:bold; color:#8D6E63; text-transform:uppercase; letter-spacing:0.5px; margin-bottom:4px;"">Mobile App Token</div>
                                <div style=""font-size:13px; color:#5D4037; margin-bottom:8px;"">Mobile app mein ye reset code paste karein:</div>
                                <div style=""font-family:Consolas, 'Courier New', monospace; font-size:14px; font-weight:bold; color:#3E2723; background:#FFFFFF; padding:10px 12px; border-radius:6px; border:1px solid #FFE082; word-break:break-all; user-select:all;"">
                                    {safeToken}
                                </div>
                            </div>

                            <p style=""font-size:13px; line-height:1.5; color:#777777; margin-top:24px;"">
                                If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color:#FAFAFA; border-top:1px solid #EEEEEE; padding:18px; text-align:center; color:#999999; font-size:12px;"">
                            &copy; 2026 Cloud Kitchen &bull; Delicious homemade food delivered to your door
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
";

                try
                {
                    _logger.LogInformation("Sending password reset email to '{Email}' via SMTP...", email);
                    await _emailSender.SendEmailAsync(email, "Reset Your Cloud Kitchen Password", htmlMessage);
                    _logger.LogInformation("Successfully sent password reset email to '{Email}'.", email);
                    if (_env.IsDevelopment())
                    {
                        Console.WriteLine($"[EMAIL SENT] Password reset email sent successfully to {email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to '{Email}'. Full error: {Message}", email, ex.Message);
                    if (_env.IsDevelopment())
                    {
                        Console.WriteLine($"[SMTP ERROR] Failed to send email to {email}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"[SMTP INNER ERROR]: {ex.InnerException.Message}");
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Password reset requested for non-existent email '{Email}'.", email);
                if (_env.IsDevelopment())
                {
                    Console.WriteLine($"[DEV DEBUG] Password reset requested for non-existent email '{email}'. No user exists with this email.");
                }
            }

            // Always return the generic message for privacy & security
            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Agar email registered hai to instructions bhej di jayengi."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Invalid password reset request." });
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new AuthResponse { Success = true, Message = "Your password has been reset successfully. You can now login." });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new AuthResponse { Success = false, Message = errors });
        }

        // ============================================================
        // CURRENT USER INFO ("WHO AM I") API
        // ============================================================
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? user.UserRole;

            string? sellerStatus = null;
            int? kitchenId = null;
            if (role == "Seller")
            {
                var kitchen = await _context.Kitchens.Include(k => k.KitchenOwner).FirstOrDefaultAsync(k => k.KitchenOwner.Email == user.Email);
                sellerStatus = kitchen?.ApprovalStatus ?? user.SellerStatus;
                kitchenId = kitchen?.Id;

                if (!string.IsNullOrEmpty(sellerStatus) && !string.Equals(user.SellerStatus, sellerStatus, StringComparison.OrdinalIgnoreCase))
                {
                    user.SellerStatus = sellerStatus;
                    await _userManager.UpdateAsync(user);
                }

                if (!string.Equals(sellerStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(sellerStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        var reason = !string.IsNullOrWhiteSpace(kitchen?.RejectionReason)
                            ? $" Rejection reason: {kitchen.RejectionReason}"
                            : string.Empty;
                        return StatusCode(403, new AuthResponse
                        {
                            Success = false,
                            Message = $"Your seller account has been rejected by the administrator.{reason}",
                            Role = role,
                            SellerStatus = "Rejected"
                        });
                    }

                    return StatusCode(403, new AuthResponse
                    {
                        Success = false,
                        Message = "Your seller account is waiting for admin approval. Please wait for an administrator to approve your kitchen on the web portal.",
                        Role = role,
                        SellerStatus = "Pending"
                    });
                }
            }

            int? customerId = null;
            if (role == "Customer")
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
                customerId = customer?.Id;
            }

            return Ok(new
            {
                success = true,
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                phoneNumber = user.PhoneNumber,
                city = user.City,
                role = role,
                sellerStatus = sellerStatus,
                kitchenId = kitchenId,
                customerId = customerId
            });
        }

        // ============================================================
        // LOGOUT & TOKEN REVOCATION API
        // ============================================================
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    user.RefreshTokenHash = null;
                    user.RefreshTokenExpiryTime = null;
                    await _userManager.UpdateAsync(user);
                }
            }

            return Ok(new AuthResponse { Success = true, Message = "Logged out successfully." });
        }
    }
}
