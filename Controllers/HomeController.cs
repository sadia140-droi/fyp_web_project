using Cloud_Kitchen_web_App.Models;
using CloudKitchen.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudKitchen.Controllers
{
    public class HomeController : Controller
    {
        private readonly CloudKitchenDbContext _context;

        public HomeController(CloudKitchenDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // HOME PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ========================================================
            // TODAY'S DEALS
            // Get real deals from SQL Server
            //
            // IsDeal = true
            // Available = true
            // ========================================================

            var deals = await _context.MenuItems
                .AsNoTracking()
                .Include(m => m.Kitchen)
                .Where(m => m.IsDeal && m.Available)
                .Select(m => new DealViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    Image = m.Image,
                    KitchenId = m.KitchenId,
                    KitchenName = m.Kitchen != null
                        ? m.Kitchen.Name
                        : "Kitchen"
                })
                .ToListAsync();


            // ========================================================
            // HOME PAGE MODEL
            // ========================================================

            var model = new KitchenPageViewModel
            {
                // Home page par Available Kitchens nahi hain.
                Kitchens = new List<KitchenViewModel>(),

                // Real database deals
                Deals = deals,

                // Dynamic Home page categories
                Categories = await _context.AdminCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => new CategoryViewModel
                    {
                        Name = c.Name,
                        ImagePath = !string.IsNullOrWhiteSpace(c.ImagePath) ? c.ImagePath : "/images/14.JPG"
                    })
                    .Take(8)
                    .ToListAsync()
            };


            return View(model);
        }


        // ============================================================
        // TOPBAR SEARCH
        // ============================================================

        [HttpGet]
        public IActionResult Search(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return RedirectToAction("Index", "Customer");
            }

            return RedirectToAction(
                "Index",
                "Customer",
                new
                {
                    searchQuery = searchQuery.Trim()
                }
            );
        }
    }
}