document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    /* =========================================================
       1. HERO SLIDER
       (Owned here only — removed duplicate copy from site.js)
    ========================================================= */
    const slides = document.getElementById("slides");
    const pager = document.getElementById("pager");
    const prevBtn = document.getElementById("prevBtn");
    const nextBtn = document.getElementById("nextBtn");

    let currentSlide = 0;
    let slideTimer = null;

    if (slides) {
        const slideItems = slides.querySelectorAll(".slide");
        const totalSlides = slideItems.length;

        if (pager && totalSlides > 0) {
            pager.innerHTML = "";
            slideItems.forEach(function (_, index) {
                const dot = document.createElement("span");
                dot.className = "dot";
                if (index === 0) dot.classList.add("active");
                dot.dataset.index = index;

                dot.addEventListener("click", function () {
                    goToSlide(index);
                    restartSlider();
                });

                pager.appendChild(dot);
            });
        }

        function goToSlide(index) {
            if (totalSlides === 0) return;
            if (index < 0) index = totalSlides - 1;
            if (index >= totalSlides) index = 0;

            currentSlide = index;
            slides.style.transform = "translateX(-" + (currentSlide * 100) + "%)";

            if (pager) {
                const dots = pager.querySelectorAll(".dot");
                dots.forEach(function (dot, dotIndex) {
                    dot.classList.toggle("active", dotIndex === currentSlide);
                });
            }
        }

        if (nextBtn) {
            nextBtn.addEventListener("click", function () {
                goToSlide(currentSlide + 1);
                restartSlider();
            });
        }

        if (prevBtn) {
            prevBtn.addEventListener("click", function () {
                goToSlide(currentSlide - 1);
                restartSlider();
            });
        }

        function startSlider() {
            if (totalSlides <= 1) return;
            slideTimer = setInterval(function () {
                goToSlide(currentSlide + 1);
            }, 4000);
        }

        function restartSlider() {
            if (slideTimer) clearInterval(slideTimer);
            startSlider();
        }

        goToSlide(0);
        startSlider();
    }


    /* =========================================================
       2. ORDER NOW BUTTON (SMOOTH SCROLL) -- Req #6
    ========================================================= */
    const orderNowBtn = document.getElementById("orderNowBtn");
    if (orderNowBtn) {
        orderNowBtn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation(); // IMPORTANT: stops site.js's broad .btn-primary
            // add-to-cart matcher from firing on this button
            const dealsSection = document.getElementById("todayDeals") ||
                document.getElementById("deals-section") ||
                document.querySelector(".deals-container");

            if (dealsSection) {
                dealsSection.scrollIntoView({ behavior: "smooth", block: "start" });
            }
        });
    }


    /* =========================================================
       3. SEE MENU BUTTON -- Req #7
    ========================================================= */
    const seeMenuBtn = document.getElementById("seeMenuBtn");
    if (seeMenuBtn) {
        seeMenuBtn.addEventListener("click", function () {
            window.location.href = "/Customer/Index";
        });
    }


    /* =========================================================
       4. VIEW ALL DEALS (EXPAND / COLLAPSE GRID)
       (Owned here only — removed duplicate empty-toggle copy from site.js)
    ========================================================= */
    const viewAllBtn = document.getElementById("viewAllBtn");
    const dealsExpanded = document.getElementById("dealsExpanded");
    const expandedGrid = document.getElementById("expandedGrid");

    if (viewAllBtn && dealsExpanded && expandedGrid) {
        viewAllBtn.addEventListener("click", function () {
            const isOpen = dealsExpanded.classList.contains("open");

            if (isOpen) {
                dealsExpanded.classList.remove("open");
                dealsExpanded.setAttribute("aria-hidden", "true");
                viewAllBtn.setAttribute("aria-expanded", "false");
                viewAllBtn.textContent = "View All";
                return;
            }

            const dealCards = document.querySelectorAll("#dealsGrid .deal-card");
            expandedGrid.innerHTML = "";

            dealCards.forEach(function (card) {
                const id = card.dataset.id || "";
                const title = card.dataset.title || "Food Item";
                const price = Number(card.dataset.price) || 0;
                const image = card.dataset.img || "";
                const kitchen = card.dataset.kitchen || "Cloud Kitchen";

                const expandedCard = document.createElement("div");
                expandedCard.className = "expanded-card";

                expandedCard.innerHTML = `
                    <img src="${escapeHtml(image)}" alt="${escapeHtml(title)}">
                    <div class="expanded-body">
                        <div>
                            <h3 class="title">${escapeHtml(title)}</h3>
                            <div>From ${escapeHtml(kitchen)}</div>
                        </div>
                        <div class="price-row">
                            <span>Rs. ${price}</span>
                            <button type="button" 
                                    class="btn btn-primary expanded-add-btn add-btn"
                                    data-id="${escapeHtml(id)}"
                                    data-title="${escapeHtml(title)}"
                                    data-price="${price}"
                                    data-img="${escapeHtml(image)}"
                                    data-kitchen="${escapeHtml(kitchen)}">
                                Add
                            </button>
                        </div>
                    </div>
                `;

                expandedGrid.appendChild(expandedCard);
            });

            dealsExpanded.classList.add("open");
            dealsExpanded.setAttribute("aria-hidden", "false");
            viewAllBtn.setAttribute("aria-expanded", "true");
            viewAllBtn.textContent = "Show Less";
        });
    }


    /* =========================================================
       5. ADD DEAL TO CART
       -- REMOVED FROM THIS FILE --
       Cart add/open/render/badge logic now lives ONLY in site.js
       to avoid two competing cart states (was causing Req #1/#2/#3 bugs).
    ========================================================= */


    /* =========================================================
       6. CATEGORY CLICK
    ========================================================= */
    const categories = document.querySelectorAll(".cat[data-cat]");

    categories.forEach(function (category) {
        function openCategory() {
            const categoryName = category.dataset.cat || "";
            if (!categoryName) return;

            window.location.href = "/Customer/Index?category=" + encodeURIComponent(categoryName);
        }

        category.addEventListener("click", openCategory);

        category.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                openCategory();
            }
        });
    });


    /* =========================================================
       7. HELPER: ESCAPE HTML
    ========================================================= */
    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
});
