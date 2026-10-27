(function () {
    "use strict";

    const recentBody = document.getElementById("recentBody");
    const searchRecent = document.getElementById("searchRecent");
    const filterRecent = document.getElementById("filterRecent");

    const modalBackdrop = document.getElementById("modalBackdrop");
    const modalTitle = document.getElementById("modalTitle");
    const modalBody = document.getElementById("modalBody");
    const closeModal = document.getElementById("closeModal");
    const modalCancel = document.getElementById("modalCancel");
    const modalNext = document.getElementById("modalNext");

    if (!recentBody) {
        return;
    }

    let currentModalOrderId = null;
    let isUpdatingStatus = false;
    let isCancellingOrder = false;

    // =========================================================
    // ANTI-FORGERY TOKEN
    // =========================================================

    function getAntiForgeryToken() {
        const tokenInput =
            document.querySelector('#orderAntiForgeryForm input[name="__RequestVerificationToken"]') ||
            document.querySelector('#sellerLogoutForm input[name="__RequestVerificationToken"]') ||
            document.querySelector('input[name="__RequestVerificationToken"]');

        return tokenInput ? tokenInput.value : "";
    }

    // =========================================================
    // STATUS HELPERS
    // =========================================================

    function normalize(value) {
        return (value || "").toString().trim().toLowerCase();
    }

    function normalizeStatus(status) {
        return (status || "").trim().toLowerCase();
    }

    function statusClass(status) {
        switch (normalizeStatus(status)) {
            case "order placed":
                return "s-new";
            case "order accepted":
            case "kitchen preparing":
            case "preparing":
                return "s-prep";
            case "out for delivery":
                return "s-out";
            case "delivered":
            case "order delivered":
            case "completed":
                return "s-delivered";
            case "cancelled":
            case "canceled":
                return "s-cancel";
            default:
                return "s-new";
        }
    }

    function canAdvanceStatus(status) {
        return [
            "order placed",
            "order accepted",
            "kitchen preparing",
            "preparing",
            "out for delivery"
        ].includes(normalizeStatus(status));
    }

    function canCancelStatus(status) {
        return [
            "order placed",
            "order accepted",
            "kitchen preparing",
            "preparing"
        ].includes(normalizeStatus(status));
    }

    function nextStatusLabel(status) {
        switch (normalizeStatus(status)) {
            case "order placed":
                return "Accept Order";
            case "order accepted":
                return "Start Preparing";
            case "kitchen preparing":
            case "preparing":
                return "Mark Out for Delivery";
            case "out for delivery":
                return "Mark Delivered";
            default:
                return "Next Status";
        }
    }

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return "";
        }

        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // =========================================================
    // DOM / ROW HELPERS
    // =========================================================

    function getOrderRows() {
        return Array.from(
            recentBody.querySelectorAll("tr")
        ).filter(row => row.querySelector("[data-order-id]") || row.getAttribute("data-order-id"));
    }

    function getRowText(row) {
        return normalize(row.textContent);
    }

    function getRowStatus(row) {
        const statusElement = row.querySelector(".status-badge");
        if (statusElement) {
            return normalize(statusElement.textContent);
        }
        return normalize(row.getAttribute("data-status"));
    }

    function getOrderIdFromButton(button) {
        if (!button) {
            return null;
        }

        return button.getAttribute("data-order-id") || button.getAttribute("data-id");
    }

    function getOrderRow(orderId) {
        if (!orderId) {
            return null;
        }

        const directRow = recentBody.querySelector(`tr[data-order-id="${orderId}"]`);
        if (directRow) {
            return directRow;
        }

        const buttons = recentBody.querySelectorAll("[data-order-id], [data-id]");
        for (const button of buttons) {
            if (
                button.getAttribute("data-order-id") === orderId ||
                button.getAttribute("data-id") === orderId
            ) {
                return button.closest("tr");
            }
        }

        return null;
    }

    function getCellText(row, index) {
        if (!row) {
            return "";
        }

        const cells = row.querySelectorAll("td");
        if (!cells[index]) {
            return "";
        }

        return cells[index].textContent.trim();
    }

    // =========================================================
    // FILTERING & SEARCH
    // =========================================================

    function filterOrders() {
        const searchTerm = normalize(
            searchRecent ? searchRecent.value : ""
        );

        const selectedStatus = normalize(
            filterRecent ? filterRecent.value : "All"
        );

        const rows = getOrderRows();
        let visibleCount = 0;

        rows.forEach(row => {
            const rowText = getRowText(row);
            const rowStatus = getRowStatus(row);

            const matchesSearch =
                searchTerm === "" ||
                rowText.includes(searchTerm);

            const matchesStatus =
                selectedStatus === "" ||
                selectedStatus === "all" ||
                rowStatus === selectedStatus;

            const shouldShow = matchesSearch && matchesStatus;
            row.style.display = shouldShow ? "" : "none";

            if (shouldShow) {
                visibleCount++;
            }
        });

        updateNoResultsMessage(rows.length, visibleCount);
    }

    function updateNoResultsMessage(totalRows, visibleRows) {
        const existingMessage =
            recentBody.querySelector("[data-filter-empty-message]");

        if (existingMessage) {
            existingMessage.remove();
        }

        if (totalRows === 0 || visibleRows > 0) {
            return;
        }

        const row = document.createElement("tr");
        row.setAttribute("data-filter-empty-message", "true");
        row.innerHTML = `
            <td colspan="7"
                style="text-align:center; padding:18px; color:var(--muted);">
                No completed orders match your search or filter.
            </td>
        `;
        recentBody.appendChild(row);
    }

    // =========================================================
    // MODAL
    // =========================================================

    function openOrderDetails(orderId) {
        const row = getOrderRow(orderId);

        if (!row || !modalBackdrop || !modalBody) {
            return;
        }

        currentModalOrderId = orderId;

        const orderNumber = getCellText(row, 0);
        const customerCell = row.querySelectorAll("td")[1];
        const items = getCellText(row, 2);
        const amount = getCellText(row, 3);
        const placed = getCellText(row, 4);

        const statusBadge = row.querySelector(".status-badge");
        const status = statusBadge ? statusBadge.textContent.trim() : (row.getAttribute("data-status") || getCellText(row, 5));

        let customerName = "";
        let phone = "";

        if (customerCell) {
            const strong = customerCell.querySelector("strong");
            customerName = strong ? strong.textContent.trim() : customerCell.textContent.trim();

            const phoneElement = customerCell.querySelector("div");
            if (phoneElement) {
                phone = phoneElement.textContent.trim();
            }
        }

        if (modalTitle) {
            modalTitle.textContent = "Order #" + orderNumber;
        }

        modalBody.innerHTML = `
            <div style="display:grid; grid-template-columns: repeat(auto-fit,minmax(220px,1fr)); gap:16px;">
                <div>
                    <strong>Order #</strong>
                    <div style="margin-top:4px;">${escapeHtml(orderNumber)}</div>
                </div>

                <div>
                    <strong>Customer</strong>
                    <div style="margin-top:4px;">${escapeHtml(customerName)}</div>
                </div>

                <div>
                    <strong>Phone</strong>
                    <div style="margin-top:4px;">${escapeHtml(phone || "-")}</div>
                </div>

                <div>
                    <strong>Items</strong>
                    <div style="margin-top:4px;">${escapeHtml(items)}</div>
                </div>

                <div>
                    <strong>Amount</strong>
                    <div style="margin-top:4px; font-weight:700;">${escapeHtml(amount)}</div>
                </div>

                <div>
                    <strong>Placed</strong>
                    <div style="margin-top:4px; color:var(--muted);">${escapeHtml(placed)}</div>
                </div>

                <div>
                    <strong>Status</strong>
                    <div id="modalStatusContainer" style="margin-top:4px;">
                        <span class="status-badge ${statusClass(status)}">${escapeHtml(status)}</span>
                    </div>
                </div>
            </div>
        `;

        updateModalActionButtons(status);
        modalBackdrop.style.display = "flex";
    }

    function updateModalActionButtons(status) {
        if (modalCancel) {
            if (canCancelStatus(status)) {
                modalCancel.style.display = "inline-flex";
                modalCancel.disabled = false;
                modalCancel.textContent = "Cancel Order";
            } else {
                modalCancel.style.display = "none";
            }
        }

        if (modalNext) {
            if (canAdvanceStatus(status)) {
                modalNext.style.display = "inline-flex";
                modalNext.disabled = false;
                modalNext.textContent = nextStatusLabel(status);
            } else {
                modalNext.style.display = "none";
            }
        }
    }

    function closeOrderModal() {
        if (!modalBackdrop) {
            return;
        }

        modalBackdrop.style.display = "none";
        currentModalOrderId = null;

        if (modalBody) {
            modalBody.innerHTML = "";
        }
    }

    // =========================================================
    // ORDER ACTIONS (NEXT & CANCEL)
    // =========================================================

    async function advanceOrderStatus(orderId, buttonElement) {
        if (!orderId || isUpdatingStatus) {
            return;
        }

        const token = getAntiForgeryToken();
        if (!token) {
            alert("Security verification token was not found. Please refresh the page.");
            return;
        }

        const row = getOrderRow(orderId);
        const currentStatus = row ? (row.getAttribute("data-status") || getCellText(row, 5)) : "";

        const originalText = buttonElement ? buttonElement.textContent : "";
        if (buttonElement) {
            buttonElement.disabled = true;
            buttonElement.textContent = "Updating...";
        }

        isUpdatingStatus = true;

        try {
            const response = await fetch("/Seller/UpdateOrderStatus", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({
                    orderId: orderId
                }),
                credentials: "same-origin"
            });

            const data = await response.json();

            if (!response.ok || data.success === false) {
                throw new Error(data.message || "Failed to advance order status.");
            }

            const newStatus = data.status;

            // 1. Update row in the table
            if (row) {
                row.setAttribute("data-status", newStatus);

                const statusBadge = row.querySelector(".status-badge");
                if (statusBadge) {
                    statusBadge.className = "status-badge " + statusClass(newStatus);
                    statusBadge.textContent = newStatus;
                }

                const nextBtn = row.querySelector(".btn-next");
                if (nextBtn) {
                    if (canAdvanceStatus(newStatus)) {
                        nextBtn.disabled = false;
                        nextBtn.textContent = "Next";
                    } else {
                        nextBtn.remove();
                    }
                }
            }

            // 2. Update modal if currently opened for this order
            if (currentModalOrderId === orderId) {
                const modalStatusContainer = document.getElementById("modalStatusContainer");
                if (modalStatusContainer) {
                    modalStatusContainer.innerHTML = `
                        <span class="status-badge ${statusClass(newStatus)}">${escapeHtml(newStatus)}</span>
                    `;
                }
                updateModalActionButtons(newStatus);
            }

            filterOrders();

        } catch (err) {
            console.error("Error advancing order:", err);
            alert(err.message || "An error occurred while updating the order status.");

            if (buttonElement) {
                buttonElement.disabled = false;
                buttonElement.textContent = originalText || "Next";
            }
        } finally {
            isUpdatingStatus = false;
        }
    }

    async function cancelOrder(orderId, buttonElement) {
        if (!orderId || isCancellingOrder) {
            return;
        }

        if (!confirm("Are you sure you want to cancel this order?")) {
            return;
        }

        const token = getAntiForgeryToken();
        if (!token) {
            alert("Security verification token was not found. Please refresh the page.");
            return;
        }

        const originalText = buttonElement ? buttonElement.textContent : "";
        if (buttonElement) {
            buttonElement.disabled = true;
            buttonElement.textContent = "Cancelling...";
        }

        isCancellingOrder = true;

        try {
            const response = await fetch("/Seller/CancelOrder", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({
                    orderId: orderId
                }),
                credentials: "same-origin"
            });

            const data = await response.json();

            if (!response.ok || data.success === false) {
                throw new Error(data.message || "Failed to cancel order.");
            }

            const newStatus = "Cancelled";
            const row = getOrderRow(orderId);

            if (row) {
                row.setAttribute("data-status", newStatus);

                const statusBadge = row.querySelector(".status-badge");
                if (statusBadge) {
                    statusBadge.className = "status-badge " + statusClass(newStatus);
                    statusBadge.textContent = newStatus;
                }

                const nextBtn = row.querySelector(".btn-next");
                if (nextBtn) {
                    nextBtn.remove();
                }
            }

            closeOrderModal();
            filterOrders();

        } catch (err) {
            console.error("Error cancelling order:", err);
            alert(err.message || "An error occurred while cancelling the order.");

            if (buttonElement) {
                buttonElement.disabled = false;
                buttonElement.textContent = originalText || "Cancel Order";
            }
        } finally {
            isCancellingOrder = false;
        }
    }

    // =========================================================
    // EVENT LISTENERS
    // =========================================================

    recentBody.addEventListener("click", function (event) {
        const nextButton = event.target.closest(".btn-next");
        if (nextButton) {
            const orderId = getOrderIdFromButton(nextButton);
            if (orderId) {
                advanceOrderStatus(orderId, nextButton);
            }
            return;
        }

        const viewButton = event.target.closest(".btn-view");
        if (viewButton) {
            const orderId = getOrderIdFromButton(viewButton);
            if (orderId) {
                openOrderDetails(orderId);
            }
            return;
        }
    });

    if (modalNext) {
        modalNext.addEventListener("click", function () {
            if (currentModalOrderId) {
                advanceOrderStatus(currentModalOrderId, modalNext);
            }
        });
    }

    if (modalCancel) {
        modalCancel.addEventListener("click", function () {
            if (currentModalOrderId) {
                cancelOrder(currentModalOrderId, modalCancel);
            }
        });
    }

    if (searchRecent) {
        searchRecent.addEventListener("input", filterOrders);
    }

    if (filterRecent) {
        filterRecent.addEventListener("change", filterOrders);
    }

    if (closeModal) {
        closeModal.addEventListener("click", closeOrderModal);
    }

    if (modalBackdrop) {
        modalBackdrop.addEventListener("click", function (event) {
            if (event.target === modalBackdrop) {
                closeOrderModal();
            }
        });
    }

    document.addEventListener("keydown", function (event) {
        if (
            event.key === "Escape" &&
            modalBackdrop &&
            modalBackdrop.style.display !== "none"
        ) {
            closeOrderModal();
        }
    });

    // Initial filter run
    filterOrders();

})();