(() => {
    'use strict';

    // =========================================================
    // DOM ELEMENTS
    // =========================================================

    const ordersBody = document.getElementById('ordersBody');
    const searchInput = document.getElementById('searchInput');
    const statusFilter = document.getElementById('statusFilter');
    const hideCompletedFilter = document.getElementById('hideCompletedFilter');

    const totalCount = document.getElementById('totalCount');
    const newCount = document.getElementById('newCount');
    const pendingCount = document.getElementById('pendingCount');
    const lastSync = document.getElementById('lastSync');

    const modalBackdrop = document.getElementById('modalBackdrop');
    const modalTitle = document.getElementById('modalTitle');
    const modalStatusBadge = document.getElementById('modalStatusBadge');
    const modalBody = document.getElementById('modalBody');

    const closeModal = document.getElementById('closeModal');
    const modalCloseBottom = document.getElementById('modalCloseBottom');
    const modalCancel = document.getElementById('modalCancel');

    const statusToast = document.getElementById('statusToast');
    const toastMessage = document.getElementById('toastMessage');

    // =========================================================
    // STATE
    // =========================================================

    let currentViewOrderId = null;
    let isStatusUpdating = false;
    let isCancelling = false;
    let toastTimer = null;

    if (!ordersBody) {
        console.error('Orders table body was not found.');
        return;
    }

    // =========================================================
    // ANTI-FORGERY TOKEN
    // =========================================================

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector(
            '#orderAntiForgeryForm input[name="__RequestVerificationToken"]'
        );
        return tokenInput ? tokenInput.value : '';
    }

    // =========================================================
    // HELPERS
    // =========================================================

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return '';
        }
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function formatCurrency(value) {
        const amount = Number(value);
        if (!Number.isFinite(amount)) {
            return 'Rs. 0';
        }
        return `Rs. ${amount.toLocaleString('en-PK', {
            maximumFractionDigits: 0
        })}`;
    }

    function formatDate(value) {
        if (!value) return '-';
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return '-';
        return date.toLocaleDateString('en-GB', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        }) + ', ' + date.toLocaleTimeString('en-US', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        });
    }

    function normalizeStatus(status) {
        return (status || '').trim().toLowerCase();
    }

    // =========================================================
    // STATUS HELPERS
    // =========================================================

    function statusClass(status) {
        const s = normalizeStatus(status);
        switch (s) {
            case 'order placed':
            case 'new':
            case 'placed':
                return 's-new';

            case 'order accepted':
            case 'accepted':
                return 's-accepted';

            case 'kitchen preparing':
            case 'preparing':
                return 's-prep';

            case 'ready':
            case 'order ready':
            case 'food ready':
                return 's-ready';

            case 'out for delivery':
            case 'dispatched':
                return 's-out';

            case 'delivered':
            case 'order delivered':
            case 'completed':
                return 's-delivered';

            case 'cancelled':
            case 'canceled':
            case 'order cancelled':
                return 's-cancel';

            default:
                return 's-new';
        }
    }

    function statusBadgeHtml(status) {
        const safeStatus = escapeHtml(status || 'Unknown');
        return `<span class="status-badge ${statusClass(status)}">${safeStatus}</span>`;
    }

    function isFinalStatus(status) {
        const s = normalizeStatus(status);
        return [
            'delivered',
            'order delivered',
            'completed',
            'cancelled',
            'canceled',
            'order cancelled'
        ].includes(s);
    }

    function getStepIndex(status) {
        const s = normalizeStatus(status);
        if (s === 'cancelled' || s === 'canceled' || s === 'order cancelled') return -1;
        if (s === 'order placed' || s === 'new' || s === 'placed') return 0;
        if (s === 'order accepted' || s === 'accepted') return 1;
        if (s === 'kitchen preparing' || s === 'preparing') return 2;
        if (s === 'ready' || s === 'order ready' || s === 'food ready') return 3;
        if (s === 'out for delivery' || s === 'dispatched') return 4;
        if (s === 'delivered' || s === 'order delivered' || s === 'completed') return 5;
        return 0;
    }

    // =========================================================
    // TOAST NOTIFICATION
    // =========================================================

    function showToast(message) {
        if (!statusToast || !toastMessage) return;
        toastMessage.textContent = message;
        statusToast.classList.add('show');

        if (toastTimer) clearTimeout(toastTimer);
        toastTimer = setTimeout(() => {
            statusToast.classList.remove('show');
        }, 3600);
    }

    // =========================================================
    // ROW DATA PARSING
    // =========================================================

    function getOrderFromRow(row) {
        if (!row) return null;

        const itemRows = row.querySelectorAll('[data-order-item]');
        const items = [];

        itemRows.forEach(itemRow => {
            items.push({
                name: itemRow.dataset.name || '',
                quantity: Number(itemRow.dataset.quantity) || 1,
                price: Number(itemRow.dataset.price) || 0
            });
        });

        return {
            id: row.dataset.orderId || '',
            customer: row.dataset.customer || '',
            phone: row.dataset.phone || '',
            address: row.dataset.address || '',
            total: Number(row.dataset.total) || 0,
            placedAt: row.dataset.placedAt || '',
            status: row.dataset.status || '',
            paymentMethod: row.dataset.paymentMethod || 'COD',
            deliveryFee: Number(row.dataset.deliveryFee) || 0,
            subtotal: Number(row.dataset.subtotal) || 0,
            items: items
        };
    }

    function getOrderRow(orderId) {
        const rows = ordersBody.querySelectorAll('tr[data-order-id]');
        for (const row of rows) {
            if (String(row.dataset.orderId) === String(orderId)) {
                return row;
            }
        }
        return null;
    }

    function getAllOrderRows() {
        return Array.from(ordersBody.querySelectorAll('tr[data-order-id]'));
    }

    // =========================================================
    // RENDER & FILTER
    // =========================================================

    function renderOrders() {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const selectedStatus = statusFilter?.value || 'All';
        const hideCompleted = hideCompletedFilter?.checked || false;

        const rows = getAllOrderRows();
        let visibleCount = 0;

        rows.forEach(row => {
            const customer = (row.dataset.customer || '').toLowerCase();
            const phone = (row.dataset.phone || '').toLowerCase();
            const orderId = (row.dataset.orderId || '').toLowerCase();
            const status = row.dataset.status || '';
            const itemNames = (row.dataset.itemNames || '').toLowerCase();

            let statusMatches = false;
            if (selectedStatus === 'All') {
                statusMatches = true;
            } else {
                statusMatches = normalizeStatus(status).includes(normalizeStatus(selectedStatus));
            }

            const searchMatches =
                !query ||
                customer.includes(query) ||
                phone.includes(query) ||
                orderId.includes(query) ||
                itemNames.includes(query);

            const isFinal = isFinalStatus(status);
            const matchesHideCompleted = !hideCompleted || !isFinal;

            const visible = statusMatches && searchMatches && matchesHideCompleted;

            row.style.display = visible ? '' : 'none';
            if (visible) visibleCount++;
        });

        const noResults = document.getElementById('noOrdersResult');
        if (rows.length > 0 && visibleCount === 0) {
            if (!noResults) {
                const row = document.createElement('tr');
                row.id = 'noOrdersResult';
                row.innerHTML = `
                    <td colspan="8" style="text-align:center;padding:36px;color:#777;font-size:0.95rem;">
                        <i class="fas fa-search" style="margin-right:6px;color:var(--soft-orange);"></i>
                        No orders match your search or filter criteria.
                    </td>
                `;
                ordersBody.appendChild(row);
            }
        } else if (noResults) {
            noResults.remove();
        }

        updateCounts();
    }

    // =========================================================
    // COUNTERS
    // =========================================================

    function updateCounts() {
        const rows = getAllOrderRows();
        let total = 0;
        let newOrders = 0;
        let pending = 0;

        rows.forEach(row => {
            const status = normalizeStatus(row.dataset.status);
            total++;

            if (status === 'order placed' || status === 'new' || status === 'placed') {
                newOrders++;
            }

            if ([
                'order accepted',
                'accepted',
                'kitchen preparing',
                'preparing',
                'ready',
                'order ready',
                'out for delivery',
                'dispatched'
            ].includes(status)) {
                pending++;
            }
        });

        if (totalCount) totalCount.textContent = total.toString();
        if (newCount) newCount.textContent = newOrders.toString();
        if (pendingCount) pendingCount.textContent = pending.toString();
    }

    function updateLastSync() {
        if (!lastSync) return;
        lastSync.textContent = 'Last sync: ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    }

    // =========================================================
    // UPDATE ORDER STATUS (FROM DROPDOWN)
    // =========================================================

    async function updateOrderStatus(orderId, newStatus, selectElement) {
        if (!orderId || !newStatus) return;

        const row = getOrderRow(orderId);
        if (!row) {
            alert('Order not found.');
            return;
        }

        const currentStatus = row.dataset.status || '';
        if (normalizeStatus(currentStatus) === normalizeStatus(newStatus)) {
            return; // No change
        }

        const token = getAntiForgeryToken();
        if (!token) {
            alert('Security token not found. Please refresh the page.');
            return;
        }

        try {
            isStatusUpdating = true;
            if (selectElement) {
                selectElement.disabled = true;
                selectElement.style.opacity = '0.6';
            }

            const response = await fetch('/Seller/UpdateOrderStatus', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    orderId: orderId,
                    newStatus: newStatus
                }),
                credentials: 'same-origin'
            });

            const data = await response.json();

            if (!response.ok || data.success === false) {
                throw new Error(data.message || 'Unable to update order status.');
            }

            // Update row status
            const updatedStatus = data.status || newStatus;
            row.dataset.status = updatedStatus;

            // Update CURRENT STATUS cell badge
            const statusCell = row.querySelector('[data-order-status]');
            if (statusCell) {
                statusCell.innerHTML = statusBadgeHtml(updatedStatus);
            }

            // Update select value and disabled state if final
            if (selectElement) {
                selectElement.value = updatedStatus;
                if (isFinalStatus(updatedStatus)) {
                    selectElement.disabled = true;
                } else {
                    selectElement.disabled = false;
                }
                selectElement.style.opacity = '1';
            }

            // Update modal if currently viewing this order
            if (currentViewOrderId === orderId && modalBackdrop.style.display === 'flex') {
                populateModal(row);
            }

            // Show real-time feedback toast
            showToast(`Order #${orderId} set to "${updatedStatus}". Customer notified!`);

            updateCounts();
            updateLastSync();

        } catch (error) {
            console.error('Error updating order status:', error);
            alert(error.message || 'Error updating order status. Please try again.');

            // Revert select to original status
            if (selectElement) {
                selectElement.value = currentStatus;
                selectElement.disabled = isFinalStatus(currentStatus);
                selectElement.style.opacity = '1';
            }
        } finally {
            isStatusUpdating = false;
        }
    }

    // =========================================================
    // CANCEL ORDER
    // =========================================================

    async function cancelOrder(orderId) {
        if (!orderId || isCancelling) return;

        const row = getOrderRow(orderId);
        if (!row) {
            alert('Order not found.');
            return;
        }

        const token = getAntiForgeryToken();
        if (!token) {
            alert('Security token not found. Please refresh the page.');
            return;
        }

        const confirmed = window.confirm(`Are you sure you want to cancel Order #${orderId}?`);
        if (!confirmed) return;

        try {
            isCancelling = true;

            const response = await fetch('/Seller/CancelOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ orderId: orderId }),
                credentials: 'same-origin'
            });

            const data = await response.json();

            if (!response.ok || data.success === false) {
                throw new Error(data.message || 'Unable to cancel order.');
            }

            row.dataset.status = 'Cancelled';

            // Update badge
            const statusCell = row.querySelector('[data-order-status]');
            if (statusCell) {
                statusCell.innerHTML = statusBadgeHtml('Cancelled');
            }

            // Update dropdown
            const dropdown = row.querySelector('.status-dropdown');
            if (dropdown) {
                dropdown.value = 'Cancelled';
                dropdown.disabled = true;
            }

            if (currentViewOrderId === orderId) {
                populateModal(row);
            }

            showToast(`Order #${orderId} was cancelled. Customer notified!`);
            updateCounts();
            updateLastSync();

        } catch (error) {
            console.error('Error cancelling order:', error);
            alert(error.message || 'Failed to cancel order.');
        } finally {
            isCancelling = false;
        }
    }

    // =========================================================
    // ORDER DETAILS MODAL (MATCHING SCREENSHOT 2)
    // =========================================================

    function populateModal(row) {
        const order = getOrderFromRow(row);
        if (!order) return;

        currentViewOrderId = order.id;

        if (modalTitle) {
            modalTitle.textContent = `Order #${order.id}`;
        }

        if (modalStatusBadge) {
            modalStatusBadge.innerHTML = statusBadgeHtml(order.status);
        }

        if (modalCancel) {
            const finalState = isFinalStatus(order.status);
            modalCancel.style.display = finalState ? 'none' : 'inline-block';
        }

        if (modalBody) {
            // Items bullet list
            const itemsHtml = order.items.length > 0
                ? order.items.map(item => `
                    <li>
                        <div class="item-left">
                            <span class="bullet">•</span>
                            <strong>${item.quantity}x ${escapeHtml(item.name)}</strong>
                        </div>
                        <span class="item-price">${formatCurrency(item.price * item.quantity)}</span>
                    </li>
                `).join('')
                : `<li><span class="bullet">•</span> <span>No item details available.</span></li>`;

            // 6-step progress lifecycle
            // Steps: New (0), Accepted (1), Preparing (2), Ready (3), Dispatched (4), Delivered (5)
            const currentStepIdx = getStepIndex(order.status);
            const isCancelled = currentStepIdx === -1;

            const steps = [
                { key: 'New', label: 'New' },
                { key: 'Accepted', label: 'Accepted' },
                { key: 'Preparing', label: 'Preparing' },
                { key: 'Ready', label: 'Ready' },
                { key: 'Dispatched', label: 'Dispatched' },
                { key: 'Delivered', label: 'Delivered' }
            ];

            const stepsHtml = isCancelled
                ? `<div class="progress-pill cancelled"><i class="fas fa-times-circle" style="margin-right:4px;"></i> Cancelled</div>`
                : steps.map((step, idx) => {
                    let pillClass = 'upcoming';
                    if (idx < currentStepIdx) {
                        pillClass = 'completed';
                    } else if (idx === currentStepIdx) {
                        pillClass = 'active';
                    }
                    return `<div class="progress-pill ${pillClass}">${step.label}</div>`;
                }).join('');

            modalBody.innerHTML = `
                <!-- 2x2 Details Grid -->
                <div class="order-details-grid">
                    <div class="detail-card-box">
                        <div class="detail-label-text">CUSTOMER</div>
                        <div class="detail-value-text">${escapeHtml(order.customer || '-')}</div>
                    </div>

                    <div class="detail-card-box">
                        <div class="detail-label-text">PHONE</div>
                        <div class="detail-value-text">${escapeHtml(order.phone || 'N/A')}</div>
                    </div>

                    <div class="detail-card-box">
                        <div class="detail-label-text">TOTAL</div>
                        <div class="detail-value-text">${formatCurrency(order.total)} (${escapeHtml(order.paymentMethod || 'COD')})</div>
                    </div>

                    <div class="detail-card-box">
                        <div class="detail-label-text">PLACED</div>
                        <div class="detail-value-text">${formatDate(order.placedAt)}</div>
                    </div>

                    <div class="detail-card-box full-width">
                        <div class="detail-label-text">DELIVERY ADDRESS</div>
                        <div class="detail-value-text">${escapeHtml(order.address || 'Address not provided')}</div>
                    </div>
                </div>

                <!-- Items Section -->
                <div style="margin-top: 18px;">
                    <div class="modal-section-title">Items</div>
                    <ul class="modal-items-bullet-list">
                        ${itemsHtml}
                    </ul>
                </div>

                <!-- Progress Section (Screenshot 2) -->
                <div style="margin-top: 20px;">
                    <div class="modal-section-title">Progress</div>
                    <div class="progress-pills-row">
                        ${stepsHtml}
                    </div>
                </div>
            `;
        }
    }

    function openOrderModal(orderId) {
        const row = getOrderRow(orderId);
        if (!row || !modalBackdrop) return;

        populateModal(row);
        modalBackdrop.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }

    function closeOrderModal() {
        if (!modalBackdrop) return;
        modalBackdrop.style.display = 'none';
        document.body.style.overflow = '';
        currentViewOrderId = null;
    }

    // =========================================================
    // EVENT LISTENERS
    // =========================================================

    // Dropdown change & View button clicks on ordersBody
    ordersBody.addEventListener('change', function (event) {
        const target = event.target;
        if (target && target.classList.contains('status-dropdown')) {
            const orderId = target.dataset.orderId;
            const newStatus = target.value;
            updateOrderStatus(orderId, newStatus, target);
        }
    });

    ordersBody.addEventListener('click', function (event) {
        const viewBtn = event.target.closest('.btn-view');
        if (viewBtn) {
            const orderId = viewBtn.dataset.id;
            openOrderModal(orderId);
        }
    });

    // Modal Close
    if (closeModal) {
        closeModal.addEventListener('click', closeOrderModal);
    }

    if (modalCloseBottom) {
        modalCloseBottom.addEventListener('click', closeOrderModal);
    }

    if (modalBackdrop) {
        modalBackdrop.addEventListener('click', function (event) {
            if (event.target === modalBackdrop) {
                closeOrderModal();
            }
        });
    }

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && modalBackdrop && modalBackdrop.style.display === 'flex') {
            closeOrderModal();
        }
    });

    // Modal Cancel Order button
    if (modalCancel) {
        modalCancel.addEventListener('click', function () {
            if (!currentViewOrderId) return;
            cancelOrder(currentViewOrderId);
        });
    }

    // Search and filter listeners
    function debounce(fn, delay) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => fn.apply(this, args), delay);
        };
    }

    if (searchInput) {
        searchInput.addEventListener('input', debounce(renderOrders, 160));
    }

    if (statusFilter) {
        statusFilter.addEventListener('change', renderOrders);
    }

    if (hideCompletedFilter) {
        hideCompletedFilter.addEventListener('change', renderOrders);
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    updateCounts();
    renderOrders();

})();