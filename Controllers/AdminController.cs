using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CloudKitchenDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(
            CloudKitchenDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ============================================================
        // ADMIN LOGIN
        // ============================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Please enter username/email and password.";

                return View();
            }

            username = username.Trim();

            ApplicationUser? admin = null;

            // --------------------------------------------------------
            // FIND ADMIN BY EMAIL
            // --------------------------------------------------------

            if (username.Contains("@"))
            {
                admin = await _userManager.FindByEmailAsync(username);
            }

            // --------------------------------------------------------
            // FIND ADMIN BY USERNAME
            // --------------------------------------------------------

            if (admin == null)
            {
                admin = await _userManager.FindByNameAsync(username);
            }

            if (admin == null)
            {
                ViewBag.Error =
                    "Invalid username/email or password.";

                return View();
            }

            // --------------------------------------------------------
            // VERIFY ADMIN ROLE
            // --------------------------------------------------------

            var isInAdminRole = await _userManager.IsInRoleAsync(admin, "Admin");
            var isUserRoleAdmin = string.Equals(admin.UserRole, "Admin", StringComparison.OrdinalIgnoreCase);

            var userRoles = await _userManager.GetRolesAsync(admin);
            if (userRoles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                isInAdminRole = true;
            }

            if (!isInAdminRole && !isUserRoleAdmin)
            {
                ViewBag.Error =
                    "You are not authorized as an administrator.";

                return View();
            }

            // Auto-sync: If user has Admin in UserRole or AspNetUserRoles, ensure both are in sync
            if (!isInAdminRole && isUserRoleAdmin)
            {
                await _userManager.AddToRoleAsync(admin, "Admin");
            }
            else if (isInAdminRole && !isUserRoleAdmin)
            {
                admin.UserRole = "Admin";
                await _userManager.UpdateAsync(admin);
            }

            // --------------------------------------------------------
            // VERIFY PASSWORD
            // --------------------------------------------------------

            var result =
                await _signInManager.CheckPasswordSignInAsync(
                    admin,
                    password,
                    lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ViewBag.Error =
                    "Your account is temporarily locked because of multiple failed login attempts.";

                return View();
            }

            if (!result.Succeeded)
            {
                ViewBag.Error =
                    "Invalid username/email or password.";

                return View();
            }

            // --------------------------------------------------------
            // SIGN IN
            // --------------------------------------------------------

            await _signInManager.SignInAsync(
                admin,
                isPersistent: false);

            return RedirectToAction(
                nameof(Dashboard));
        }

        // ============================================================
        // DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);

            // 1. 7-Day Revenue Trend
            var rawRecentOrders = await _context.Orders
                .Where(o => o.Status != "Cancelled" && o.PlacedAt >= sevenDaysAgo)
                .Select(o => new { o.PlacedAt, o.Total })
                .ToListAsync();

            var revenueData = rawRecentOrders
                .GroupBy(o => o.PlacedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.Total) })
                .ToList();

            var last7DaysLabels = new List<string>();
            var last7DaysRevenue = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                last7DaysLabels.Add(date.ToString("MMM dd"));
                var dailyRevenue = revenueData.FirstOrDefault(r => r.Date == date)?.Revenue ?? 0;
                last7DaysRevenue.Add(dailyRevenue);
            }

            // 2. Top 5 Performing Kitchens
            var topKitchensData = await _context.Orders
                .Include(o => o.Kitchen)
                .Where(o => o.Status != "Cancelled" && o.Kitchen != null)
                .GroupBy(o => o.Kitchen.Name)
                .Select(g => new { KitchenName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topKitchenNames = topKitchensData.Select(x => x.KitchenName).ToList();
            var topKitchenOrderCounts = topKitchensData.Select(x => x.Count).ToList();

            var dashboardModel =
                new AdminDashboardViewModel
                {
                    TotalUsers =
                        await _userManager.Users.CountAsync(),

                    TotalKitchens =
                        await _context.Kitchens.CountAsync(),

                    TotalOrders =
                        await _context.Orders.CountAsync(),

                    Revenue =
                        await _context.Orders
                            .Where(o =>
                                o.Status != "Cancelled")
                            .SumAsync(o =>
                                (decimal?)o.Total) ?? 0,

                    RecentOrders =
                        await _context.Orders
                            .Include(o => o.Kitchen)
                            .Include(o => o.CustomerAccount)
                            .OrderByDescending(o => o.PlacedAt)
                            .Take(5)
                            .Select(o =>
                                new RecentOrderViewModel
                                {
                                    OrderId = o.Id,
                                    Customer = !string.IsNullOrWhiteSpace(o.CustomerAccount.Name)
                                        ? o.CustomerAccount.Name
                                        : (!string.IsNullOrWhiteSpace(o.Customer) && !o.Customer.Contains("@") ? o.Customer : (o.CustomerAccount.Name ?? o.Customer)),
                                    Kitchen = o.Kitchen.Name,
                                    Status = o.Status,
                                    Amount = o.Total
                                })
                            .ToListAsync(),

                    Last7DaysLabels = last7DaysLabels,
                    Last7DaysRevenue = last7DaysRevenue,
                    TopKitchenNames = topKitchenNames,
                    TopKitchenOrderCounts = topKitchenOrderCounts
                };

            return View(dashboardModel);
        }

        // ============================================================
        // SETTINGS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var categories = await _context.AdminCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;

            return View();
        }


        // ============================================================
        // CREATE CATEGORY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Category name is required.";
                return RedirectToAction(nameof(Settings));
            }

            name = name.Trim();

            var existingCategory = await _context.AdminCategories
                .FirstOrDefaultAsync(c =>
                    c.Name.ToLower() == name.ToLower());

            if (existingCategory != null)
            {
                TempData["Error"] = "This category already exists.";
                return RedirectToAction(nameof(Settings));
            }

            var category = new AdminCategory
            {
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminCategories.Add(category);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Category added successfully.";

            return RedirectToAction(nameof(Settings));
        }


        // ============================================================
        // UPDATE CATEGORY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(
            int id,
            string name)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Invalid category information.";
                return RedirectToAction(nameof(Settings));
            }

            name = name.Trim();

            var category = await _context.AdminCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Settings));
            }

            var duplicate = await _context.AdminCategories
                .AnyAsync(c =>
                    c.Id != id &&
                    c.Name.ToLower() == name.ToLower());

            if (duplicate)
            {
                TempData["Error"] =
                    "Another category with this name already exists.";

                return RedirectToAction(nameof(Settings));
            }

            category.Name = name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";

            return RedirectToAction(nameof(Settings));
        }


        // ============================================================
        // TOGGLE CATEGORY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategory(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid category.";
                return RedirectToAction(nameof(Settings));
            }

            var category = await _context.AdminCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Settings));
            }

            category.IsActive = !category.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                category.IsActive
                    ? "Category activated successfully."
                    : "Category deactivated successfully.";

            return RedirectToAction(nameof(Settings));
        }


        // ============================================================
        // DELETE CATEGORY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid category.";
                return RedirectToAction(nameof(Settings));
            }

            var category = await _context.AdminCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Settings));
            }

            _context.AdminCategories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted successfully.";

            return RedirectToAction(nameof(Settings));
        }

        // ============================================================
        // ADMIN PROFILE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var admin =
                await _userManager.GetUserAsync(User);

            if (admin == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }

            return View(admin);
        }

        // ============================================================
        // MANAGE REVIEWS
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageReviews()
        {
            var reviews = await _context.OrderReviews
                .AsNoTracking()
                .Include(r => r.Kitchen)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AdminReviewViewModel
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    KitchenId = r.KitchenId,
                    KitchenName = r.Kitchen != null ? r.Kitchen.Name : "N/A",
                    CustomerId = r.CustomerId,
                    CustomerName = r.Customer != null ? r.Customer.Name : "Customer",
                    CustomerEmail = r.Customer != null ? r.Customer.Email : string.Empty,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return View(reviews);
        }

        // ============================================================
        // MANAGE KITCHENS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ManageKitchens()
        {
            var kitchens =
                await _context.Kitchens
                    .Include(k => k.KitchenOwner)
                    .OrderByDescending(k => k.Id)
                    .ToListAsync();

            return View(kitchens);
        }

        // ============================================================
        // APPROVE KITCHEN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveKitchen(int id)
        {
            var kitchen =
                await _context.Kitchens
                    .Include(k => k.KitchenOwner)
                    .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // FIND SELLER
            // --------------------------------------------------------

            ApplicationUser? seller = null;

            if (kitchen.KitchenOwner != null &&
                !string.IsNullOrWhiteSpace(
                    kitchen.KitchenOwner.Email))
            {
                seller =
                    await _userManager.FindByEmailAsync(
                        kitchen.KitchenOwner.Email);
            }

            // --------------------------------------------------------
            // APPROVE KITCHEN
            // --------------------------------------------------------

            kitchen.ApprovalStatus = "Approved";
            kitchen.RejectionReason = string.Empty;

            // Approved kitchens can be opened.
            kitchen.IsOpen = true;

            // --------------------------------------------------------
            // APPROVE SELLER
            // --------------------------------------------------------

            if (seller != null)
            {
                seller.SellerStatus = "Approved";
                seller.UserRole = "Seller";

                var isSeller =
                    await _userManager.IsInRoleAsync(
                        seller,
                        "Seller");

                if (!isSeller)
                {
                    var roleResult =
                        await _userManager.AddToRoleAsync(
                            seller,
                            "Seller");

                    if (!roleResult.Succeeded)
                    {
                        TempData["Error"] =
                            "Kitchen could not be approved because the Seller role could not be assigned.";

                        return RedirectToAction(
                            nameof(ManageKitchens));
                    }
                }

                var sellerUpdateResult =
                    await _userManager.UpdateAsync(
                        seller);

                if (!sellerUpdateResult.Succeeded)
                {
                    TempData["Error"] =
                        "Kitchen could not be approved because the seller account could not be updated.";

                    return RedirectToAction(
                        nameof(ManageKitchens));
                }
            }
            else
            {
                TempData["Error"] =
                    "Kitchen owner account could not be found.";

                return RedirectToAction(
                    nameof(ManageKitchens));
            }

            // --------------------------------------------------------
            // UPDATE ADMIN & SELLER NOTIFICATIONS
            // --------------------------------------------------------

            var adminNotifs = await _context.Notifications
                .Where(n => n.RecipientType == "Admin" && n.RecipientId == 0 && n.Body.Contains(kitchen.Name))
                .ToListAsync();

            foreach (var an in adminNotifs)
            {
                an.Status = "Read";
            }

            if (kitchen.KitchenOwner != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Kitchen Approved! 🎉",
                    Body = $"Congratulations! Your kitchen '{kitchen.Name}' has been approved by the admin. You can now manage your menu and start taking orders.",
                    Time = DateTime.UtcNow,
                    Status = "Unread",
                    Link = "/Seller/SellerDashboard",
                    RecipientType = "Seller",
                    RecipientId = kitchen.KitchenOwner.Id
                });
            }

            // --------------------------------------------------------
            // SAVE
            // --------------------------------------------------------

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kitchen approved successfully. The seller can now log in.";

            return RedirectToAction(
                nameof(ManageKitchens));
        }

        // ============================================================
        // REJECT KITCHEN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectKitchen(
            int id,
            string rejectionReason)
        {
            var kitchen =
                await _context.Kitchens
                    .Include(k => k.KitchenOwner)
                    .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // REJECTION REASON
            // --------------------------------------------------------

            var reason =
                string.IsNullOrWhiteSpace(
                    rejectionReason)
                    ? "Kitchen application rejected by administrator."
                    : rejectionReason.Trim();

            // --------------------------------------------------------
            // REJECT KITCHEN
            // --------------------------------------------------------

            kitchen.ApprovalStatus = "Rejected";
            kitchen.RejectionReason = reason;

            // Rejected kitchen must never be open.
            kitchen.IsOpen = false;

            // --------------------------------------------------------
            // REJECT SELLER
            // --------------------------------------------------------

            if (kitchen.KitchenOwner != null &&
                !string.IsNullOrWhiteSpace(
                    kitchen.KitchenOwner.Email))
            {
                var seller =
                    await _userManager.FindByEmailAsync(
                        kitchen.KitchenOwner.Email);

                if (seller != null)
                {
                    seller.SellerStatus = "Rejected";

                    await _userManager.UpdateAsync(
                        seller);
                }
            }

            // --------------------------------------------------------
            // UPDATE ADMIN & SELLER NOTIFICATIONS
            // --------------------------------------------------------

            var adminNotifs = await _context.Notifications
                .Where(n => n.RecipientType == "Admin" && n.RecipientId == 0 && n.Body.Contains(kitchen.Name))
                .ToListAsync();

            foreach (var an in adminNotifs)
            {
                an.Status = "Read";
            }

            if (kitchen.KitchenOwner != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Kitchen Application Update",
                    Body = $"Your kitchen application for '{kitchen.Name}' was not approved. Reason: {reason}",
                    Time = DateTime.UtcNow,
                    Status = "Unread",
                    Link = "/Seller/Setting",
                    RecipientType = "Seller",
                    RecipientId = kitchen.KitchenOwner.Id
                });
            }

            // --------------------------------------------------------
            // SAVE
            // --------------------------------------------------------

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kitchen rejected successfully.";

            return RedirectToAction(
                nameof(ManageKitchens));
        }

        // ============================================================
        // TOGGLE KITCHEN OPEN / CLOSED STATUS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleKitchenStatus(
            int id)
        {
            var kitchen =
                await _context.Kitchens
                    .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // ONLY APPROVED KITCHENS CAN BE OPEN
            // --------------------------------------------------------

            if (!string.Equals(
                    kitchen.ApprovalStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only an approved kitchen can be opened.";

                return RedirectToAction(
                    nameof(ManageKitchens));
            }

            kitchen.IsOpen =
                !kitchen.IsOpen;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                kitchen.IsOpen
                    ? "Kitchen is now open."
                    : "Kitchen is now closed.";

            return RedirectToAction(
                nameof(ManageKitchens));
        }

        // ============================================================
        // DELETE KITCHEN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKitchen(
            int id)
        {
            var kitchen =
                await _context.Kitchens
                    .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null)
            {
                return NotFound();
            }

            _context.Kitchens.Remove(kitchen);

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Kitchen deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "This kitchen cannot be deleted because it is linked with existing records.";
            }

            return RedirectToAction(
                nameof(ManageKitchens));
        }

        // ============================================================
        // MANAGE ORDERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ManageOrders()
        {
            var orders =
                await _context.Orders
                    .Include(o => o.Kitchen)
                    .Include(o => o.CustomerAccount)
                    .OrderByDescending(o => o.PlacedAt)
                    .ToListAsync();

            return View(orders);
        }

        // ============================================================
        // UPDATE ORDER STATUS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(
            string id,
            string status)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(status))
            {
                return RedirectToAction(
                    nameof(ManageOrders));
            }

            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var allowedStatuses =
                new[]
                {
                    "Order Placed",
                    "Accepted",
                    "Preparing",
                    "Out for Delivery",
                    "Delivered",
                    "Cancelled"
                };

            if (!allowedStatuses.Contains(
                    status))
            {
                TempData["Error"] =
                    "Invalid order status.";

                return RedirectToAction(
                    nameof(ManageOrders));
            }

            // --------------------------------------------------------
            // DON'T CHANGE DELIVERED ORDER
            // --------------------------------------------------------

            if (string.Equals(
                    order.Status,
                    "Delivered",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    status,
                    "Delivered",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "A delivered order cannot be changed.";

                return RedirectToAction(
                    nameof(ManageOrders));
            }

            order.Status =
                status.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Order status updated successfully.";

            return RedirectToAction(
                nameof(ManageOrders));
        }

        // ============================================================
        // CANCEL ORDER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(
                    nameof(ManageOrders));
            }

            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (string.Equals(
                    order.Status,
                    "Delivered",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "A delivered order cannot be cancelled.";

                return RedirectToAction(
                    nameof(ManageOrders));
            }

            order.Status =
                "Cancelled";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Order cancelled successfully.";

            return RedirectToAction(
                nameof(ManageOrders));
        }

        // ============================================================
        // MANAGE USERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var users =
                await _userManager.Users
                    .OrderByDescending(u => u.Id)
                    .ToListAsync();

            return View(users);
        }

        // ============================================================
        // VIEW USER
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ViewUser(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ============================================================
        // EDIT USER
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EditUser(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(
            string id,
            string fullName,
            string email,
            string phoneNumber,
            string city,
            string userRole,
            string sellerStatus)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.FullName =
                fullName?.Trim()
                ?? string.Empty;

            user.Email =
                email?.Trim()
                ?? string.Empty;

            user.UserName =
                user.Email;

            user.NormalizedEmail =
                _userManager.NormalizeEmail(
                    user.Email);

            user.NormalizedUserName =
                _userManager.NormalizeName(
                    user.UserName);

            user.PhoneNumber =
                phoneNumber?.Trim();

            user.City =
                city?.Trim()
                ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(
                    userRole))
            {
                user.UserRole =
                    userRole.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                    sellerStatus))
            {
                user.SellerStatus =
                    sellerStatus.Trim();
            }

            var result =
                await _userManager.UpdateAsync(
                    user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(user);
            }

            // --------------------------------------------------------
            // SYNCHRONIZE ADMIN ROLE
            // --------------------------------------------------------

            var currentRoles =
                await _userManager.GetRolesAsync(
                    user);

            if (string.Equals(
                    user.UserRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!currentRoles.Contains(
                        "Admin"))
                {
                    await _userManager.AddToRoleAsync(
                        user,
                        "Admin");
                }
            }
            else
            {
                if (currentRoles.Contains(
                        "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(
                        user,
                        "Admin");
                }
            }

            return RedirectToAction(
                nameof(ManageUsers));
        }

        // ============================================================
        // TOGGLE USER LOCK / UNLOCK
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // PROTECT CURRENT ADMIN
            // --------------------------------------------------------

            var currentAdmin =
                await _userManager.GetUserAsync(User);

            if (currentAdmin != null &&
                currentAdmin.Id == user.Id)
            {
                TempData["Error"] =
                    "You cannot block your own admin account.";

                return RedirectToAction(
                    nameof(ManageUsers));
            }

            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd >
                    DateTimeOffset.UtcNow)
            {
                await _userManager
                    .SetLockoutEndDateAsync(
                        user,
                        null);
            }
            else
            {
                await _userManager
                    .SetLockoutEndDateAsync(
                        user,
                        DateTimeOffset.UtcNow
                            .AddYears(100));
            }

            return RedirectToAction(
                nameof(ManageUsers));
        }

        // ============================================================
        // DELETE USER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // PROTECT CURRENT ADMIN
            // --------------------------------------------------------

            var currentAdmin =
                await _userManager.GetUserAsync(User);

            if (currentAdmin != null &&
                currentAdmin.Id == user.Id)
            {
                TempData["Error"] =
                    "You cannot delete your own admin account.";

                return RedirectToAction(
                    nameof(ManageUsers));
            }

            var result =
                await _userManager.DeleteAsync(
                    user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to delete this user.";

                return RedirectToAction(
                    nameof(ManageUsers));
            }

            TempData["Success"] =
                "User deleted successfully.";

            return RedirectToAction(
                nameof(ManageUsers));
        }

        // ============================================================
        // ADMIN NOTIFICATIONS
        // ============================================================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Notifications()
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            try
            {
                // Ensure existing pending kitchens have admin notifications
                var pendingKitchens = await _context.Kitchens
                    .Include(k => k.KitchenOwner)
                    .Where(k => k.ApprovalStatus == "Pending")
                    .ToListAsync();

                bool changes = false;
                foreach (var pk in pendingKitchens)
                {
                    bool exists = await _context.Notifications.AnyAsync(n =>
                        n.RecipientType == "Admin" &&
                        n.RecipientId == 0 &&
                        n.Title.Contains("Approval") &&
                        n.Body.Contains(pk.Name));

                    if (!exists)
                    {
                        var ownerName = pk.KitchenOwner?.Name ?? "Seller";
                        var ownerEmail = pk.KitchenOwner?.Email ?? "";
                        _context.Notifications.Add(new Notification
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Title = "New Kitchen Approval Request",
                            Body = $"Kitchen '{pk.Name}' registered by {ownerName} ({ownerEmail}) is waiting for admin approval.",
                            Time = DateTime.UtcNow,
                            Status = "Unread",
                            Link = "/Admin/ManageKitchens",
                            RecipientType = "Admin",
                            RecipientId = 0
                        });
                        changes = true;
                    }
                }

                if (changes)
                {
                    await _context.SaveChangesAsync();
                }

                var notifications = await _context.Notifications
                    .AsNoTracking()
                    .Where(n =>
                        n.RecipientType == "Admin" &&
                        n.RecipientId == 0)
                    .OrderByDescending(n => n.Time)
                    .Take(30)
                    .Select(n => new
                    {
                        id = n.Id,
                        title = n.Title,
                        body = n.Body,
                        time = n.Time.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        timeAgo = GetTimeAgo(n.Time),
                        status = n.Status,
                        link = string.IsNullOrWhiteSpace(n.Link) ? "/Admin/ManageKitchens" : n.Link
                    })
                    .ToListAsync();

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.RecipientType == "Admin" && n.RecipientId == 0 && n.Status == "Unread");

                return Json(new { success = true, unreadCount, notifications });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, unreadCount = 0, notifications = new object[0] });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkNotificationRead(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.RecipientType == "Admin" &&
                    n.RecipientId == 0);

            if (notification != null)
            {
                notification.Status = "Read";
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(new { success = true });
            }

            if (notification != null && !string.IsNullOrWhiteSpace(notification.Link))
            {
                return Redirect(notification.Link);
            }

            return RedirectToAction(nameof(ManageKitchens));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n =>
                    n.RecipientType == "Admin" &&
                    n.RecipientId == 0 &&
                    n.Status == "Unread")
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.Status = "Read";
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(new { success = true, count = unreadNotifications.Count });
            }

            return RedirectToAction(nameof(Dashboard));
        }

        private static string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.UtcNow - dt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
            return dt.ToString("dd MMM yyyy");
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
                nameof(Login));
        }
    }
}
