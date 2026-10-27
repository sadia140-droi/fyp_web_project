using Cloud_Kitchen_web_App.Api.DTOs;
using Cloud_Kitchen_web_App.Api.DTOs.Requests;
using Cloud_Kitchen_web_App.Api.DTOs.Responses;
using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Seller")]
    public class SellerApiController : ControllerBase
    {
        private readonly CloudKitchenDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SellerApiController(CloudKitchenDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Kitchen?> GetSellerKitchenAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return null;

            var owner = await _context.KitchenOwners.FirstOrDefaultAsync(o => o.Email == email);
            if (owner == null) return null;

            return await _context.Kitchens.Include(k => k.MenuItems).FirstOrDefaultAsync(k => k.KitchenOwnerId == owner.Id);
        }

        private async Task<KitchenOwner?> GetSellerOwnerAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return null;

            return await _context.KitchenOwners.FirstOrDefaultAsync(o => o.Email == email);
        }

        // ============================================================
        // 1. DASHBOARD & ANALYTICS
        // ============================================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            var todayStart = DateTime.UtcNow.Date;

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.CustomerAccount)
                .Where(o => o.KitchenId == kitchen.Id)
                .OrderByDescending(o => o.PlacedAt)
                .ToListAsync();

            var todayOrders = orders.Where(o => o.PlacedAt >= todayStart).ToList();
            var todayRevenue = todayOrders.Where(o => o.Status != "Cancelled").Sum(o => o.Total);
            var totalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.Total);

            var pendingOrdersCount = orders.Count(o =>
                o.Status == "Order Placed" ||
                o.Status == "Order Accepted" ||
                o.Status == "Kitchen Preparing" ||
                o.Status == "Preparing" ||
                o.Status == "Out for Delivery");

            var completedOrdersCount = orders.Count(o => o.Status == "Delivered");

            var recentOrders = orders.Take(10).Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Customer = !string.IsNullOrWhiteSpace(o.CustomerAccount?.Name) && !o.CustomerAccount.Name.Contains("@")
                    ? o.CustomerAccount.Name.Trim()
                    : (!string.IsNullOrWhiteSpace(o.Customer) && !o.Customer.Contains("@")
                        ? o.Customer.Trim()
                        : (!string.IsNullOrWhiteSpace(o.CustomerAccount?.Name) ? o.CustomerAccount.Name.Trim() : (!string.IsNullOrWhiteSpace(o.Customer) ? o.Customer.Split('@')[0] : "Customer"))),
                Phone = o.Phone,
                Address = o.Address,
                Subtotal = o.Subtotal,
                DeliveryFee = o.DeliveryFee,
                Total = o.Total,
                PaymentMethod = o.PaymentMethod,
                PlacedAt = o.PlacedAt,
                AcceptedAt = o.AcceptedAt,
                Status = o.Status,
                KitchenId = o.KitchenId
            }).ToList();

            var reviews = await _context.OrderReviews
                .AsNoTracking()
                .Where(r => r.KitchenId == kitchen.Id)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new CustomerReviewDto
                {
                    Id = r.Id,
                    CustomerName = r.Customer != null ? r.Customer.Name : "Customer",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var totalReviews = await _context.OrderReviews.CountAsync(r => r.KitchenId == kitchen.Id);

            var dashboardDto = new SellerDashboardDto
            {
                TodayOrdersCount = todayOrders.Count,
                TodayRevenue = todayRevenue,
                TotalOrdersCount = orders.Count,
                TotalRevenue = totalRevenue,
                PendingOrdersCount = pendingOrdersCount,
                CompletedOrdersCount = completedOrdersCount,
                Rating = kitchen.Rating,
                TotalReviews = totalReviews,
                RecentOrders = recentOrders,
                RecentReviews = reviews
            };

            return Ok(new { success = true, dashboard = dashboardDto });
        }

        // ============================================================
        // REVIEWS - STRICT OWNERSHIP (LOGGED-IN SELLER'S KITCHEN ONLY)
        // ============================================================

        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews()
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null)
            {
                return NotFound(new { success = false, message = "Seller kitchen not found." });
            }

            var reviewEntities = await _context.OrderReviews
                .AsNoTracking()
                .Where(r => r.KitchenId == kitchen.Id)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var totalReviews = reviewEntities.Count;
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recent30Days = reviewEntities.Count(r => r.CreatedAt >= thirtyDaysAgo);
            var fiveStars = reviewEntities.Count(r => r.Rating == 5);
            var fourStars = reviewEntities.Count(r => r.Rating == 4);
            var threeStars = reviewEntities.Count(r => r.Rating == 3);
            var twoStars = reviewEntities.Count(r => r.Rating == 2);
            var oneStars = reviewEntities.Count(r => r.Rating == 1);

            var reviews = reviewEntities.Select(r => new CustomerReviewDto
            {
                Id = r.Id,
                CustomerName = r.Customer != null ? r.Customer.Name : "Customer",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

            var response = new SellerReviewsResponseDto
            {
                KitchenId = kitchen.Id,
                KitchenName = kitchen.Name,
                Summary = new SellerReviewsSummaryDto
                {
                    AverageRating = kitchen.Rating,
                    TotalReviews = totalReviews,
                    Recent30DaysCount = recent30Days,
                    FiveStarCount = fiveStars,
                    FourStarCount = fourStars,
                    ThreeStarCount = threeStars,
                    TwoStarCount = twoStars,
                    OneStarCount = oneStars
                },
                Reviews = reviews
            };

            return Ok(new { success = true, data = response });
        }

        // ============================================================
        // 2. KITCHEN MANAGEMENT
        // ============================================================

        [HttpGet("kitchen")]
        public async Task<IActionResult> GetKitchen()
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null)
            {
                return NotFound(new { success = false, message = "Kitchen not found." });
            }

            var dto = new Cloud_Kitchen_web_App.Api.DTOs.Responses.KitchenDto
            {
                Id = kitchen.Id,
                Name = kitchen.Name,
                Phone = kitchen.Phone,
                Description = kitchen.Description,
                Cuisine = kitchen.Cuisine,
                City = kitchen.City,
                Area = kitchen.Area,
                Address = kitchen.Address,
                Lat = kitchen.Lat,
                Lng = kitchen.Lng,
                Image = kitchen.Image,
                OpeningTime = kitchen.OpeningTime,
                ClosingTime = kitchen.ClosingTime,
                DeliveryRatePerKm = kitchen.DeliveryRatePerKm,
                DeliveryAreas = kitchen.DeliveryAreas,
                Rating = kitchen.Rating,
                DeliveryTime = kitchen.DeliveryTime,
                MinOrder = kitchen.MinOrder,
                IsOpen = kitchen.IsOpen,
                ApprovalStatus = kitchen.ApprovalStatus
            };

            return Ok(new { success = true, kitchen = dto });
        }

        [HttpPut("kitchen/settings")]
        public async Task<IActionResult> UpdateKitchenSettings([FromBody] KitchenSettingsUpdateRequest request)
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            kitchen.IsOpen = request.IsStoreOpen;
            kitchen.OpeningTime = request.OpenTime?.Trim() ?? string.Empty;
            kitchen.ClosingTime = request.CloseTime?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Kitchen settings updated successfully." });
        }

        [HttpPut("kitchen/profile")]
        public async Task<IActionResult> UpdateKitchenProfile([FromBody] KitchenProfileUpdateRequest request)
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            kitchen.Name = request.KitchenName?.Trim() ?? string.Empty;
            kitchen.Phone = request.KitchenPhone?.Trim() ?? string.Empty;
            kitchen.Description = request.Description?.Trim() ?? string.Empty;
            kitchen.Cuisine = request.Cuisine?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Kitchen profile updated successfully." });
        }

        // ============================================================
        // 3. SELLER OWNER PROFILE
        // ============================================================

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var owner = await GetSellerOwnerAsync();
            if (owner == null) return NotFound(new { success = false, message = "Seller profile not found." });

            var kitchen = await _context.Kitchens.FirstOrDefaultAsync(k => k.KitchenOwnerId == owner.Id);

            var dto = new SellerOwnerProfileDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email,
                Phone = owner.Phone,
                Address = owner.Address,
                KitchenName = kitchen?.Name ?? string.Empty,
                ApprovalStatus = kitchen?.ApprovalStatus ?? "Pending"
            };

            return Ok(new { success = true, profile = dto });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateSellerOwnerProfileRequest request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var owner = await _context.KitchenOwners.FirstOrDefaultAsync(o => o.Email == email);
            if (owner == null) return NotFound(new { success = false, message = "Seller profile not found." });

            owner.Name = request.Name?.Trim() ?? owner.Name;
            owner.Phone = request.Phone?.Trim() ?? owner.Phone;
            owner.Address = request.Address?.Trim() ?? owner.Address;

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.FullName = owner.Name;
                user.PhoneNumber = owner.Phone;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Seller profile updated successfully." });
        }

        // ============================================================
        // 4. MENU MANAGEMENT
        // ============================================================

        [HttpGet("menu")]
        public async Task<IActionResult> GetMenu()
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            var items = kitchen.MenuItems.OrderByDescending(m => m.Id).Select(m => new Cloud_Kitchen_web_App.Api.DTOs.Responses.MenuItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category,
                Description = m.Description,
                Price = m.Price,
                Available = m.Available,
                IsDeal = m.IsDeal,
                Image = m.Image,
                KitchenId = m.KitchenId
            }).ToList();

            return Ok(new { success = true, items });
        }

        [HttpPost("menu")]
        public async Task<IActionResult> AddMenuItem([FromForm] MenuItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            if (kitchen.ApprovalStatus != "Approved")
                return StatusCode(403, new { success = false, message = "Kitchen is not approved yet." });

            string imagePath = string.Empty;
            if (request.Image != null && request.Image.Length > 0)
            {
                imagePath = await SaveIFormFileAsync(request.Image, "menu");
            }
            else if (!string.IsNullOrEmpty(request.ImageBase64))
            {
                imagePath = await SaveImageFileAsync(request.ImageBase64, "menu");
            }

            var menuItem = new MenuItem
            {
                Name = request.Name.Trim(),
                Category = request.Category?.Trim() ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty,
                Price = request.Price,
                Available = request.Available,
                IsDeal = request.IsDeal,
                KitchenId = kitchen.Id,
                Image = imagePath
            };

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Menu item added.", itemId = menuItem.Id });
        }

        [HttpPut("menu/{id}")]
        public async Task<IActionResult> UpdateMenuItem(int id, [FromForm] MenuItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            if (kitchen.ApprovalStatus != "Approved")
                return StatusCode(403, new { success = false, message = "Kitchen is not approved yet." });

            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.KitchenId == kitchen.Id);
            if (menuItem == null) return NotFound(new { success = false, message = "Menu item not found or unauthorized." });

            menuItem.Name = request.Name.Trim();
            menuItem.Category = request.Category?.Trim() ?? string.Empty;
            menuItem.Description = request.Description?.Trim() ?? string.Empty;
            menuItem.Price = request.Price;
            menuItem.Available = request.Available;
            menuItem.IsDeal = request.IsDeal;

            if (request.Image != null && request.Image.Length > 0)
            {
                menuItem.Image = await SaveIFormFileAsync(request.Image, "menu");
            }
            else if (!string.IsNullOrEmpty(request.ImageBase64))
            {
                menuItem.Image = await SaveImageFileAsync(request.ImageBase64, "menu");
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Menu item updated." });
        }

        [HttpDelete("menu/{id}")]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            if (kitchen.ApprovalStatus != "Approved")
                return StatusCode(403, new { success = false, message = "Kitchen is not approved yet." });

            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.KitchenId == kitchen.Id);
            if (menuItem == null) return NotFound(new { success = false, message = "Menu item not found or unauthorized." });

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Menu item deleted." });
        }

        // ============================================================
        // 5. ORDERS & STATUS UPDATES
        // ============================================================

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.CustomerAccount)
                .Where(o => o.KitchenId == kitchen.Id)
                .OrderByDescending(o => o.PlacedAt)
                .ToListAsync();

            var dtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Customer = !string.IsNullOrWhiteSpace(o.CustomerAccount?.Name)
                    ? o.CustomerAccount.Name
                    : (!string.IsNullOrWhiteSpace(o.Customer) && !o.Customer.Contains("@") ? o.Customer : (o.CustomerAccount?.Name ?? o.Customer)),
                Phone = o.Phone,
                Address = o.Address,
                CustomerLatitude = o.CustomerLatitude,
                CustomerLongitude = o.CustomerLongitude,
                Subtotal = o.Subtotal,
                DeliveryFee = o.DeliveryFee,
                Total = o.Total,
                PaymentMethod = o.PaymentMethod,
                PlacedAt = o.PlacedAt,
                AcceptedAt = o.AcceptedAt,
                Status = o.Status,
                KitchenId = o.KitchenId,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Name = i.Name,
                    Qty = i.Qty,
                    Price = i.Price
                }).ToList()
            }).ToList();

            return Ok(new { success = true, orders = dtos });
        }

        [HttpPost("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            if (kitchen.ApprovalStatus != "Approved")
                return StatusCode(403, new { success = false, message = "Kitchen is not approved yet." });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.KitchenId == kitchen.Id);
            if (order == null) return NotFound(new { success = false, message = "Order not found or unauthorized." });

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                return BadRequest(new { success = false, message = "This order can no longer be modified." });
            }

            string? targetStatus = null;
            var reqStatus = (request.Status ?? "").Trim();

            if (reqStatus.Equals("Order Placed", StringComparison.OrdinalIgnoreCase) ||
                reqStatus.Equals("New", StringComparison.OrdinalIgnoreCase) ||
                reqStatus.Equals("New / Placed", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Order Placed";
            }
            else if (reqStatus.Equals("Order Accepted", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Order Accepted";
            }
            else if (reqStatus.Equals("Kitchen Preparing", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Preparing", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Kitchen Preparing";
            }
            else if (reqStatus.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Order Ready", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Ready";
            }
            else if (reqStatus.Equals("Out for Delivery", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Dispatch Order", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Out for Delivery";
            }
            else if (reqStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Order Delivered", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Delivered";
            }
            else if (reqStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Cancel Order", StringComparison.OrdinalIgnoreCase) ||
                     reqStatus.Equals("Order Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                targetStatus = "Cancelled";
            }

            if (targetStatus == null)
            {
                return BadRequest(new { success = false, message = $"Invalid order status '{request.Status}' provided." });
            }

            order.Status = targetStatus;

            if (targetStatus == "Order Accepted" && order.AcceptedAt == null)
            {
                order.AcceptedAt = DateTime.UtcNow;
            }

            // Notify customer
            var customerNotification = new Notification
            {
                Id = Guid.NewGuid().ToString("N"),
                RecipientType = "Customer",
                RecipientId = order.CustomerId,
                Title = $"Order Update - {kitchen.Name}",
                Body = $"Your order #{order.Id} from {kitchen.Name} is now {order.Status}.",
                Time = DateTime.UtcNow,
                Status = "Unread",
                OrderId = order.Id
            };
            _context.Notifications.Add(customerNotification);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, status = order.Status, message = $"Order status updated to {order.Status}." });
        }

        [HttpPost("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id, [FromBody] CancelSellerOrderRequest request)
        {
            var kitchen = await GetSellerKitchenAsync();
            if (kitchen == null) return NotFound(new { success = false, message = "Kitchen not found." });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.KitchenId == kitchen.Id);
            if (order == null) return NotFound(new { success = false, message = "Order not found." });

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                return BadRequest(new { success = false, message = "This order cannot be cancelled." });
            }

            order.Status = "Cancelled";

            string reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? "The kitchen was unable to fulfill your order."
                : request.Reason.Trim();

            var customerNotification = new Notification
            {
                Id = Guid.NewGuid().ToString("N"),
                RecipientType = "Customer",
                RecipientId = order.CustomerId,
                Title = $"Order Cancelled - {kitchen.Name}",
                Body = $"Your order #{order.Id} from {kitchen.Name} was cancelled. Reason: {reason}",
                Time = DateTime.UtcNow,
                Status = "Unread",
                OrderId = order.Id
            };
            _context.Notifications.Add(customerNotification);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order cancelled successfully." });
        }

        // ============================================================
        // 6. NOTIFICATIONS API
        // ============================================================

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var kitchenOwner = await _context.KitchenOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email);

            if (kitchenOwner == null)
            {
                return NotFound(new { success = false, message = "Seller information was not found." });
            }

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientType == "Seller" && n.RecipientId == kitchenOwner.Id)
                .OrderByDescending(n => n.Time)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Body,
                    Time = n.Time,
                    IsRead = n.Status == "Read",
                    RelatedOrderId = n.OrderId
                })
                .ToListAsync();

            return Ok(new { success = true, notifications });
        }

        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var kitchenOwner = await _context.KitchenOwners
                .FirstOrDefaultAsync(x => x.Email == email);

            if (kitchenOwner == null) return NotFound(new { success = false, message = "Seller not found." });

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientType == "Seller" && n.RecipientId == kitchenOwner.Id);

            if (notification == null) return NotFound(new { success = false, message = "Notification not found." });

            notification.Status = "Read";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notification marked as read." });
        }

        [HttpPost("notifications/read-all")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var kitchenOwner = await _context.KitchenOwners
                .FirstOrDefaultAsync(x => x.Email == email);

            if (kitchenOwner == null) return NotFound(new { success = false, message = "Seller not found." });

            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientType == "Seller" && n.RecipientId == kitchenOwner.Id && n.Status == "Unread")
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.Status = "Read";
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, count = unreadNotifications.Count, message = "All notifications marked as read." });
        }

        // ============================================================
        // 7. FILE HELPERS
        // ============================================================

        private async Task<string> SaveImageFileAsync(string base64Image, string folderName)
        {
            try
            {
                if (base64Image.Contains(","))
                {
                    base64Image = base64Image.Split(',')[1];
                }

                var bytes = Convert.FromBase64String(base64Image);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folderName);
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString("N") + ".jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                return $"/images/{folderName}/{fileName}";
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<string> SaveIFormFileAsync(IFormFile file, string folderName)
        {
            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folderName);
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/images/{folderName}/{fileName}";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
