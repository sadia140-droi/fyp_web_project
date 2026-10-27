// kitchen_information.js
// Client-side validation for the Kitchen Information (Kitchen Profile) form.
// Actual data submission happens via normal MVC form POST
// (asp-controller="Account", asp-action="SellerKitchenProfile").

document.addEventListener("DOMContentLoaded", function () {

    const kitchenForm = document.getElementById("kitchenForm");

    if (!kitchenForm) {
        return;
    }

    kitchenForm.addEventListener("submit", function (e) {

        // Check cuisine selection
        const cuisines = document.querySelectorAll('input[name="cuisine"]:checked');

        if (cuisines.length === 0) {
            e.preventDefault();
            alert("Please select at least one food category.");
            return;
        }



        // If validation passes, the form submits normally to the
        // MVC controller action, which saves the data via SQL Server
        // and sets the seller/kitchen status to "Pending Approval".
    });

});
