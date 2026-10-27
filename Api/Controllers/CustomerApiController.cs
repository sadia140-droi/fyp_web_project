using CloudKitchen.Data;
using Cloud_Kitchen_web_App.Api.DTOs;
using Cloud_Kitchen_web_App.Api.Services;
using Cloud_Kitchen_web_App.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Api.Controllers
{
    [Route("api/customer")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Customer")]
    public class CustomerApiController : ControllerBase
    {
        private readonly CloudKitchenDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerApiController(
            CloudKitchenDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetCustomerEmail()
        {
            return User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? string.Empty;
        }

        private async Task<int?> GetCustomerIdAsync()
        {
            var email = GetCustomerEmail();
            if (string.IsNullOrEmpty(email)) return null;

            return await _context.Customers
                .Where(c => c.Email == email)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        // ============================================================
        // 1. KITCHENS API
        // ============================================================

        [AllowAnonymous]
        [HttpGet("kitchens")]
        public async Task<IActionResult> GetKitchens(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double radiusKm = 30,
            [FromQuery] string? city = null,
            [FromQuery] string? area = null)
        {
            var query = _context.Kitchens
                .AsNoTracking()
                .Include(k => k.MenuItems).Where(k => k.IsOpen && k.ApprovalStatus == "Approved");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(k =>
                    k.Name.ToLower().Contains(s) ||
                    k.City.ToLower().Contains(s) ||
                    k.Area.ToLower().Contains(s) ||
                    k.Cuisine.ToLower().Contains(s) ||
                    k.MenuItems.Any(m => m.Available && (m.Name.ToLower().Contains(s) || m.Category.ToLower().Contains(s))));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var c = category.Trim().ToLower();
                query = query.Where(k => k.MenuItems.Any(m => m.Available && m.Category.ToLower() == c));
            }

            // City filter: if a specific city is selected (and not 'All Cities'), match city or include 'TBD' / empty
            if (!string.IsNullOrWhiteSpace(city) && !city.Equals("All Cities", StringComparison.OrdinalIgnoreCase) && !city.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var cLower = city.Trim().ToLower();
                query = query.Where(k => k.City.ToLower() == cLower || k.City.ToLower() == "tbd" || string.IsNullOrEmpty(k.City));
            }

            if (!string.IsNullOrWhiteSpace(area) && !area.StartsWith("All Areas", StringComparison.OrdinalIgnoreCase))
            {
                var aLower = area.Trim().ToLower();
                query = query.Where(k => k.Area.ToLower().Contains(aLower) || k.Area.ToLower() == "tbd" || string.IsNullOrEmpty(k.Area));
            }

            var kitchens = await query.ToListAsync();
            var customerId = await GetCustomerIdAsync();
            var favoriteKitchenIds = new HashSet<int>();
            if (customerId.HasValue)
            {
                favoriteKitchenIds = new HashSet<int>(await _context.FavoriteKitchens
                    .AsNoTracking()
                    .Where(f => f.CustomerId == customerId.Value)
                    .Select(f => f.KitchenId)
                    .ToListAsync());
            }

            var orderService = new OrderService(_context);

            var result = kitchens.Select(k => {
                decimal distance = 0;
                bool hasCoordinates = (k.Lat != 0 || k.Lng != 0);

                if (lat.HasValue && lng.HasValue && hasCoordinates)
                {
                    distance = orderService.CalculateDistanceKm(lat.Value, lng.Value, k.Lat, k.Lng);
                }
                else
                {
                    distance = 1.8m;
                }

                // Live address resolution: if Address is missing or 'TBD', fall back to Area & City
                string displayAddr = !string.IsNullOrWhiteSpace(k.Address) && k.Address != "TBD"
                    ? k.Address
                    : (!string.IsNullOrWhiteSpace(k.Area) ? $"{k.Area}, {k.City}" : (!string.IsNullOrWhiteSpace(k.City) ? k.City : "Pakistan"));

                // Live calculated delivery time based on distance
                string calculatedDeliveryTime = !string.IsNullOrWhiteSpace(k.DeliveryTime) && k.DeliveryTime != "25-35 mins" && k.DeliveryTime != "30 min"
                    ? k.DeliveryTime
                    : (distance > 0 ? $"{20 + (int)Math.Round(distance * 2.5m)}-{30 + (int)Math.Round(distance * 2.5m)} mins" : "25-35 mins");

                return new KitchenDto
                {
                    Id = k.Id,
                    Name = k.Name,
                    Image = k.Image,
                    Rating = k.Rating,
                    DeliveryTime = calculatedDeliveryTime,
                    MinOrder = k.MinOrder,
                    DeliveryRatePerKm = k.DeliveryRatePerKm > 0 ? k.DeliveryRatePerKm : 30m,
                    DistanceKm = distance,
                    Address = displayAddr,
                    City = k.City,
                    Area = k.Area,
                    Cuisine = !string.IsNullOrWhiteSpace(k.Cuisine) && k.Cuisine != "TBD" ? k.Cuisine : "Authentic Homemade",
                    Description = k.Description,
                    Lat = k.Lat,
                    Lng = k.Lng,
                    IsOpen = k.IsOpen,
                    OpeningTime = k.OpeningTime,
                    ClosingTime = k.ClosingTime,
                    IsFavorite = favoriteKitchenIds.Contains(k.Id),
                    Categories = k.MenuItems != null && k.MenuItems.Any()
                        ? k.MenuItems.Where(m => m.Available && !string.IsNullOrEmpty(m.Category)).Select(m => m.Category).Distinct().ToList()
                        : new List<string>()
                };
            }).ToList();

            // Distance filtering ONLY filters out kitchens that have actual GPS coordinates
            if (lat.HasValue && lng.HasValue)
            {
                result = result.Where(k => {
                    var kitchenDb = kitchens.FirstOrDefault(item => item.Id == k.Id);
                    if (kitchenDb != null && (kitchenDb.Lat != 0 || kitchenDb.Lng != 0))
                    {
                        return (double)k.DistanceKm <= radiusKm;
                    }
                    return true;
                }).ToList();
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categoryNames = await _context.MenuItems
                .AsNoTracking()
                .Where(m => m.Available && m.Kitchen.IsOpen && m.Kitchen.ApprovalStatus == "Approved" && !string.IsNullOrWhiteSpace(m.Category))
                .Select(m => m.Category.Trim())
                .Distinct()
                .ToListAsync();

            var adminCategories = await _context.AdminCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync();

            var categories = adminCategories.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                imagePath = string.IsNullOrWhiteSpace(c.ImagePath) ? "/images/14.JPG" : c.ImagePath,
                hasActiveKitchens = categoryNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase)
            }).OrderBy(c => c.name).ToList();

            return Ok(categories);
        }

        [AllowAnonymous]
        [HttpGet("deals")]
        public async Task<IActionResult> GetDeals()
        {
            var deals = await _context.MenuItems
                .AsNoTracking()
                .Include(m => m.Kitchen)
                .Where(m => m.IsDeal && m.Available && m.Kitchen.IsOpen && m.Kitchen.ApprovalStatus == "Approved")
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    category = m.Category,
                    description = m.Description,
                    price = m.Price,
                    image = m.Image,
                    kitchenId = m.KitchenId,
                    kitchenName = m.Kitchen.Name
                })
                .ToListAsync();

            return Ok(deals);
        }

        // ============================================================
        // FETCH SINGLE KITCHEN DETAILS (FOR MOBILE & WEB APPS)
        // ============================================================
        [AllowAnonymous]
        [HttpGet("kitchens/{id}")]
        public async Task<IActionResult> GetKitchenDetails(int id, [FromQuery] double? lat, [FromQuery] double? lng)
        {
            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .Include(k => k.MenuItems)
                .FirstOrDefaultAsync(k => k.Id == id && k.ApprovalStatus == "Approved");

            if (kitchen == null)
            {
                return NotFound(new { message = "Kitchen not found or not approved." });
            }

            var customerId = await GetCustomerIdAsync();
            bool isFavorite = false;
            if (customerId.HasValue)
            {
                isFavorite = await _context.FavoriteKitchens
                    .AnyAsync(f => f.CustomerId == customerId.Value && f.KitchenId == id);
            }

            decimal distance = 0;
            if (lat.HasValue && lng.HasValue)
            {
                var orderService = new OrderService(_context);
                distance = orderService.CalculateDistanceKm(lat.Value, lng.Value, kitchen.Lat, kitchen.Lng);
            }

            // Reviews
            var reviews = await _context.OrderReviews
                .AsNoTracking()
                .Where(r => r.KitchenId == id)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .Select(r => new CustomerReviewDto
                {
                    Id = r.Id,
                    CustomerName = r.Customer != null ? r.Customer.Name : "Customer",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var totalReviews = await _context.OrderReviews.CountAsync(r => r.KitchenId == id);

            // Active menu items
            var menuItems = kitchen.MenuItems
                .Where(m => m.Available)
                .Select(m => new MenuItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Category = m.Category,
                    Description = m.Description,
                    Price = m.Price,
                    IsDeal = m.IsDeal,
                    Image = m.Image
                }).ToList();

            var categories = menuItems
                .Select(m => m.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dto = new KitchenDetailDto
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
                DistanceKm = distance,
                IsFavorite = isFavorite,
                ReviewCount = totalReviews,
                Categories = categories,
                Reviews = reviews,
                MenuItems = menuItems
            };

            return Ok(dto);
        }

        // ============================================================
        // GET KITCHEN REVIEWS (PUBLIC - APPROVED KITCHEN ONLY)
        // ============================================================

        [AllowAnonymous]
        [HttpGet("kitchens/{id:int}/reviews")]
        public async Task<IActionResult> GetKitchenReviews(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid kitchen ID." });
            }

            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == id && k.ApprovalStatus == "Approved");

            if (kitchen == null)
            {
                return NotFound(new { success = false, message = "Kitchen not found or not approved." });
            }

            var reviewEntities = await _context.OrderReviews
                .AsNoTracking()
                .Where(r => r.KitchenId == id)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var totalReviews = reviewEntities.Count;
            var fiveStars = reviewEntities.Count(r => r.Rating == 5);
            var fourStars = reviewEntities.Count(r => r.Rating == 4);
            var threeStars = reviewEntities.Count(r => r.Rating == 3);
            var twoStars = reviewEntities.Count(r => r.Rating == 2);
            var oneStars = reviewEntities.Count(r => r.Rating == 1);

            var reviews = reviewEntities.Select(r => new PublicKitchenReviewDto
            {
                Id = r.Id,
                CustomerFirstName = !string.IsNullOrWhiteSpace(r.Customer != null ? r.Customer.Name : null)
                    ? r.Customer.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
                    : "Customer",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

            var response = new PublicKitchenReviewsResponseDto
            {
                KitchenId = kitchen.Id,
                KitchenName = kitchen.Name,
                Summary = new PublicKitchenReviewsSummaryDto
                {
                    AverageRating = kitchen.Rating,
                    TotalReviews = totalReviews,
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

        [AllowAnonymous]
        [HttpGet("kitchens/{id}/menu")]
        public async Task<IActionResult> GetKitchenMenu(int id)
        {
            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == id && k.IsOpen && k.ApprovalStatus == "Approved");

            if (kitchen == null)
                return NotFound(new { message = "Kitchen not found or not available" });

            var menu = await _context.MenuItems
                .AsNoTracking()
                .Where(m => m.KitchenId == id && m.Available)
                .Select(m => new MenuItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Category = m.Category,
                    Description = m.Description,
                    Price = m.Price,
                    IsDeal = m.IsDeal,
                    Image = m.Image
                }).ToListAsync();

            return Ok(menu);
        }

        // ============================================================
        // 2. FAVORITES API
        // ============================================================

        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] double? lat, [FromQuery] double? lng)
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var favorites = await _context.FavoriteKitchens
                .AsNoTracking()
                .Where(f => f.CustomerId == customerId.Value)
                .Include(f => f.Kitchen)
                .ToListAsync();

            var orderService = new OrderService(_context);
            var result = favorites.Select(f => {
                decimal distance = 0;
                if (lat.HasValue && lng.HasValue)
                {
                    distance = orderService.CalculateDistanceKm(lat.Value, lng.Value, f.Kitchen.Lat, f.Kitchen.Lng);
                }

                return new FavoriteKitchenDto
                {
                    FavoriteId = f.Id,
                    KitchenId = f.Kitchen.Id,
                    Name = f.Kitchen.Name,
                    Image = f.Kitchen.Image,
                    Rating = f.Kitchen.Rating,
                    Cuisine = f.Kitchen.Cuisine,
                    DeliveryTime = f.Kitchen.DeliveryTime,
                    MinOrder = f.Kitchen.MinOrder,
                    IsOpen = f.Kitchen.IsOpen && f.Kitchen.ApprovalStatus == "Approved",
                    DistanceKm = distance
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("favorites/ids")]
        public async Task<IActionResult> GetFavoriteIds()
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var ids = await _context.FavoriteKitchens
                .AsNoTracking()
                .Where(f => f.CustomerId == customerId.Value)
                .Select(f => f.KitchenId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpPost("favorites/{kitchenId}")]
        public async Task<IActionResult> ToggleFavorite(int kitchenId)
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var kitchenExists = await _context.Kitchens.AnyAsync(k => k.Id == kitchenId);
            if (!kitchenExists)
                return NotFound(new { message = "Kitchen not found." });

            var existing = await _context.FavoriteKitchens
                .FirstOrDefaultAsync(f => f.CustomerId == customerId.Value && f.KitchenId == kitchenId);

            if (existing != null)
            {
                _context.FavoriteKitchens.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { isFavorite = false, message = "Removed from favorites." });
            }

            var favorite = new FavoriteKitchen
            {
                CustomerId = customerId.Value,
                KitchenId = kitchenId,
                CreatedAt = DateTime.UtcNow
            };
            _context.FavoriteKitchens.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { isFavorite = true, message = "Added to favorites." });
        }

        [HttpDelete("favorites/{kitchenId}")]
        public async Task<IActionResult> RemoveFavorite(int kitchenId)
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var existing = await _context.FavoriteKitchens
                .FirstOrDefaultAsync(f => f.CustomerId == customerId.Value && f.KitchenId == kitchenId);

            if (existing == null)
                return NotFound(new { message = "Kitchen is not in favorites." });

            _context.FavoriteKitchens.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Removed from favorites." });
        }

        // ============================================================
        // 3. PRE-ORDER DELIVERY FEE CALCULATOR API
        // ============================================================

        [AllowAnonymous]
        [HttpPost("calculate-delivery")]
        public async Task<IActionResult> CalculateDelivery([FromBody] DeliveryCalculationRequestDto request)
        {
            var kitchen = await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == request.KitchenId && k.ApprovalStatus == "Approved");

            if (kitchen == null)
            {
                return NotFound(new { message = "Kitchen not found or not approved." });
            }

            var orderService = new OrderService(_context);
            double kitchenLat = kitchen.Lat;
            double kitchenLng = kitchen.Lng;
            if (kitchenLat == 0 && kitchenLng == 0)
            {
                kitchenLat = 33.6844;
                kitchenLng = 73.0479;
            }

            double custLat = request.CustomerLatitude;
            double custLng = request.CustomerLongitude;
            if (custLat == 0 && custLng == 0)
            {
                custLat = 33.7107;
                custLng = 73.0673;
            }

            var distance = orderService.CalculateDistanceKm(custLat, custLng, kitchenLat, kitchenLng);
            if (distance > 50m || distance <= 0m)
            {
                distance = 3.5m;
            }

            decimal ratePerKm = kitchen.DeliveryRatePerKm > 0 ? kitchen.DeliveryRatePerKm : 30m;
            decimal calculatedFee = Math.Round(distance * ratePerKm, 0);
            decimal fee = calculatedFee < 40m ? 40m : calculatedFee;

            return Ok(new DeliveryCalculationResponseDto
            {
                KitchenId = kitchen.Id,
                KitchenName = kitchen.Name,
                DistanceKm = distance,
                DeliveryFee = fee,
                MinOrder = kitchen.MinOrder,
                CanDeliver = kitchen.IsOpen,
                Message = $"{distance:0.0} KM @ Rs. {ratePerKm:0}/KM"
            });
        }

        // ============================================================
        // 4. ORDERS API
        // ============================================================

        [HttpPost("orders")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequestDto request)
        {
            var customerId = await GetCustomerIdAsync();
            var email = GetCustomerEmail();

            if (!customerId.HasValue || string.IsNullOrEmpty(email))
                return Unauthorized();

            var orderService = new OrderService(_context);
            var (success, message, order) = await orderService.PlaceOrderAsync(request, email, customerId.Value);

            if (!success || order == null)
                return BadRequest(new { message });

            var responseDto = new OrderResponseDto
            {
                Id = order.Id,
                Status = order.Status,
                Subtotal = order.Subtotal,
                DeliveryFee = order.DeliveryFee,
                Total = order.Total
            };

            return Ok(responseDto);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var email = GetCustomerEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var customerId = await GetCustomerIdAsync();

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => (customerId.HasValue && o.CustomerId == customerId.Value) || o.Customer == email)
                .OrderByDescending(o => o.PlacedAt)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    Status = o.Status,
                    Subtotal = o.Subtotal,
                    DeliveryFee = o.DeliveryFee,
                    Total = o.Total
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderDetails(string id)
        {
            var email = GetCustomerEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            var customerId = await GetCustomerIdAsync();
            if ((customerId.HasValue && order.CustomerId != customerId.Value) &&
                !string.Equals(order.Customer, email, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { success = false, message = "Unauthorized access to order." });
            }

            return Ok(order);
        }

        [HttpPost("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var email = GetCustomerEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            var customerId = await GetCustomerIdAsync();
            if ((customerId.HasValue && order.CustomerId != customerId.Value) &&
                !string.Equals(order.Customer, email, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { success = false, message = "Unauthorized access to order." });
            }

            string status = string.IsNullOrWhiteSpace(order.Status) ? "Accepted" : order.Status.Trim();
            bool isAccepted = status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ||
                              status.Equals("Order Accepted", StringComparison.OrdinalIgnoreCase);

            if (!isAccepted)
                return BadRequest(new { message = "This order can no longer be cancelled." });

            if (!order.AcceptedAt.HasValue)
                return BadRequest(new { message = "This order cannot be cancelled because its acceptance time is unavailable." });

            DateTime cancellationDeadline = order.AcceptedAt.Value.AddMinutes(5);
            if (DateTime.UtcNow >= cancellationDeadline)
                return BadRequest(new { message = "The 5-minute cancellation period has expired." });

            order.Status = "Cancelled";

            if (customerId.HasValue)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString("N"),
                    RecipientType = "Customer",
                    RecipientId = customerId.Value,
                    Title = "Order Cancelled",
                    Body = $"Your order #{order.Id} has been cancelled successfully.",
                    Time = DateTime.UtcNow,
                    Status = "Unread",
                    OrderId = order.Id
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Order has been cancelled successfully." });
        }

        [HttpPost("orders/{id}/review")]
        public async Task<IActionResult> ReviewOrder(string id, [FromBody] Cloud_Kitchen_web_App.Api.DTOs.Requests.ReviewRequestDto request)
        {
            var customerEmail = GetCustomerEmail();
            if (string.IsNullOrEmpty(customerEmail)) return Unauthorized();

            var customerId = await GetCustomerIdAsync();
            if (customerId == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && (o.CustomerId == customerId.Value || o.Customer == customerEmail));
            if (order == null) return NotFound(new { success = false, message = "Order not found." });

            var reviewableStatuses = new[] { "Delivered", "Order Delivered", "Completed" };
            if (!reviewableStatuses.Contains(order.Status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "You can review an order only after it has been delivered." });
            }

            var existingReview = await _context.OrderReviews.FirstOrDefaultAsync(r => r.CustomerId == customerId && r.OrderId == order.Id);
            if (existingReview != null)
            {
                return BadRequest(new { success = false, message = "You have already reviewed this order." });
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { success = false, message = "Rating must be between 1 and 5." });
            }

            var review = new Cloud_Kitchen_web_App.Models.OrderReview
            {
                OrderId = order.Id,
                CustomerId = customerId.Value,
                KitchenId = order.KitchenId,
                Rating = request.Rating,
                Comment = string.IsNullOrWhiteSpace(request.Comment) ? string.Empty : request.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderReviews.Add(review);
            await _context.SaveChangesAsync();

            // Recompute kitchen rating average
            var avgRating = await _context.OrderReviews
                .Where(r => r.KitchenId == order.KitchenId)
                .AverageAsync(r => (double)r.Rating);
            var kitchen = await _context.Kitchens.FindAsync(order.KitchenId);
            if (kitchen != null)
            {
                kitchen.Rating = Math.Round(avgRating, 1);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Review submitted successfully." });
        }

        [HttpPost("orders/{id}/reorder")]
        public async Task<IActionResult> Reorder(string id)
        {
            var customerEmail = GetCustomerEmail();
            var customerId = await GetCustomerIdAsync();
            if (string.IsNullOrEmpty(customerEmail) && !customerId.HasValue) return Unauthorized();

            var normalizedEmail = customerEmail?.Trim().ToLower() ?? string.Empty;

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Kitchen)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && 
                    ((!string.IsNullOrEmpty(normalizedEmail) && o.Customer.ToLower() == normalizedEmail) || 
                     (customerId.HasValue && o.CustomerId == customerId.Value)));

            if (order == null) return NotFound(new { success = false, message = "Order not found." });
            if (order.Kitchen == null) return BadRequest(new { success = false, message = "The kitchen for this order is no longer available." });
            if (!order.Kitchen.IsOpen || !string.Equals(order.Kitchen.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "This kitchen is currently unavailable." });
            }
            if (order.Items == null || order.Items.Count == 0)
            {
                return BadRequest(new { success = false, message = "This order has no items to reorder." });
            }

            var menuItemIds = order.Items.Select(i => i.MenuItemId).Distinct().ToList();
            var currentMenuItems = await _context.MenuItems
                .AsNoTracking()
                .Where(m => menuItemIds.Contains(m.Id) && m.KitchenId == order.KitchenId && m.Available)
                .Select(m => new { m.Id, m.Name, m.Price, m.Image, m.KitchenId, KitchenName = m.Kitchen.Name })
                .ToListAsync();

            if (currentMenuItems.Count == 0)
            {
                return BadRequest(new { success = false, message = "None of the items from this order are currently available." });
            }

            var resultItems = new List<object>();
            var unavailableItems = new List<string>();
            string kitchenName = order.Kitchen.Name;
            int kitchenId = order.KitchenId;

            foreach (var oldItem in order.Items)
            {
                var currentItem = currentMenuItems.FirstOrDefault(m => m.Id == oldItem.MenuItemId);
                if (currentItem == null)
                {
                    unavailableItems.Add(oldItem.Name);
                    continue;
                }

                resultItems.Add(new
                {
                    menuItemId = currentItem.Id,
                    name = currentItem.Name,
                    price = currentItem.Price,
                    qty = oldItem.Qty,
                    quantity = oldItem.Qty,
                    imageUrl = currentItem.Image,
                    image = currentItem.Image,
                    kitchenId = kitchenId,
                    kitchenName = kitchenName,
                    total = currentItem.Price * oldItem.Qty
                });
            }

            return Ok(new
            {
                success = true,
                kitchenId = kitchenId,
                kitchenName = kitchenName,
                items = resultItems,
                availableItems = resultItems,
                unavailableItems = unavailableItems
            });
        }

        // ============================================================
        // 5. CUSTOMER PROFILE API
        // ============================================================

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var email = GetCustomerEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null)
            {
                return NotFound(new { message = "Customer profile not found." });
            }

            var dto = new CustomerProfileDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                Latitude = customer.Latitude,
                Longitude = customer.Longitude,
                ProfilePictureUrl = customer.ProfilePictureUrl
            };

            return Ok(dto);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileRequest request)
        {
            var email = GetCustomerEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
            {
                return NotFound(new { message = "Customer profile not found." });
            }

            customer.Name = request.Name?.Trim() ?? customer.Name;
            customer.Phone = request.Phone?.Trim() ?? customer.Phone;
            customer.Address = request.Address?.Trim() ?? customer.Address;
            if (request.Latitude.HasValue) customer.Latitude = request.Latitude.Value;
            if (request.Longitude.HasValue) customer.Longitude = request.Longitude.Value;

            if (!string.IsNullOrEmpty(request.ProfilePictureBase64))
            {
                try
                {
                    string b64 = request.ProfilePictureBase64.Contains(",")
                        ? request.ProfilePictureBase64.Split(',')[1]
                        : request.ProfilePictureBase64;
                    var bytes = Convert.FromBase64String(b64);
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "customers");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString("N") + ".jpg";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    customer.ProfilePictureUrl = "/images/customers/" + fileName;
                }
                catch { }
            }

            // Also synchronize ApplicationUser
            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser != null)
            {
                appUser.FullName = customer.Name;
                appUser.PhoneNumber = customer.Phone;
                await _userManager.UpdateAsync(appUser);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully.",
                profile = new CustomerProfileDto
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    Latitude = customer.Latitude,
                    Longitude = customer.Longitude,
                    ProfilePictureUrl = customer.ProfilePictureUrl
                }
            });
        }

        // ============================================================
        // 6. CHANGE PASSWORD (SECURITY API)
        // ============================================================

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { message = "Current and new passwords are required." });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { message = "New password and confirmation password do not match." });
            }

            var email = GetCustomerEmail();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = errors });
            }

            return Ok(new { success = true, message = "Password changed successfully." });
        }

        // ============================================================
        // 7. CUSTOMER SETTINGS API
        // ============================================================

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var settings = await _context.CustomerSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CustomerId == customerId.Value);

            if (settings == null)
            {
                return Ok(new CustomerSettingsDto());
            }

            return Ok(new CustomerSettingsDto
            {
                OrderNotifications = settings.OrderNotifications,
                PromotionalNotifications = settings.PromotionalNotifications,
                EmailNotifications = settings.EmailNotifications,
                ShowProfile = settings.ShowProfile,
                Language = settings.Language
            });
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateCustomerSettingsRequest request)
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var settings = await _context.CustomerSettings
                .FirstOrDefaultAsync(s => s.CustomerId == customerId.Value);

            if (settings == null)
            {
                settings = new CustomerSettings
                {
                    CustomerId = customerId.Value
                };
                _context.CustomerSettings.Add(settings);
            }

            settings.OrderNotifications = request.OrderNotifications;
            settings.PromotionalNotifications = request.PromotionalNotifications;
            settings.EmailNotifications = request.EmailNotifications;
            settings.ShowProfile = request.ShowProfile;
            settings.Language = string.IsNullOrWhiteSpace(request.Language) ? "English" : request.Language.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Settings updated successfully." });
        }

        // ============================================================
        // 8. NOTIFICATIONS API
        // ============================================================

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientType == "Customer" && n.RecipientId == customerId.Value)
                .OrderByDescending(n => n.Time)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientType == "Customer" && n.RecipientId == customerId.Value);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            notification.Status = "Read";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }

        [HttpPost("notifications/read-all")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var customerId = await GetCustomerIdAsync();
            if (!customerId.HasValue) return Unauthorized();

            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientType == "Customer" && n.RecipientId == customerId.Value && n.Status == "Unread")
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.Status = "Read";
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, count = unreadNotifications.Count, message = "All notifications marked as read." });
        }
    }
}
