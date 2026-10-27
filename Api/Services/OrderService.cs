using System;

using System.Linq;

using System.Threading.Tasks;

using CloudKitchen.Data;

using Cloud_Kitchen_web_App.Models;

using Cloud_Kitchen_web_App.Api.DTOs;

using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;



namespace Cloud_Kitchen_web_App.Api.Services

{

    public class OrderService

    {

        private readonly CloudKitchenDbContext _context;



        public OrderService(CloudKitchenDbContext context)

        {

            _context = context;

        }



        public decimal CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)

        {

            const double earthRadiusKm = 6371.0;



            double dLat = DegreesToRadians(lat2 - lat1);

            double dLon = DegreesToRadians(lon2 - lon1);



            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +

                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *

                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);



            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));



            return Math.Round((decimal)(earthRadiusKm * c), 2, MidpointRounding.AwayFromZero);

        }



        private double DegreesToRadians(double degrees)

        {

            return degrees * Math.PI / 180.0;

        }



        public async Task<(bool Success, string Message, Order? Order)> PlaceOrderAsync(OrderRequestDto request, string customerEmail, int customerId)

        {

            if (request.Items == null || !request.Items.Any())

                return (false, "Cart is empty", null);



            var kitchen = await _context.Kitchens.FindAsync(request.KitchenId);

            if (kitchen == null || !kitchen.IsOpen || kitchen.ApprovalStatus != "Approved")

                return (false, "Kitchen is not available", null);



            decimal subtotal = 0;

            var orderItems = new List<OrderItem>();



            foreach (var cartItem in request.Items)

            {

                if (cartItem.Qty <= 0)

                    return (false, "Invalid quantity", null);



                var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == cartItem.MenuItemId && m.KitchenId == request.KitchenId);

                

                if (menuItem == null || !menuItem.Available)

                    return (false, "One or more items are unavailable or do not belong to this kitchen", null);



                subtotal += menuItem.Price * cartItem.Qty;

                

                orderItems.Add(new OrderItem

                {

                    MenuItemId = menuItem.Id,

                    Name = menuItem.Name,

                    Qty = cartItem.Qty,

                    Price = menuItem.Price

                });

            }



            if (subtotal < kitchen.MinOrder)

                return (false, $"Minimum order amount is {kitchen.MinOrder}", null);



            double kLat = kitchen.Lat;

            double kLng = kitchen.Lng;

            if (kLat == 0 && kLng == 0)

            {

                kLat = 33.6844;

                kLng = 73.0479;

            }



            double cLat = request.CustomerLatitude;

            double cLng = request.CustomerLongitude;

            if (cLat == 0 && cLng == 0)

            {

                cLat = 33.7107;

                cLng = 73.0673;

            }



            decimal distanceKm = CalculateDistanceKm(cLat, cLng, kLat, kLng);

            if (distanceKm > 50 || distanceKm <= 0)

            {

                distanceKm = request.DistanceKm.HasValue && request.DistanceKm.Value > 0 && request.DistanceKm.Value <= 50

                    ? request.DistanceKm.Value

                    : 3.5m;

            }



            decimal ratePerKm = kitchen.DeliveryRatePerKm > 0 ? kitchen.DeliveryRatePerKm : 30m;

            decimal calculatedFee = Math.Max(40m, Math.Round(distanceKm * ratePerKm, 0));

            decimal deliveryFee = request.DeliveryFee.HasValue && request.DeliveryFee.Value > 0

                ? request.DeliveryFee.Value

                : calculatedFee;



            decimal total = subtotal + deliveryFee;



            var existingOrderIds = _context.Orders

                .Select(o => o.Id)

                .AsEnumerable()

                .Select(id => int.TryParse(id, out int parsedId) ? parsedId : 0)

                .Where(id => id > 0)

                .ToList();



            int nextOrderId = existingOrderIds.Count == 0 ? 1 : existingOrderIds.Max() + 1;



            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)

                return (false, "Customer not found", null);



            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try

            {

                var order = new Order

                {

                    Id = nextOrderId.ToString(),

                    CustomerId = customerId,

                    CustomerAccount = customer,

                    Customer = !string.IsNullOrWhiteSpace(request.ReceiverName) ? request.ReceiverName.Trim() : (!string.IsNullOrWhiteSpace(customer.Name) ? customer.Name.Trim() : customerEmail),

                    Phone = request.PhoneNumber,

                    Address = string.IsNullOrWhiteSpace(request.Landmark) ? request.DeliveryAddress : $"{request.DeliveryAddress}, Landmark: {request.Landmark}",

                    CustomerLatitude = request.CustomerLatitude,

                    CustomerLongitude = request.CustomerLongitude,

                    Items = orderItems,

                    Subtotal = subtotal,

                    DeliveryFee = deliveryFee,

                    DeliveryDistanceKm = distanceKm,

                    Total = total,

                    PaymentMethod = "COD",

                    PlacedAt = DateTime.UtcNow,

                    Status = "Order Placed",

                    KitchenId = kitchen.Id

                };



                _context.Orders.Add(order);

                await _context.SaveChangesAsync();



                // Create notification

                var notification = new Notification

                {

                    Id = Guid.NewGuid().ToString("N"),

                    RecipientType = "Customer",

                    RecipientId = customerId,

                    Title = $"Order Placed - {kitchen.Name}",

                    Body = $"Your order #{order.Id} from {kitchen.Name} has been placed successfully.",

                    Time = DateTime.UtcNow,

                    Status = "Unread",

                    OrderId = order.Id

                };

                _context.Notifications.Add(notification);

                

                var sellerNotification = new Notification

                {

                    Id = Guid.NewGuid().ToString("N"),

                    RecipientType = "Seller",

                    RecipientId = kitchen.KitchenOwnerId,

                    Title = "New Order",

                    Body = $"You have received a new order #{order.Id}.",

                    Time = DateTime.UtcNow,

                    Status = "Unread",

                    OrderId = order.Id

                };

                _context.Notifications.Add(sellerNotification);



                await _context.SaveChangesAsync();

                await transaction.CommitAsync();



                return (true, "Order placed successfully", order);

            }

            catch (Exception ex)

            {

                await transaction.RollbackAsync();

                Console.WriteLine($"Order placement exception: {ex}");

                return (false, ex.InnerException?.Message ?? ex.Message, (Order?)null);

            }
            });

        }

    }

}

