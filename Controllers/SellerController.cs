using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace Cloud_Kitchen_web_App.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly CloudKitchenDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public SellerController(
            CloudKitchenDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        // ============================================================
        // SELLER DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> SellerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var kitchenOwner = await _context.KitchenOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                TempData["Error"] =
                    "Seller kitchen information was not found.";

                return View(new SellerDashboardViewModel
                {
                    SellerName =
                        user.FullName ??
                        user.Email ??
                        "Seller"
                });
            }

            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    k => k.KitchenOwnerId == kitchenOwner.Id);

            if (kitchen == null)
            {
                TempData["Error"] =
                    "Your kitchen has not been created yet.";

                return View(new SellerDashboardViewModel
                {
                    SellerName =
                        user.FullName ??
                        user.Email ??
                        "Seller"
                });
            }

            var totalOrders = await _context.Orders
                .CountAsync(
                    o => o.KitchenId == kitchen.Id);

            var weekStart =
                DateTime.UtcNow.Date.AddDays(-6);

            var ordersThisWeek = await _context.Orders
                .CountAsync(o =>
                    o.KitchenId == kitchen.Id &&
                    o.PlacedAt >= weekStart);

            var activeStatuses = new[]
            {
                "Order Accepted",
                "Kitchen Preparing",
                "Preparing",
                "Out for Delivery"
            };

            var activeOrders = await _context.Orders
                .CountAsync(o =>
                    o.KitchenId == kitchen.Id &&
                    activeStatuses.Contains(o.Status));

            var todayStart =
                DateTime.UtcNow.Date;

            var tomorrowStart =
                todayStart.AddDays(1);

            var todayEarnings =
                await _context.Orders
                    .Where(o =>
                        o.KitchenId == kitchen.Id &&
                        o.PlacedAt >= todayStart &&
                        o.PlacedAt < tomorrowStart &&
                        (
                            o.Status == "Delivered" ||
                            o.Status == "Order Delivered" ||
                            o.Status == "Completed"
                        ))
                    .SumAsync(o =>
                        (decimal?)o.Total) ?? 0m;

            var completedStatuses = new[]
            {
                "Delivered",
                "Order Delivered",
                "Completed"
            };

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.CustomerAccount)
                .Where(o =>
                    o.KitchenId == kitchen.Id &&
                    completedStatuses.Contains(o.Status))
                .OrderByDescending(o => o.PlacedAt)
                .Take(10)
                .ToListAsync();

            var totalReviewsCount = await _context.OrderReviews
                .CountAsync(r => r.KitchenId == kitchen.Id);

            var vm = new SellerDashboardViewModel
            {
                SellerName =
                    user.FullName ??
                    user.Email ??
                    "Seller",

                KitchenName = kitchen.Name,

                StoreStatus =
                    kitchen.IsOpen
                        ? "Open"
                        : "Closed",

                TotalOrders = totalOrders,

                OrdersThisWeek = ordersThisWeek,

                ActiveOrders = activeOrders,

                TodayEarnings = todayEarnings,

                Rating = kitchen.Rating,

                TotalReviews = totalReviewsCount,

                RecentOrders = recentOrders
            };

            return View(vm);
        }


        // ============================================================
        // MENU MANAGEMENT - READ
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MenuManage()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var kitchenOwner = await _context.KitchenOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                TempData["Error"] =
                    "Seller kitchen information was not found.";

                return View(new MenuViewModel());
            }

            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .Include(k => k.MenuItems)
                .FirstOrDefaultAsync(
                    k => k.KitchenOwnerId == kitchenOwner.Id);

            if (kitchen == null)
            {
                TempData["Error"] =
                    "Your kitchen has not been created yet.";

                return View(new MenuViewModel());
            }

            var vm = new MenuViewModel
            {
                KitchenName = kitchen.Name,

                Items = kitchen.MenuItems
                    .OrderByDescending(x => x.Id)
                    .ToList()
            };

            return View(vm);
        }


        // ============================================================
        // CREATE MENU ITEM
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(
            [FromForm] MenuItemInputViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Please provide valid menu item information."
                });
            }

            var kitchen =
                await GetCurrentSellerKitchenAsync();

            if (kitchen == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Seller kitchen information was not found."
                });
            }

            if (!IsKitchenApproved(kitchen))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message =
                            "Your kitchen has not been approved yet."
                    });
            }

            var menuItem = new MenuItem
            {
                Name = model.Name.Trim(),

                Category =
                    model.Category?.Trim() ??
                    string.Empty,

                Description =
                    model.Description?.Trim() ??
                    string.Empty,

                Price = model.Price,

                Available = model.Available,

                IsDeal = model.IsDeal,

                KitchenId = kitchen.Id
            };

            if (model.Image != null)
            {
                var imagePath =
                    await SaveMenuImageAsync(model.Image);

                if (imagePath == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Invalid image. Please upload a valid JPG, PNG or WebP image up to 5 MB."
                    });
                }

                menuItem.Image = imagePath;
            }

            _context.MenuItems.Add(menuItem);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message =
                    "Menu item added successfully.",
                itemId = menuItem.Id
            });
        }


        // ============================================================
        // UPDATE MENU ITEM
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMenuItem(
            [FromForm] MenuItemInputViewModel model)
        {
            if (model.Id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid menu item."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Please provide valid menu item information."
                });
            }

            var kitchen =
                await GetCurrentSellerKitchenAsync();

            if (kitchen == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Seller kitchen information was not found."
                });
            }

            if (!IsKitchenApproved(kitchen))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message =
                            "Your kitchen has not been approved yet."
                    });
            }

            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    x.KitchenId == kitchen.Id);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Menu item was not found."
                });
            }

            menuItem.Name =
                model.Name.Trim();

            menuItem.Category =
                model.Category?.Trim() ??
                string.Empty;

            menuItem.Description =
                model.Description?.Trim() ??
                string.Empty;

            menuItem.Price =
                model.Price;

            menuItem.Available =
                model.Available;

            menuItem.IsDeal =
                model.IsDeal;

            if (model.Image != null)
            {
                var newImagePath =
                    await SaveMenuImageAsync(
                        model.Image);

                if (newImagePath == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Invalid image. Please upload a valid JPG, PNG or WebP image up to 5 MB."
                    });
                }

                var oldImagePath =
                    menuItem.Image;

                menuItem.Image =
                    newImagePath;

                await _context.SaveChangesAsync();

                DeleteMenuImage(oldImagePath);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message =
                    "Menu item updated successfully."
            });
        }


        // ============================================================
        // DELETE MENU ITEM
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItem(
            [FromBody] DeleteMenuItemRequest request)
        {
            if (request == null ||
                request.Id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Invalid menu item."
                });
            }

            var kitchen =
                await GetCurrentSellerKitchenAsync();

            if (kitchen == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Seller kitchen information was not found."
                });
            }

            if (!IsKitchenApproved(kitchen))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message =
                            "Your kitchen has not been approved yet."
                    });
            }

            var menuItem =
                await _context.MenuItems
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.Id &&
                        x.KitchenId == kitchen.Id);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Menu item was not found."
                });
            }

            var imagePath =
                menuItem.Image;

            _context.MenuItems.Remove(menuItem);

            await _context.SaveChangesAsync();

            DeleteMenuImage(imagePath);

            return Ok(new
            {
                success = true,
                message =
                    "Menu item deleted successfully."
            });
        }


        // ============================================================
        // TOGGLE MENU ITEM AVAILABILITY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ToggleMenuItemAvailability(
                [FromBody] ToggleMenuItemRequest request)
        {
            if (request == null ||
                request.Id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Invalid menu item."
                });
            }

            var kitchen =
                await GetCurrentSellerKitchenAsync();

            if (kitchen == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Seller kitchen information was not found."
                });
            }

            if (!IsKitchenApproved(kitchen))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message =
                            "Your kitchen has not been approved yet."
                    });
            }

            var menuItem =
                await _context.MenuItems
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.Id &&
                        x.KitchenId == kitchen.Id);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Menu item was not found."
                });
            }

            menuItem.Available =
                request.Available;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,

                available =
                    menuItem.Available,

                message =
                    menuItem.Available
                        ? "Menu item is now available."
                        : "Menu item is now unavailable."
            });
        }


        // ============================================================
        // UPDATE ORDER STATUS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(
            [FromBody] SellerOrderRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.OrderId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid order."
                });
            }

            var kitchen =
                await GetCurrentSellerKitchenAsync();

            if (kitchen == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Seller kitchen information was not found."
                });
            }

            if (!IsKitchenApproved(kitchen))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message =
                            "Your kitchen has not been approved yet."
                    });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == request.OrderId &&
                    o.KitchenId == kitchen.Id);

            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Order not found."
                });
            }
            var previousStatus = order.Status;

            if (!string.IsNullOrWhiteSpace(request.NewStatus))
            {
                string? targetStatus = null;
                var reqStatus = request.NewStatus.Trim();

                if (reqStatus.Equals("Order Placed", StringComparison.OrdinalIgnoreCase) ||
                    reqStatus.Equals("New", StringComparison.OrdinalIgnoreCase))
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
                         reqStatus.Equals("Dispatched", StringComparison.OrdinalIgnoreCase))
                {
                    targetStatus = "Out for Delivery";
                }
                else if (reqStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
                         reqStatus.Equals("Order Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    targetStatus = "Delivered";
                }
                else if (reqStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                         reqStatus.Equals("Order Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    targetStatus = "Cancelled";
                }

                if (targetStatus == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid order status requested."
                    });
                }

                if ((order.Status == "Delivered" || order.Status == "Cancelled") &&
                    !string.Equals(order.Status, targetStatus, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "This order is finalized and can no longer be updated."
                    });
                }

                if (targetStatus == "Order Accepted" && !order.AcceptedAt.HasValue)
                {
                    order.AcceptedAt = DateTime.UtcNow;
                }

                order.Status = targetStatus;
            }
            else
            {
                switch (order.Status)
                {
                    case "Order Placed":
                        order.Status = "Order Accepted";
                        order.AcceptedAt = DateTime.UtcNow;
                        break;

                    case "Order Accepted":
                        order.Status = "Kitchen Preparing";
                        break;

                    case "Kitchen Preparing":
                    case "Preparing":
                        order.Status = "Ready";
                        break;

                    case "Ready":
                        order.Status = "Out for Delivery";
                        break;

                    case "Out for Delivery":
                        order.Status = "Delivered";
                        break;

                    case "Delivered":
                    case "Cancelled":
                        return BadRequest(new
                        {
                            success = false,
                            message = "This order can no longer be advanced."
                        });

                    default:
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid order status."
                        });
                }
            }
             
            CreateCustomerOrderStatusNotification(
                order,
                previousStatus);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                status = order.Status,
                message = $"Order status updated to {order.Status}."
            });
        }


        // ============================================================
        // CANCEL ORDER
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(
            [FromBody] SellerOrderRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.OrderId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid order."
                });
            }

            var kitchen =
                await GetCurrentSellerKitchenAsync();

            if (kitchen == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message =
                        "Seller kitchen information was not found."
                });
            }

            if (!IsKitchenApproved(kitchen))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        success = false,
                        message =
                            "Your kitchen has not been approved yet."
                    });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == request.OrderId &&
                    o.KitchenId == kitchen.Id);

            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Order not found."
                });
            }

            var cancellableStatuses = new[]
            {
                "Order Placed",
                "Order Accepted",
                "Kitchen Preparing",
                "Preparing"
            };

            if (!cancellableStatuses.Contains(
                    order.Status))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This order can no longer be cancelled."
                });
            }

            order.Status =
                "Cancelled";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,

                status =
                    order.Status,

                message =
                    "Order cancelled successfully."
            });
        }


        // ============================================================
        // MANAGE ORDERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ManageOrders()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                TempData["Error"] =
                    "Seller kitchen information was not found.";

                return View(
                    new OrdersViewModel());
            }

            var kitchen =
                await _context.Kitchens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        k =>
                            k.KitchenOwnerId ==
                            kitchenOwner.Id);

            if (kitchen == null)
            {
                TempData["Error"] =
                    "Your kitchen has not been created yet.";

                return View(
                    new OrdersViewModel());
            }

            var orders =
                await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Kitchen)
                    .Include(o => o.Items)
                    .Include(o => o.CustomerAccount)
                    .Where(o =>
                        o.KitchenId == kitchen.Id)
                    .OrderByDescending(
                        o => o.PlacedAt)
                    .ToListAsync();

            var vm = new OrdersViewModel
            {
                SellerName =
                    user.FullName ??
                    user.Email ??
                    "Seller",

                KitchenName =
                    kitchen.Name,

                StoreStatus =
                    kitchen.IsOpen
                        ? "Open"
                        : "Closed",

                Orders =
                    orders
            };

            return View(vm);
        }


        // ============================================================
        // SELLER NOTIFICATIONS
        // ============================================================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Notification()
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                TempData["Error"] =
                    "Seller information was not found.";

                return View(
                    new NotificationViewModel());
            }

            // --------------------------------------------------------
            // LOAD ONLY THIS SELLER'S NOTIFICATIONS
            // --------------------------------------------------------
            //
            // RecipientType = Seller
            // RecipientId   = KitchenOwner.Id
            //
            // This prevents one seller from seeing another seller's
            // notifications.
            // --------------------------------------------------------

            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KitchenOwnerId == kitchenOwner.Id);

            var notifications =
                await _context.Notifications
                    .AsNoTracking()
                    .Where(n =>
                        n.RecipientType == "Seller" &&
                        (n.RecipientId == kitchenOwner.Id || (kitchen != null && n.RecipientId == kitchen.Id)))
                    .OrderByDescending(n => n.Time)
                    .ToListAsync();

            var vm =
                new NotificationViewModel
                {
                    Notifications = notifications
                };

            return View(vm);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Notifications()
        {
            return await Notification();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetSellerUnreadNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });

            var kitchenOwner = await _context.KitchenOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == user.Email);

            if (kitchenOwner == null) return Json(new { count = 0 });

            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KitchenOwnerId == kitchenOwner.Id);

            var count = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n =>
                    n.RecipientType == "Seller" &&
                    (n.RecipientId == kitchenOwner.Id || (kitchen != null && n.RecipientId == kitchen.Id)) &&
                    n.Status != "Read");

            return Json(new { count = count });
        }
        // ============================================================
        // MARK SELLER NOTIFICATION AS READ
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSellerNotificationAsRead(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Notification ID is required."
                });
            }

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Seller is not authenticated."
                });
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Seller information was not found."
                });
            }

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.Id == id &&
                        n.RecipientType == "Seller" &&
                        n.RecipientId == kitchenOwner.Id);

            if (notification == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Notification not found."
                });
            }

            notification.Status = "Read";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                notificationId = notification.Id
            });
        }


        // ============================================================
        // MARK ALL SELLER NOTIFICATIONS AS READ
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllSellerNotificationsAsRead()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Seller is not authenticated."
                });
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Seller information was not found."
                });
            }

            var unreadNotifications =
                await _context.Notifications
                    .Where(n =>
                        n.RecipientType == "Seller" &&
                        n.RecipientId == kitchenOwner.Id &&
                        n.Status != "Read")
                    .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.Status = "Read";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                count = unreadNotifications.Count
            });
        }

        // ============================================================
        // SELLER PROFILE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> SellerProfile()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                TempData["Error"] =
                    "Seller profile was not found.";

                return View(
                    new SellerProfileViewModel());
            }

            var kitchen =
                await _context.Kitchens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        k =>
                            k.KitchenOwnerId ==
                            kitchenOwner.Id);

            var vm =
                new SellerProfileViewModel
                {
                    Name =
                        user.FullName ?? "",

                    Shop =
                        kitchen?.Name ?? "",

                    Phone =
                        user.PhoneNumber ?? "",

                    Email =
                        user.Email ?? "",

                    Address =
                        kitchen?.Address ?? "",

                    Photo =
                        "/images/profile.png"
                };

            return View(vm);
        }


        // ============================================================
        // SELLER SETTINGS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Setting()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Email == user.Email);

            Kitchen? kitchen = null;

            if (kitchenOwner != null)
            {
                kitchen =
                    await _context.Kitchens
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            k =>
                                k.KitchenOwnerId ==
                                kitchenOwner.Id);
            }

            var vm =
                new SettingViewModel
                {
                    SellerName =
                        user.FullName ??
                        user.Email ??
                        "Seller",

                    KitchenName =
                        kitchen?.Name ?? "",

                    StoreOpen = kitchen?.IsOpen ?? true,
                    OpenTime = !string.IsNullOrWhiteSpace(kitchen?.OpeningTime) ? kitchen.OpeningTime : "09:00",
                    CloseTime = !string.IsNullOrWhiteSpace(kitchen?.ClosingTime) ? kitchen.ClosingTime : "22:00",
                    DeliveryRatePerKm = kitchen?.DeliveryRatePerKm > 0 ? kitchen.DeliveryRatePerKm : 30m,
                    City = kitchen?.City ?? "",
                    Area = kitchen?.Area ?? "",
                    Lat = kitchen?.Lat ?? 0,
                    Lng = kitchen?.Lng ?? 0,

                    NotificationsEnabled =
                        true
                };

            return View(vm);
        }


        // ============================================================
        // UPDATE SELLER SETTINGS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSellerSettings([FromBody] SellerSettingsRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request payload." });
            }

            var kitchen = await GetCurrentSellerKitchenAsync();
            if (kitchen == null)
            {
                return NotFound(new { message = "Kitchen profile not found." });
            }

            kitchen.IsOpen = request.StoreOpen;
            if (!string.IsNullOrWhiteSpace(request.OpenTime))
            {
                kitchen.OpeningTime = request.OpenTime.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.CloseTime))
            {
                kitchen.ClosingTime = request.CloseTime.Trim();
            }
            if (request.DeliveryRatePerKm > 0)
            {
                kitchen.DeliveryRatePerKm = Math.Round(request.DeliveryRatePerKm, 2);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Settings updated successfully." });
        }

        public class SellerSettingsRequest
        {
            public bool StoreOpen { get; set; }
            public string? OpenTime { get; set; }
            public string? CloseTime { get; set; }
            public decimal DeliveryRatePerKm { get; set; }
        }


        // ============================================================
        // PRIVATE: CURRENT SELLER KITCHEN
        // ============================================================

        private async Task<Kitchen?>
            GetCurrentSellerKitchenAsync()
        {
            var user =
                await _userManager
                    .GetUserAsync(User);

            if (user == null ||
                string.IsNullOrWhiteSpace(
                    user.Email))
            {
                return null;
            }

            var kitchenOwner =
                await _context.KitchenOwners
                    .FirstOrDefaultAsync(
                        x =>
                            x.Email ==
                            user.Email);

            if (kitchenOwner == null)
            {
                return null;
            }

            return await _context.Kitchens
                .FirstOrDefaultAsync(
                    x =>
                        x.KitchenOwnerId ==
                        kitchenOwner.Id);
        }


        // ============================================================
        // PRIVATE: KITCHEN APPROVAL
        // ============================================================

        private static bool
            IsKitchenApproved(Kitchen kitchen)
        {
            return string.Equals(
                kitchen.ApprovalStatus,
                "Approved",
                StringComparison.OrdinalIgnoreCase);
        }


        // ============================================================
        // PRIVATE: SAVE MENU IMAGE
        // ============================================================

        private async Task<string?>
            SaveMenuImageAsync(IFormFile image)
        {
            const long maxFileSize =
                5 * 1024 * 1024;

            if (image.Length <= 0 ||
                image.Length > maxFileSize)
            {
                return null;
            }

            var allowedContentTypes =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg",
                    "image/png",
                    "image/webp"
                };

            if (!allowedContentTypes.Contains(
                    image.ContentType))
            {
                return null;
            }

            var extension =
                Path.GetExtension(
                    image.FileName)
                    .ToLowerInvariant();

            var allowedExtensions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

            if (!allowedExtensions.Contains(
                    extension))
            {
                return null;
            }

            if (!await HasValidImageSignatureAsync(
                    image,
                    extension))
            {
                return null;
            }

            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "menu");

            Directory.CreateDirectory(
                uploadsFolder);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var physicalPath =
                Path.Combine(
                    uploadsFolder,
                    fileName);

            await using var stream =
                new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await image.CopyToAsync(stream);

            return
                $"/uploads/menu/{fileName}";
        }
        // ============================================================
        // CUSTOMER ORDER STATUS NOTIFICATION
        // ============================================================

        private void CreateCustomerOrderStatusNotification(
            Order order,
            string previousStatus)
        {
            if (order == null)
            {
                return;
            }

            if (string.Equals(
                    previousStatus,
                    order.Status,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var kitchenName = order.Kitchen?.Name ?? _context.Kitchens.Where(k => k.Id == order.KitchenId).Select(k => k.Name).FirstOrDefault() ?? "the kitchen";

            string title;
            string body;

            switch (order.Status)
            {
                case "Order Accepted":
                case "Accepted":
                    title = $"Order Accepted - {kitchenName}";
                    body = $"Your order #{order.Id} from {kitchenName} has been accepted.";
                    break;

                case "Kitchen Preparing":
                case "Preparing":
                    title = $"Kitchen Preparing - {kitchenName}";
                    body = $"Your order #{order.Id} is now being prepared by {kitchenName}.";
                    break;

                case "Ready":
                case "Order Ready":
                    title = $"Order Ready - {kitchenName}";
                    body = $"Your order #{order.Id} from {kitchenName} is prepared and ready for delivery.";
                    break;

                case "Out for Delivery":
                case "Dispatched":
                    title = $"Out for Delivery - {kitchenName}";
                    body = $"Your order #{order.Id} from {kitchenName} is now out for delivery.";
                    break;

                case "Delivered":
                case "Order Delivered":
                    title = $"Order Delivered - {kitchenName}";
                    body = $"Your order #{order.Id} from {kitchenName} has been delivered successfully. Enjoy your meal!";
                    break;

                case "Cancelled":
                case "Order Cancelled":
                    title = $"Order Cancelled - {kitchenName}";
                    body = $"Your order #{order.Id} has been cancelled by {kitchenName}.";
                    break;

                default:
                    title = $"Order {order.Status} - {kitchenName}";
                    body = $"Your order #{order.Id} from {kitchenName} status has been updated to {order.Status}.";
                    break;
            }

            int? customerId = null;
            if (order.CustomerId > 0)
            {
                customerId = order.CustomerId;
            }
            else if (!string.IsNullOrWhiteSpace(order.Customer))
            {
                customerId = _context.Customers
                    .Where(c => c.Email == order.Customer)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefault();
            }

            if (!customerId.HasValue)
            {
                return;
            }

            var notification =
                new Notification
                {
                    Id =
                        Guid.NewGuid()
                            .ToString("N"),

                    RecipientType =
                        "Customer",

                    RecipientId =
                        customerId.Value,

                    Title =
                        title,

                    Body =
                        body,

                    Time =
                        DateTime.UtcNow,

                    Status =
                        "Unread",

                    Link =
                        $"/Customer/OrderStatus?id={order.Id}",

                    OrderId =
                        order.Id
                };

            _context.Notifications.Add(notification);
        }

        // ============================================================
        // PRIVATE: IMAGE SIGNATURE VALIDATION
        // ============================================================

        private static async Task<bool>
            HasValidImageSignatureAsync(
                IFormFile image,
                string extension)
        {
            var buffer =
                new byte[16];

            await using var stream =
                image.OpenReadStream();

            var bytesRead =
                await stream.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length));

            if (extension is ".jpg" or ".jpeg")
            {
                return
                    bytesRead >= 3 &&
                    buffer[0] == 0xFF &&
                    buffer[1] == 0xD8 &&
                    buffer[2] == 0xFF;
            }

            if (extension == ".png")
            {
                return
                    bytesRead >= 8 &&
                    buffer[0] == 0x89 &&
                    buffer[1] == 0x50 &&
                    buffer[2] == 0x4E &&
                    buffer[3] == 0x47 &&
                    buffer[4] == 0x0D &&
                    buffer[5] == 0x0A &&
                    buffer[6] == 0x1A &&
                    buffer[7] == 0x0A;
            }

            if (extension == ".webp")
            {
                return
                    bytesRead >= 12 &&
                    buffer[0] == (byte)'R' &&
                    buffer[1] == (byte)'I' &&
                    buffer[2] == (byte)'F' &&
                    buffer[3] == (byte)'F' &&
                    buffer[8] == (byte)'W' &&
                    buffer[9] == (byte)'E' &&
                    buffer[10] == (byte)'B' &&
                    buffer[11] == (byte)'P';
            }

            return false;
        }


        // ============================================================
        // PRIVATE: DELETE MENU IMAGE
        // ============================================================

        private void DeleteMenuImage(
            string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(
                    imagePath))
            {
                return;
            }

            if (!imagePath.StartsWith(
                    "/uploads/menu/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName =
                Path.GetFileName(
                    imagePath);

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                return;
            }

            var physicalPath =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "menu",
                    fileName);

            if (System.IO.File.Exists(
                    physicalPath))
            {
                System.IO.File.Delete(
                    physicalPath);
            }
        }

        // ============================================================
        // SELLER REVIEWS - STRICT OWNERSHIP CHECK
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var kitchenOwner = await _context.KitchenOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == user.Email);

            if (kitchenOwner == null)
            {
                TempData["Error"] = "Seller kitchen information was not found.";
                return RedirectToAction(nameof(SellerDashboard));
            }

            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KitchenOwnerId == kitchenOwner.Id);

            if (kitchen == null)
            {
                TempData["Error"] = "Kitchen was not found.";
                return RedirectToAction(nameof(SellerDashboard));
            }

            var reviewList = await _context.OrderReviews
                .AsNoTracking()
                .Where(r => r.KitchenId == kitchen.Id)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var totalReviews = reviewList.Count;
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recent30DaysCount = reviewList.Count(r => r.CreatedAt >= thirtyDaysAgo);
            var fiveStars = reviewList.Count(r => r.Rating == 5);
            var fourStars = reviewList.Count(r => r.Rating == 4);
            var threeStars = reviewList.Count(r => r.Rating == 3);
            var twoStars = reviewList.Count(r => r.Rating == 2);
            var oneStars = reviewList.Count(r => r.Rating == 1);

            var reviews = reviewList.Select(r => new SellerReviewItemViewModel
            {
                Id = r.Id,
                OrderId = r.OrderId,
                CustomerFirstName = !string.IsNullOrWhiteSpace(r.Customer?.Name)
                    ? r.Customer.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
                    : "Customer",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

            var vm = new SellerReviewsViewModel
            {
                KitchenId = kitchen.Id,
                KitchenName = kitchen.Name,
                AverageRating = kitchen.Rating,
                TotalReviews = totalReviews,
                Recent30DaysReviewsCount = recent30DaysCount,
                FiveStarCount = fiveStars,
                FourStarCount = fourStars,
                ThreeStarCount = threeStars,
                TwoStarCount = twoStars,
                OneStarCount = oneStars,
                Reviews = reviews
            };

            return View(vm);
        }
    }


    // ================================================================
    // SELLER REQUEST MODELS
    // ================================================================

    public class DeleteMenuItemRequest
    {
        public int Id { get; set; }
    }


    public class ToggleMenuItemRequest
    {
        public int Id { get; set; }

        public bool Available { get; set; }
    }


    public class SellerOrderRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string? NewStatus { get; set; }
    }
}