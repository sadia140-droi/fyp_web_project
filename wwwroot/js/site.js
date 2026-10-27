document.addEventListener("DOMContentLoaded", function () {

    /* =========================================================
       1. HERO SLIDER
       -- REMOVED --
       Was duplicated here AND in Home.js, causing two independent
       auto-slide intervals to fight each other. Now owned only by Home.js.
    ========================================================= */


    /* =========================================================
       2. EXPANDABLE DEALS SECTION (VIEW ALL)
       -- REMOVED --
       This copy only toggled a class without ever building the
       expanded cards, and it fired on the SAME click as Home.js's
       version, cancelling it out (View All appeared to do nothing).
       Now owned only by Home.js.
    ========================================================= */


    /* =========================================================
       3. CART DRAWER & STATE MANAGEMENT -- Req #1, #2, #3
       This file is now the SINGLE source of truth for the cart.
       Uses the drawer markup that already exists in _Layout.cshtml
       (#customerCartDrawer, #customerOverlay, #customerCartBody,
        #customerCartTotal, #customerCartClose, #customerCartBtn,
        #customerCartBadge, #customerCheckoutBtn) instead of injecting
       a second, duplicate drawer.
    ========================================================= */
    /* =========================================================
       3. CART DRAWER & STATE MANAGEMENT (kitchenCarts)
    ========================================================= */
    window.getCartStorageKey = function () {
        const email = (window.currentUserEmail && window.currentUserEmail !== "guest")
            ? window.currentUserEmail.trim().toLowerCase()
            : "guest";
        return "kitchenCarts_" + encodeURIComponent(email);
    };

    function getKitchenCarts() {
        try {
            const key = window.getCartStorageKey();
            if (localStorage.getItem("kitchenCarts")) {
                localStorage.removeItem("kitchenCarts"); // Purge legacy shared cart
            }
            const raw = localStorage.getItem(key);
            if (!raw) return {};
            const parsed = JSON.parse(raw);
            return (parsed && typeof parsed === "object" && !Array.isArray(parsed)) ? parsed : {};
        } catch (e) {
            return {};
        }
    }

    function saveKitchenCarts(carts) {
        try {
            const key = window.getCartStorageKey();
            localStorage.setItem(key, JSON.stringify(carts));
            localStorage.removeItem("kitchenCarts"); // Purge legacy shared cart
        } catch (e) {}
    }

    const cartDrawer = document.getElementById("customerCartDrawer");
    const cartOverlay = document.getElementById("customerOverlay");
    const cartBtn = document.getElementById("customerCartBtn");
    const cartCloseBtn = document.getElementById("customerCartClose");

    function openCart() {
        if (cartDrawer && cartOverlay) {
            cartDrawer.classList.add("active");
            cartOverlay.classList.add("active");
        }
    }

    function closeCart() {
        if (cartDrawer && cartOverlay) {
            cartDrawer.classList.remove("active");
            cartOverlay.classList.remove("active");
        }
    }

    if (cartBtn) {
        cartBtn.addEventListener("click", function (e) {
            e.preventDefault();
            openCart();
        });
    }

    if (cartCloseBtn) {
        cartCloseBtn.addEventListener("click", closeCart);
    }

    if (cartOverlay) {
        cartOverlay.addEventListener("click", closeCart);
    }


    /* =========================================================
       4. GLOBAL CLICK HANDLER -- ADD TO CART
    ========================================================= */
    document.addEventListener("click", function (e) {
        const addBtn = e.target.closest(".add-btn, .expanded-add-btn");
        if (!addBtn) return;

        e.preventDefault();

        const card = addBtn.closest(".deal-card, .expanded-card, .card");

        const id = addBtn.getAttribute("data-id") || (card ? card.getAttribute("data-id") : `item_${Date.now()}`);
        const name = addBtn.getAttribute("data-title") || (card && card.querySelector(".title") ? card.querySelector(".title").innerText.trim() : "Food Item");
        const priceVal = addBtn.getAttribute("data-price") || (card && card.querySelector(".price-row span") ? card.querySelector(".price-row span").innerText : "0");
        const price = parseInt(String(priceVal).replace(/[^0-9]/g, ""), 10) || 0;
        const image = addBtn.getAttribute("data-img") || (card && card.querySelector("img") ? card.querySelector("img").src : "");
        const kitchenName = addBtn.getAttribute("data-kitchen") || (card ? card.getAttribute("data-kitchen") : "Cloud Kitchen");
        const kitchenId = parseInt(String(addBtn.getAttribute("data-kitchen-id") || (card ? card.getAttribute("data-kitchen-id") : "0"))) || 0;

        const carts = getKitchenCarts();
        if (!carts[kitchenName]) {
            carts[kitchenName] = [];
        }

        const existingItem = carts[kitchenName].find(x => x.id === id || x.name === name);
        if (existingItem) {
            existingItem.qty = (Number(existingItem.qty) || 1) + 1;
            existingItem.quantity = existingItem.qty;
            if (kitchenId > 0 && !existingItem.kitchenId) existingItem.kitchenId = kitchenId;
        } else {
            carts[kitchenName].push({
                id: id,
                menuItemId: parseInt(String(id).replace(/[^0-9]/g, "")) || 0,
                kitchenId: kitchenId,
                name: name,
                price: price,
                image: image,
                img: image,
                kitchen: kitchenName,
                kitchenName: kitchenName,
                qty: 1,
                quantity: 1
            });
        }

        saveKitchenCarts(carts);
        updateCartUI();
        openCart();
    });


    /* =========================================================
       6. CART UI & BADGE COUNTER UPDATER
    ========================================================= */
    function escapeHtml(text) {
        if (!text) return "";
        return String(text)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function updateCartUI() {
        const carts = getKitchenCarts();
        let totalItems = 0;
        const kitchenNames = Object.keys(carts).filter(k => Array.isArray(carts[k]) && carts[k].length > 0);

        kitchenNames.forEach(k => {
            carts[k].forEach(item => {
                totalItems += (Number(item.qty) || Number(item.quantity) || 1);
            });
        });

        // Update cart badge
        const badge = document.getElementById("customerCartBadge");
        if (badge) {
            badge.innerText = totalItems;
            badge.style.display = totalItems > 0 ? "flex" : "none";
        }

        const cartDrawerBody = document.getElementById("customerCartBody");
        const cartDisclaimer = document.getElementById("customerCartDisclaimer");
        const cartFooter = document.getElementById("customerCartFooter") || document.querySelector(".cart-footer");

        if (cartFooter) {
            cartFooter.style.display = "none";
        }

        if (!cartDrawerBody) return;

        if (kitchenNames.length === 0) {
            if (cartDisclaimer) cartDisclaimer.style.display = "none";
            cartDrawerBody.innerHTML = `
                <div class="empty-cart" style="text-align: center; padding: 60px 20px;">
                    <i class="fa-solid fa-basket-shopping" style="font-size: 3.2rem; color: #e5ded6; margin-bottom: 16px; display: block;"></i>
                    <p style="font-size: 1.15rem; font-weight: 800; color: #444; margin-bottom: 6px;">Your cart is empty</p>
                    <small style="color: #888; font-size: 0.88rem;">Add items from kitchens to start ordering</small>
                </div>
            `;
            return;
        }

        if (cartDisclaimer) {
            cartDisclaimer.style.display = "block";
        }

        let html = "";
        kitchenNames.forEach(kitchenName => {
            const items = carts[kitchenName];
            const kitchenSubtotal = items.reduce((sum, x) => sum + ((Number(x.price) || 0) * (Number(x.qty) || Number(x.quantity) || 1)), 0);
            
            // Dynamic delivery fee per kilometer
            const ratePerKm = Number(items[0]?.deliveryRatePerKm) || 30;
            const kLat = Number(items[0]?.kitchenLat) || 0;
            const kLng = Number(items[0]?.kitchenLng) || 0;
            const userLat = parseFloat(localStorage.getItem("userLat") || sessionStorage.getItem("userLat") || "0");
            const userLng = parseFloat(localStorage.getItem("userLng") || sessionStorage.getItem("userLng") || "0");

            let distanceKm = 3.2; // default intra-city distance fallback
            if (userLat !== 0 && userLng !== 0 && kLat !== 0 && kLng !== 0) {
                const R = 6371;
                const dLat = (kLat - userLat) * Math.PI / 180;
                const dLon = (kLng - userLng) * Math.PI / 180;
                const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
                          Math.cos(userLat * Math.PI / 180) * Math.cos(kLat * Math.PI / 180) *
                          Math.sin(dLon/2) * Math.sin(dLon/2);
                const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
                const d = R * c;
                if (d > 0 && d < 100) {
                    distanceKm = Math.round(d * 10) / 10;
                }
            }

            const deliveryFee = Math.max(40, Math.round(distanceKm * ratePerKm));
            const kitchenTotal = kitchenSubtotal + deliveryFee;
            const itemCount = items.reduce((sum, x) => sum + (Number(x.qty) || Number(x.quantity) || 1), 0);
            const firstKitchenId = items[0]?.kitchenId || "";

            html += `
                <div class="cart-kitchen-card">
                    <!-- Kitchen Card Header -->
                    <div class="cart-kitchen-header">
                        <div>
                            <div class="cart-kitchen-title-box">
                                <i class="fa-solid fa-utensils"></i>
                                <span>${escapeHtml(kitchenName)}</span>
                            </div>
                            <div class="cart-kitchen-count">
                                ${itemCount} ${itemCount === 1 ? 'item' : 'items'}
                            </div>
                        </div>
                        <button type="button" class="btn-clear-kitchen" data-kitchen="${escapeHtml(kitchenName)}">
                            Clear
                        </button>
                    </div>

                    <!-- Kitchen Items -->
                    <div style="display: flex; flex-direction: column;">
                        ${items.map((item, idx) => {
                            const qty = Number(item.qty) || Number(item.quantity) || 1;
                            const price = Number(item.price) || 0;
                            const imgSrc = item.img || item.image || "/images/default-food.jpg";

                            return `
                                <div class="cart-item-row">
                                    <img class="cart-item-thumbnail" src="${escapeHtml(imgSrc)}" alt="${escapeHtml(item.name || '')}" onerror="this.src='/images/default-food.jpg';">
                                    
                                    <div class="cart-item-info">
                                        <p class="cart-item-title">${escapeHtml(item.name || '')}</p>
                                        <div class="cart-item-price-tag">Rs. ${price * qty}</div>
                                        <div class="cart-item-stepper">
                                            <button type="button" class="cart-qty-btn minus" data-kitchen="${escapeHtml(kitchenName)}" data-index="${idx}">−</button>
                                            <span class="cart-qty-val">${qty}</span>
                                            <button type="button" class="cart-qty-btn plus" data-kitchen="${escapeHtml(kitchenName)}" data-index="${idx}">+</button>
                                        </div>
                                    </div>

                                    <button type="button" class="cart-item-trash" data-kitchen="${escapeHtml(kitchenName)}" data-index="${idx}" title="Remove item">
                                        <i class="fa-regular fa-trash-can"></i>
                                    </button>
                                </div>
                            `;
                        }).join('')}
                    </div>

                    <!-- Pricing Breakdown -->
                    <div class="cart-pricing-box">
                        <div class="cart-pricing-row">
                            <span>Subtotal</span>
                            <span>Rs. ${kitchenSubtotal}</span>
                        </div>
                        <div class="cart-pricing-row">
                            <span>Delivery (${distanceKm.toFixed(1)} km @@ Rs. ${ratePerKm}/km)</span>
                            <span>Rs. ${deliveryFee}</span>
                        </div>
                        <div class="cart-pricing-row total-row">
                            <span>Total</span>
                            <span>Rs. ${kitchenTotal}</span>
                        </div>
                    </div>

                    <!-- Checkout from this kitchen -->
                    <button type="button" class="btn-kitchen-checkout" data-kitchen="${escapeHtml(kitchenName)}" data-kitchen-id="${firstKitchenId}">
                        <i class="fa-solid fa-lock"></i>
                        <span>Checkout from ${escapeHtml(kitchenName)}</span>
                    </button>
                </div>
            `;
        });

        cartDrawerBody.innerHTML = html;
        attachCartEvents();
    }

    function attachCartEvents() {
        // Quantity Plus
        document.querySelectorAll(".cart-qty-btn.plus").forEach(btn => {
            btn.onclick = function () {
                const kitchen = this.getAttribute("data-kitchen");
                const idx = Number(this.getAttribute("data-index"));
                const carts = getKitchenCarts();
                if (carts[kitchen] && carts[kitchen][idx]) {
                    const currentQty = Number(carts[kitchen][idx].qty) || Number(carts[kitchen][idx].quantity) || 1;
                    carts[kitchen][idx].qty = Math.min(currentQty + 1, 100);
                    carts[kitchen][idx].quantity = carts[kitchen][idx].qty;
                    saveKitchenCarts(carts);
                    updateCartUI();
                }
            };
        });

        // Quantity Minus
        document.querySelectorAll(".cart-qty-btn.minus").forEach(btn => {
            btn.onclick = function () {
                const kitchen = this.getAttribute("data-kitchen");
                const idx = Number(this.getAttribute("data-index"));
                const carts = getKitchenCarts();
                if (carts[kitchen] && carts[kitchen][idx]) {
                    const currentQty = Number(carts[kitchen][idx].qty) || Number(carts[kitchen][idx].quantity) || 1;
                    if (currentQty > 1) {
                        carts[kitchen][idx].qty = currentQty - 1;
                        carts[kitchen][idx].quantity = carts[kitchen][idx].qty;
                    } else {
                        carts[kitchen].splice(idx, 1);
                        if (carts[kitchen].length === 0) delete carts[kitchen];
                    }
                    saveKitchenCarts(carts);
                    updateCartUI();
                }
            };
        });

        // Delete single item
        document.querySelectorAll(".cart-item-trash").forEach(btn => {
            btn.onclick = function () {
                const kitchen = this.getAttribute("data-kitchen");
                const idx = Number(this.getAttribute("data-index"));
                const carts = getKitchenCarts();
                if (carts[kitchen] && carts[kitchen][idx]) {
                    carts[kitchen].splice(idx, 1);
                    if (carts[kitchen].length === 0) delete carts[kitchen];
                    saveKitchenCarts(carts);
                    updateCartUI();
                }
            };
        });

        // Clear Kitchen items
        document.querySelectorAll(".btn-clear-kitchen").forEach(btn => {
            btn.onclick = function () {
                const kitchen = this.getAttribute("data-kitchen");
                if (!kitchen) return;
                if (confirm(`Remove all items from ${kitchen}?`)) {
                    const carts = getKitchenCarts();
                    delete carts[kitchen];
                    saveKitchenCarts(carts);
                    updateCartUI();
                }
            };
        });

        // Checkout from Kitchen
        document.querySelectorAll(".btn-kitchen-checkout").forEach(btn => {
            btn.onclick = function () {
                const kitchen = this.getAttribute("data-kitchen");
                const kitchenId = this.getAttribute("data-kitchen-id") || "";
                if (!kitchen) return;

                localStorage.setItem("selectedCheckoutKitchen", kitchen);
                sessionStorage.setItem("selectedCheckoutKitchen", kitchen);

                // Navigate to Checkout with selected kitchen parameter
                window.location.href = `/Customer/Checkout?kitchen=${encodeURIComponent(kitchen)}&kitchenId=${encodeURIComponent(kitchenId)}`;
            };
        });
    }

    updateCartUI();
});
