using System.ComponentModel.DataAnnotations;

namespace YourProject.Models
{
    /// <summary>
    /// ViewModel for the "Complete Your Kitchen Profile" form
    /// (Views/Account/KitchenInformation.cshtml).
    ///
    /// Property names match the form's "name" attributes exactly,
    /// so ASP.NET Core model binding works automatically with
    /// [FromForm] / a plain action parameter on:
    ///     POST /Account/SellerKitchenProfile
    /// </summary>
    public class KitchenInformationViewModel
    {
        // ---------- Kitchen Information ----------

        [Required(ErrorMessage = "Kitchen name is required.")]
        [StringLength(150, ErrorMessage = "Kitchen name cannot exceed 150 characters.")]
        [Display(Name = "Kitchen Name")]
        public string KitchenName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kitchen phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Kitchen Phone")]
        public string KitchenPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kitchen description is required.")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Kitchen Description")]
        public string Description { get; set; } = string.Empty;

        // ---------- Food & Cuisine ----------

        // Checkbox group: <input type="checkbox" name="cuisine" value="..." />
        // Multiple checked values automatically bind into this list
        // because the form field name "cuisine" matches this property name.
        [Required(ErrorMessage = "Please select at least one food category.")]
        [MinLength(1, ErrorMessage = "Please select at least one food category.")]
        [Display(Name = "Food Categories")]
        public List<string> Cuisine { get; set; } = new();

        // ---------- Kitchen Location ----------

        [Required(ErrorMessage = "City is required.")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Area / Locality is required.")]
        [StringLength(150)]
        [Display(Name = "Area / Locality")]
        public string Area { get; set; } = string.Empty;

        [Required(ErrorMessage = "Complete kitchen address is required.")]
        [StringLength(500)]
        [Display(Name = "Complete Kitchen Address")]
        public string Address { get; set; } = string.Empty;

        // ---------- Kitchen Operating Hours ----------

        [Required(ErrorMessage = "Opening time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Opening Time")]
        public TimeSpan OpeningTime { get; set; }

        [Required(ErrorMessage = "Closing time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Closing Time")]
        public TimeSpan ClosingTime { get; set; }

        // ---------- Delivery Information ----------

        [Required(ErrorMessage = "Please select a delivery method.")]
        [Display(Name = "Delivery Method")]
        public string DeliveryMethod { get; set; } = string.Empty; // "Self Delivery" | "Customer Pickup" | "Both"

        [Required(ErrorMessage = "Delivery rate per KM is required.")]
        [Range(0, 100000, ErrorMessage = "Enter a valid delivery rate.")]
        [Display(Name = "Delivery Rate Per KM")]
        public decimal DeliveryRatePerKm { get; set; }

        [Required(ErrorMessage = "Delivery areas are required.")]
        [StringLength(500)]
        [Display(Name = "Delivery Areas")]
        public string DeliveryAreas { get; set; } = string.Empty;

        // ---------- Kitchen Image ----------

        // <input type="file" name="kitchenImage" /> — requires the form's
        // enctype="multipart/form-data" (already set in the view).
        [Display(Name = "Kitchen Image")]
        public IFormFile? KitchenImage { get; set; }

        // ---------- Agreement ----------

        [Required(ErrorMessage = "You must confirm the information is accurate.")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree before submitting.")]
        [Display(Name = "Agreement")]
        public bool Agreement { get; set; }
    }
}