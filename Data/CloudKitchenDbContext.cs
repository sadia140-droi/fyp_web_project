using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudKitchen.Data
{
    public class CloudKitchenDbContext : IdentityDbContext<ApplicationUser>
    {
        public CloudKitchenDbContext(
            DbContextOptions<CloudKitchenDbContext> options)
            : base(options)
        {
        }

        // ============================================================
        // DATABASE TABLES
        // ============================================================

        public DbSet<Kitchen> Kitchens { get; set; }

        public DbSet<MenuItem> MenuItems { get; set; }

        public DbSet<KitchenOwner> KitchenOwners { get; set; }

        public DbSet<CustomerModel> Customers { get; set; }

        public DbSet<CustomerSettings> CustomerSettings { get; set; }

        public DbSet<FavoriteKitchen> FavoriteKitchens { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderReview> OrderReviews { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<AdminCategory> AdminCategories { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // ========================================================
            // KITCHEN → KITCHEN OWNER
            // ========================================================

            builder.Entity<Kitchen>()
                .HasOne(k => k.KitchenOwner)
                .WithMany(o => o.Kitchens)
                .HasForeignKey(k => k.KitchenOwnerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // KITCHEN → MENU ITEMS
            // ========================================================

            builder.Entity<MenuItem>()
                .HasOne(m => m.Kitchen)
                .WithMany(k => k.MenuItems)
                .HasForeignKey(m => m.KitchenId)
                .OnDelete(DeleteBehavior.Cascade);


            // ========================================================
            // KITCHEN PROPERTIES
            // ========================================================

            builder.Entity<Kitchen>()
                .Property(k => k.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<Kitchen>()
                .Property(k => k.Phone)
                .HasMaxLength(50);

            builder.Entity<Kitchen>()
                .Property(k => k.Description)
                .HasMaxLength(1000);

            builder.Entity<Kitchen>()
                .Property(k => k.Cuisine)
                .HasMaxLength(500);

            builder.Entity<Kitchen>()
                .Property(k => k.City)
                .HasMaxLength(100);

            builder.Entity<Kitchen>()
                .Property(k => k.Area)
                .HasMaxLength(200);

            builder.Entity<Kitchen>()
                .Property(k => k.Address)
                .HasMaxLength(500);

            builder.Entity<Kitchen>()
                .Property(k => k.Image)
                .HasMaxLength(500);

            builder.Entity<Kitchen>()
                .Property(k => k.OpeningTime)
                .HasMaxLength(20);

            builder.Entity<Kitchen>()
                .Property(k => k.ClosingTime)
                .HasMaxLength(20);

            builder.Entity<Kitchen>()
                .Property(k => k.DeliveryTime)
                .HasMaxLength(50);

            builder.Entity<Kitchen>()
                .Property(k => k.DeliveryAreas)
                .HasMaxLength(1000);

            builder.Entity<Kitchen>()
                .Property(k => k.DeliveryRatePerKm)
                .HasPrecision(18, 2);

            builder.Entity<Kitchen>()
                .Property(k => k.MinOrder)
                .HasPrecision(18, 2);

            builder.Entity<Kitchen>()
                .Property(k => k.ApprovalStatus)
                .IsRequired()
                .HasMaxLength(20);

            builder.Entity<Kitchen>()
                .Property(k => k.RejectionReason)
                .HasMaxLength(1000);


            // ========================================================
            // MENU ITEM PROPERTIES
            // ========================================================

            builder.Entity<MenuItem>()
                .Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<MenuItem>()
                .Property(m => m.Category)
                .HasMaxLength(100);

            builder.Entity<MenuItem>()
                .Property(m => m.Description)
                .HasMaxLength(1000);

            builder.Entity<MenuItem>()
                .Property(m => m.Image)
                .HasMaxLength(500);

            builder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);


            // ========================================================
            // MENU ITEM INDEXES
            // ========================================================

            builder.Entity<MenuItem>()
                .HasIndex(m => m.KitchenId);

            builder.Entity<MenuItem>()
                .HasIndex(m => new { m.KitchenId, m.Available });


            // ========================================================
            // KITCHEN OWNER PROPERTIES
            // ========================================================

            builder.Entity<KitchenOwner>()
                .Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<KitchenOwner>()
                .Property(o => o.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Entity<KitchenOwner>()
                .Property(o => o.Phone)
                .HasMaxLength(50);

            builder.Entity<KitchenOwner>()
                .Property(o => o.Address)
                .HasMaxLength(500);


            // ========================================================
            // CUSTOMER PROPERTIES
            // ========================================================

            builder.Entity<CustomerModel>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<CustomerModel>()
                .Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Entity<CustomerModel>()
                .Property(c => c.Phone)
                .HasMaxLength(50);

            builder.Entity<CustomerModel>()
                .Property(c => c.Address)
                .HasMaxLength(500);

            builder.Entity<CustomerModel>()
                .Property(c => c.ProfilePictureUrl)
                .HasMaxLength(500);


            // ========================================================
            // CUSTOMER SETTINGS
            // ========================================================

            builder.Entity<CustomerSettings>()
                .HasKey(s => s.Id);

            builder.Entity<CustomerSettings>()
                .Property(s => s.Language)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<CustomerSettings>()
                .HasOne(s => s.Customer)
                .WithOne()
                .HasForeignKey<CustomerSettings>(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomerSettings>()
                .HasIndex(s => s.CustomerId)
                .IsUnique();


            // ========================================================
            // FAVORITE KITCHENS
            // ========================================================

            builder.Entity<FavoriteKitchen>()
                .HasKey(f => f.Id);

            builder.Entity<FavoriteKitchen>()
                .HasOne(f => f.Customer)
                .WithMany()
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FavoriteKitchen>()
                .HasOne(f => f.Kitchen)
                .WithMany()
                .HasForeignKey(f => f.KitchenId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FavoriteKitchen>()
                .HasIndex(f => new { f.CustomerId, f.KitchenId })
                .IsUnique();


            // ========================================================
            // ORDER
            // ========================================================

            builder.Entity<Order>()
                .HasKey(o => o.Id);

            builder.Entity<Order>()
                .Property(o => o.Id)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Order>()
                .Property(o => o.Customer)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<Order>()
                .Property(o => o.Phone)
                .HasMaxLength(50);

            builder.Entity<Order>()
                .Property(o => o.Address)
                .HasMaxLength(500);

            builder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DeliveryFee)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DeliveryDistanceKm)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Order>()
                .Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Order>()
                .Property(o => o.PlacedAt)
                .IsRequired();


            // ========================================================
            // ORDER → KITCHEN
            // ========================================================

            builder.Entity<Order>()
                .HasOne(o => o.Kitchen)
                .WithMany()
                .HasForeignKey(o => o.KitchenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasIndex(o => o.KitchenId);

            builder.Entity<Order>()
                .HasIndex(o => o.PlacedAt);

            builder.Entity<Order>()
                .HasIndex(o => o.Status);


            // ========================================================
            // ORDER ITEMS
            // ========================================================

            builder.Entity<Order>()
                .OwnsMany(o => o.Items, item =>
                {
                    item.WithOwner()
                        .HasForeignKey("OrderId");

                    item.Property<int>("Id");

                    item.HasKey("Id");

                    item.Property(i => i.MenuItemId)
                        .IsRequired();

                    item.Property(i => i.Name)
                        .IsRequired()
                        .HasMaxLength(200);

                    item.Property(i => i.Qty)
                        .IsRequired();

                    item.Property(i => i.Price)
                        .HasPrecision(18, 2);
                });


            // ========================================================
            // ORDER REVIEWS
            // ========================================================

            builder.Entity<OrderReview>()
                .HasKey(r => r.Id);

            builder.Entity<OrderReview>()
                .Property(r => r.OrderId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<OrderReview>()
                .Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.Entity<OrderReview>()
                .Property(r => r.CreatedAt)
                .IsRequired();


            // ========================================================
            // ORDER REVIEW → ORDER
            // ========================================================

            builder.Entity<OrderReview>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // ORDER REVIEW → CUSTOMER
            // ========================================================

            builder.Entity<OrderReview>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // ORDER REVIEW → KITCHEN
            // ========================================================

            builder.Entity<OrderReview>()
                .HasOne(r => r.Kitchen)
                .WithMany()
                .HasForeignKey(r => r.KitchenId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========================================================
            // ONE REVIEW PER CUSTOMER PER ORDER
            // ========================================================

            builder.Entity<OrderReview>()
                .HasIndex(r => new { r.CustomerId, r.OrderId })
                .IsUnique();

            // ========================================================
            // NOTIFICATIONS
            // ========================================================

            builder.Entity<Notification>()
                .HasKey(n => n.Id);

            builder.Entity<Notification>()
                .Property(n => n.Id)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Notification>()
                .Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Entity<Notification>()
                .Property(n => n.Body)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Entity<Notification>()
                .Property(n => n.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Entity<Notification>()
                .Property(n => n.Link)
                .HasMaxLength(500);

            builder.Entity<Notification>()
                .Property(n => n.RecipientType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Entity<Notification>()
                .Property(n => n.RecipientId)
                .IsRequired();

            builder.Entity<Notification>()
                .Property(n => n.OrderId)
                .HasMaxLength(100);

            builder.Entity<Notification>()
                .Property(n => n.Time)
                .IsRequired();


            // ========================================================
            // NOTIFICATION INDEXES
            // ========================================================

            builder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientType, n.RecipientId });

            builder.Entity<Notification>()
                .HasIndex(n => new
                {
                    n.RecipientType,
                    n.RecipientId,
                    n.Status
                });

            builder.Entity<Notification>()
                .HasIndex(n => n.OrderId);
        }
    }
}