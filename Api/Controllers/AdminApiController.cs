using Cloud_Kitchen_web_App.Api.DTOs;
using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly CloudKitchenDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminApiController(
            CloudKitchenDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // 1. DASHBOARD & STATS
        // ============================================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var today = DateTime.UtcNow.Date;

            var orders = await _context.Orders.AsNoTracking().ToListAsync();
            var kitchens = await _context.Kitchens.AsNoTracking().ToListAsync();
            var totalCustomers = await _context.Customers.CountAsync();
            var totalSellers = await _context.KitchenOwners.CountAsync();

            var totalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.Total);
            var todayRevenue = orders.Where(o => o.PlacedAt >= today && o.Status != "Cancelled").Sum(o => o.Total);

            var pendingOrders = orders.Count(o =>
                o.Status == "Order Placed" ||
                o.Status == "Order Accepted" ||
                o.Status == "Kitchen Preparing" ||
                o.Status == "Preparing" ||
                o.Status == "Out for Delivery");

            var recentOrders = orders.OrderByDescending(o => o.PlacedAt).Take(10).Select(o => new AdminOrderSummaryDto
            {
                Id = o.Id,
                Customer = o.Customer,
                KitchenName = kitchens.FirstOrDefault(k => k.Id == o.KitchenId)?.Name ?? "Unknown",
                Total = o.Total,
                Status = o.Status,
                PlacedAt = o.PlacedAt
            }).ToList();

            var dto = new AdminDashboardDto
            {
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                TotalOrders = orders.Count,
                PendingOrders = pendingOrders,
                TotalKitchens = kitchens.Count,
                PendingKitchens = kitchens.Count(k => k.ApprovalStatus == "Pending"),
                ApprovedKitchens = kitchens.Count(k => k.ApprovalStatus == "Approved"),
                RejectedKitchens = kitchens.Count(k => k.ApprovalStatus == "Rejected"),
                TotalCustomers = totalCustomers,
                TotalSellers = totalSellers,
                RecentOrders = recentOrders
            };

            return Ok(new { success = true, dashboard = dto });
        }

        // ============================================================
        // 2. KITCHENS MANAGEMENT & APPROVALS
        // ============================================================

        [HttpGet("kitchens")]
        public async Task<IActionResult> GetKitchens([FromQuery] string? status, [FromQuery] string? search)
        {
            var query = _context.Kitchens
                .AsNoTracking()
                .Include(k => k.KitchenOwner)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(k => k.ApprovalStatus.ToLower() == status.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(k =>
                    k.Name.ToLower().Contains(s) ||
                    k.City.ToLower().Contains(s) ||
                    k.Cuisine.ToLower().Contains(s) ||
                    (k.KitchenOwner != null && k.KitchenOwner.Name.ToLower().Contains(s)) ||
                    (k.KitchenOwner != null && k.KitchenOwner.Email.ToLower().Contains(s)));
            }

            var kitchens = await query.OrderByDescending(k => k.Id).ToListAsync();

            var kitchenIds = kitchens.Select(k => k.Id).ToList();
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => kitchenIds.Contains(o.KitchenId))
                .Select(o => new { o.KitchenId, o.Total, o.Status })
                .ToListAsync();

            var result = kitchens.Select(k =>
            {
                var kitchenOrders = orders.Where(o => o.KitchenId == k.Id).ToList();
                return new AdminKitchenDto
                {
                    Id = k.Id,
                    Name = k.Name,
                    OwnerName = k.KitchenOwner?.Name ?? string.Empty,
                    OwnerEmail = k.KitchenOwner?.Email ?? string.Empty,
                    Phone = k.Phone,
                    Cuisine = k.Cuisine,
                    City = k.City,
                    Area = k.Area,
                    Address = k.Address,
                    Image = k.Image,
                    IsOpen = k.IsOpen,
                    ApprovalStatus = k.ApprovalStatus,
                    RejectionReason = k.RejectionReason,
                    Rating = k.Rating,
                    TotalOrders = kitchenOrders.Count,
                    TotalSales = kitchenOrders.Where(o => o.Status != "Cancelled").Sum(o => o.Total)
                };
            }).ToList();

            return Ok(new { success = true, kitchens = result });
        }

        [HttpGet("kitchens/{id}")]
        public async Task<IActionResult> GetKitchen(int id)
        {
            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .Include(k => k.KitchenOwner)
                .Include(k => k.MenuItems)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.KitchenId == id)
                .ToListAsync();

            var dto = new AdminKitchenDto
            {
                Id = kitchen.Id,
                Name = kitchen.Name,
                OwnerName = kitchen.KitchenOwner?.Name ?? string.Empty,
                OwnerEmail = kitchen.KitchenOwner?.Email ?? string.Empty,
                Phone = kitchen.Phone,
                Cuisine = kitchen.Cuisine,
                City = kitchen.City,
                Area = kitchen.Area,
                Address = kitchen.Address,
                Image = kitchen.Image,
                IsOpen = kitchen.IsOpen,
                ApprovalStatus = kitchen.ApprovalStatus,
                RejectionReason = kitchen.RejectionReason,
                Rating = kitchen.Rating,
                TotalOrders = orders.Count,
                TotalSales = orders.Where(o => o.Status != "Cancelled").Sum(o => o.Total)
            };

            return Ok(new { success = true, kitchen = dto });
        }

        [HttpPost("kitchens/{id}/approve")]
        public async Task<IActionResult> ApproveKitchen(int id)
        {
            var kitchen = await _context.Kitchens
                .Include(k => k.KitchenOwner)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            kitchen.ApprovalStatus = "Approved";
            kitchen.RejectionReason = string.Empty;
            kitchen.IsOpen = true;

            if (kitchen.KitchenOwner != null && !string.IsNullOrWhiteSpace(kitchen.KitchenOwner.Email))
            {
                var sellerUser = await _userManager.FindByEmailAsync(kitchen.KitchenOwner.Email);
                if (sellerUser != null)
                {
                    sellerUser.SellerStatus = "Approved";
                    sellerUser.UserRole = "Seller";

                    var isInRole = await _userManager.IsInRoleAsync(sellerUser, "Seller");
                    if (!isInRole)
                    {
                        await _userManager.AddToRoleAsync(sellerUser, "Seller");
                    }
                    await _userManager.UpdateAsync(sellerUser);
                }

                // Notify seller
                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString("N"),
                    RecipientType = "Seller",
                    RecipientId = kitchen.KitchenOwner.Id,
                    Title = "Kitchen Application Approved! 🎉",
                    Body = $"Congratulations! Your kitchen '{kitchen.Name}' has been approved. You can now manage your menu and start accepting orders.",
                    Time = DateTime.UtcNow,
                    Status = "Unread"
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Kitchen approved successfully." });
        }

        [HttpPost("kitchens/{id}/reject")]
        public async Task<IActionResult> RejectKitchen(int id, [FromBody] RejectKitchenRequestDto request)
        {
            var kitchen = await _context.Kitchens
                .Include(k => k.KitchenOwner)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            string reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? "Kitchen application rejected by administrator."
                : request.Reason.Trim();

            kitchen.ApprovalStatus = "Rejected";
            kitchen.RejectionReason = reason;
            kitchen.IsOpen = false;

            if (kitchen.KitchenOwner != null && !string.IsNullOrWhiteSpace(kitchen.KitchenOwner.Email))
            {
                var sellerUser = await _userManager.FindByEmailAsync(kitchen.KitchenOwner.Email);
                if (sellerUser != null)
                {
                    sellerUser.SellerStatus = "Rejected";
                    await _userManager.UpdateAsync(sellerUser);
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString("N"),
                    RecipientType = "Seller",
                    RecipientId = kitchen.KitchenOwner.Id,
                    Title = "Kitchen Application Update",
                    Body = $"Your kitchen '{kitchen.Name}' was not approved. Reason: {reason}",
                    Time = DateTime.UtcNow,
                    Status = "Unread"
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Kitchen rejected successfully." });
        }

        [HttpPost("kitchens/{id}/toggle-status")]
        public async Task<IActionResult> ToggleKitchenStatus(int id)
        {
            var kitchen = await _context.Kitchens.FirstOrDefaultAsync(k => k.Id == id);
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            if (kitchen.ApprovalStatus != "Approved")
            {
                return BadRequest(new { success = false, message = "Only approved kitchens can be toggled open or closed." });
            }

            kitchen.IsOpen = !kitchen.IsOpen;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                isOpen = kitchen.IsOpen,
                message = kitchen.IsOpen ? "Kitchen opened." : "Kitchen closed."
            });
        }

        [HttpDelete("kitchens/{id}")]
        public async Task<IActionResult> DeleteKitchen(int id)
        {
            var kitchen = await _context.Kitchens
                .Include(k => k.MenuItems)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            var hasActiveOrders = await _context.Orders.AnyAsync(o =>
                o.KitchenId == id &&
                o.Status != "Delivered" &&
                o.Status != "Cancelled");

            if (hasActiveOrders)
            {
                return BadRequest(new { success = false, message = "Cannot delete a kitchen with active ongoing orders." });
            }

            _context.Kitchens.Remove(kitchen);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Kitchen deleted successfully." });
        }

        // ============================================================
        // 3. CATEGORIES MANAGEMENT
        // ============================================================

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.AdminCategories.AsNoTracking().ToListAsync();
            var menuItems = await _context.MenuItems.AsNoTracking().Select(m => m.Category).ToListAsync();

            var dtos = categories.Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                ImagePath = c.ImagePath,
                CreatedAt = c.CreatedAt,
                ItemCount = menuItems.Count(m => string.Equals(m, c.Name, StringComparison.OrdinalIgnoreCase))
            }).ToList();

            return Ok(new { success = true, categories = dtos });
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                return BadRequest(new { success = false, message = "Category name is required." });
            }

            var name = request.Name.Trim();
            var exists = await _context.AdminCategories.AnyAsync(c => c.Name.ToLower() == name.ToLower());
            if (exists)
            {
                return BadRequest(new { success = false, message = "This category already exists." });
            }

            var category = new AdminCategory
            {
                Name = name,
                IsActive = request.IsActive,
                ImagePath = request.ImagePath?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Category created successfully.", categoryId = category.Id });
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                return BadRequest(new { success = false, message = "Category name is required." });
            }

            var category = await _context.AdminCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound(new { success = false, message = "Category not found." });

            var name = request.Name.Trim();
            var duplicate = await _context.AdminCategories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                return BadRequest(new { success = false, message = "Another category with this name already exists." });
            }

            category.Name = name;
            category.IsActive = request.IsActive;
            if (request.ImagePath != null) category.ImagePath = request.ImagePath.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Category updated successfully." });
        }

        [HttpPost("categories/{id}/toggle")]
        public async Task<IActionResult> ToggleCategory(int id)
        {
            var category = await _context.AdminCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound(new { success = false, message = "Category not found." });

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                isActive = category.IsActive,
                message = category.IsActive ? "Category activated." : "Category deactivated."
            });
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.AdminCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound(new { success = false, message = "Category not found." });

            _context.AdminCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Category deleted successfully." });
        }

        // ============================================================
        // 4. USERS MANAGEMENT
        // ============================================================

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] string? search)
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();

            if (!string.IsNullOrWhiteSpace(role) && !role.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                users = users.Where(u => u.UserRole.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                users = users.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(s))).ToList();
            }

            var dtos = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Role = u.UserRole,
                City = u.City,
                IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                SellerStatus = u.SellerStatus
            }).ToList();

            return Ok(new { success = true, users = dtos });
        }

        [HttpPost("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == id)
            {
                return BadRequest(new { success = false, message = "You cannot block your own admin account." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { success = false, message = "User not found." });

            bool isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }

            return Ok(new
            {
                success = true,
                isLocked = !isLocked,
                message = isLocked ? "User account unblocked." : "User account blocked."
            });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> EditUser(string id, [FromBody] EditUserRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { success = false, message = "User not found." });

            user.FullName = request.FullName?.Trim() ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber?.Trim() ?? user.PhoneNumber;
            user.City = request.City?.Trim() ?? user.City;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = errors });
            }

            return Ok(new { success = true, message = "User updated successfully." });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == id)
            {
                return BadRequest(new { success = false, message = "You cannot delete your own admin account." });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { success = false, message = "User not found." });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = errors });
            }

            return Ok(new { success = true, message = "User deleted successfully." });
        }

        // ============================================================
        // 5. GLOBAL ORDERS MANAGEMENT
        // ============================================================

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] string? search)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Kitchen)
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.Status.ToLower() == status.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o =>
                    o.Id.ToLower().Contains(s) ||
                    o.Customer.ToLower().Contains(s) ||
                    o.Phone.Contains(s) ||
                    o.Kitchen.Name.ToLower().Contains(s));
            }

            var orders = await query.OrderByDescending(o => o.PlacedAt).ToListAsync();

            return Ok(new { success = true, orders });
        }

        [HttpPost("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound(new { success = false, message = "Order not found." });

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                return BadRequest(new { success = false, message = "Delivered or already cancelled orders cannot be cancelled." });
            }

            order.Status = "Cancelled";

            // Notify customer
            var customerNotification = new Notification
            {
                Id = Guid.NewGuid().ToString("N"),
                RecipientType = "Customer",
                RecipientId = order.CustomerId,
                Title = "Order Cancelled by Admin",
                Body = $"Your order #{order.Id} was cancelled by administrator.",
                Time = DateTime.UtcNow,
                Status = "Unread",
                OrderId = order.Id
            };
            _context.Notifications.Add(customerNotification);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order cancelled successfully." });
        }

        // ============================================================
        // 6. ADMIN NOTIFICATIONS
        // ============================================================

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientType == "Admin" && n.RecipientId == 0)
                .OrderByDescending(n => n.Time)
                .ToListAsync();

            return Ok(new { success = true, notifications });
        }

        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(string id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientType == "Admin" && n.RecipientId == 0);

            if (notification == null) return NotFound(new { success = false, message = "Notification not found." });

            notification.Status = "Read";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notification marked as read." });
        }

        [HttpPost("notifications/read-all")]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientType == "Admin" && n.RecipientId == 0 && n.Status == "Unread")
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.Status = "Read";
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, count = unreadNotifications.Count, message = "All admin notifications marked as read." });
        }
    }
}
