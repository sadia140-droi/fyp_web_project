(() => {
    'use strict';

    // =========================================================
    // DOM ELEMENTS
    // =========================================================

    const menuBody = document.getElementById('menuBody');
    const searchInput = document.getElementById('searchInput');
    const categoryFilter = document.getElementById('categoryFilter');

    const modalBackdrop = document.getElementById('modalBackdrop');
    const modalTitle = document.getElementById('modalTitle');

    const menuItemForm = document.getElementById('menuItemForm');

    const itemId = document.getElementById('itemId');
    const itemName = document.getElementById('itemName');
    const itemCategory = document.getElementById('itemCategory');
    const itemDescription = document.getElementById('itemDescription');
    const itemPrice = document.getElementById('itemPrice');
    const itemAvail = document.getElementById('itemAvail');
    const itemDeal = document.getElementById('itemDeal');
    const itemImage = document.getElementById('itemImage');

    const imgPreview = document.getElementById('imgPreview');

    const btnAdd = document.getElementById('btnAdd');
    const btnAddEmpty = document.getElementById('btnAddEmpty');

    const saveItem = document.getElementById('saveItem');
    const cancelItem = document.getElementById('cancelItem');
    const btnCancelForm = document.getElementById('btnCancelForm');

    const formMessage = document.getElementById('formMessage');

    const antiForgeryInput = document.querySelector(
        '#menuAntiForgeryForm input[name="__RequestVerificationToken"]'
    );


    // =========================================================
    // STATE
    // =========================================================

    let isSaving = false;
    let isDeleting = false;
    let isToggling = false;


    // =========================================================
    // BASIC VALIDATION
    // =========================================================

    if (!menuBody) {
        console.error('Menu body was not found.');
        return;
    }


    // =========================================================
    // HELPERS
    // =========================================================

    function getAntiForgeryToken() {
        return antiForgeryInput ? antiForgeryInput.value : '';
    }


    function showMessage(message, type = 'error') {
        if (!formMessage) {
            alert(message);
            return;
        }

        formMessage.textContent = message;
        formMessage.className = `form-message ${type}`;
        formMessage.style.display = 'block';
    }


    function hideMessage() {
        if (!formMessage) return;

        formMessage.textContent = '';
        formMessage.style.display = 'none';
        formMessage.className = 'form-message';
    }


    function openModal() {
        if (!modalBackdrop) return;

        modalBackdrop.style.display = 'flex';
        document.body.classList.add('modal-open');

        setTimeout(() => {
            if (itemName) {
                itemName.focus();
            }
        }, 50);
    }


    function closeModal() {
        if (!modalBackdrop) return;

        modalBackdrop.style.display = 'none';
        document.body.classList.remove('modal-open');

        resetForm();
    }


    function resetForm() {
        if (menuItemForm) {
            menuItemForm.reset();
        }

        if (itemId) {
            itemId.value = '0';
        }

        if (itemAvail) {
            itemAvail.value = 'true';
        }

        if (itemDeal) {
            itemDeal.value = 'false';
        }

        if (itemImage) {
            itemImage.value = '';
        }

        if (imgPreview) {
            imgPreview.src = '';
            imgPreview.style.display = 'none';
        }

        if (modalTitle) {
            modalTitle.textContent = 'Add Menu Item';
        }

        hideMessage();
    }


    function setButtonLoading(button, loading, loadingText = 'Saving...') {
        if (!button) return;

        if (loading) {
            button.dataset.originalText = button.textContent;
            button.disabled = true;
            button.textContent = loadingText;
        } else {
            button.disabled = false;

            if (button.dataset.originalText) {
                button.textContent = button.dataset.originalText;
                delete button.dataset.originalText;
            }
        }
    }


    async function parseResponse(response) {
        const contentType = response.headers.get('content-type') || '';

        if (contentType.includes('application/json')) {
            return await response.json();
        }

        const text = await response.text();

        return {
            success: response.ok,
            message: text
        };
    }


    function getErrorMessage(data, fallback) {
        if (!data) {
            return fallback;
        }

        if (data.message) {
            return data.message;
        }

        if (data.error) {
            return data.error;
        }

        return fallback;
    }


    // =========================================================
    // ADD MENU ITEM
    // =========================================================

    function openAdd() {

        resetForm();

        if (modalTitle) {
            modalTitle.textContent = 'Add Menu Item';
        }

        openModal();
    }


    // =========================================================
    // EDIT MENU ITEM
    // =========================================================

    function openEdit(button) {

        const row = button.closest('tr');

        if (!row) {
            return;
        }

        const id = row.dataset.itemId;

        if (!id) {
            showMessage('Menu item ID was not found.');
            return;
        }

        resetForm();

        if (itemId) {
            itemId.value = id;
        }

        if (itemName) {
            itemName.value = row.dataset.name || '';
        }

        if (itemCategory) {
            itemCategory.value = row.dataset.category || '';
        }

        if (itemDescription) {
            itemDescription.value = row.dataset.description || '';
        }

        if (itemPrice) {
            itemPrice.value = row.dataset.price || '';
        }

        if (itemAvail) {
            itemAvail.value =
                row.dataset.available === 'true'
                    ? 'true'
                    : 'false';
        }

        if (itemDeal) {
            itemDeal.value =
                row.dataset.deal === 'true'
                    ? 'true'
                    : 'false';
        }

        const existingImage = row.querySelector('.menu-item-image');

        if (existingImage && existingImage.src) {
            if (imgPreview) {
                imgPreview.src = existingImage.src;
                imgPreview.style.display = 'block';
            }
        }

        if (modalTitle) {
            modalTitle.textContent = 'Edit Menu Item';
        }

        openModal();
    }


    // =========================================================
    // IMAGE PREVIEW
    // =========================================================

    if (itemImage) {

        itemImage.addEventListener('change', function () {

            const file = this.files && this.files[0];

            if (!file) {
                return;
            }

            const allowedTypes = [
                'image/jpeg',
                'image/png',
                'image/webp'
            ];

            if (!allowedTypes.includes(file.type)) {

                this.value = '';

                if (imgPreview) {
                    imgPreview.src = '';
                    imgPreview.style.display = 'none';
                }

                showMessage(
                    'Only JPG, PNG and WEBP images are allowed.'
                );

                return;
            }


            const maxSize = 5 * 1024 * 1024;

            if (file.size > maxSize) {

                this.value = '';

                if (imgPreview) {
                    imgPreview.src = '';
                    imgPreview.style.display = 'none';
                }

                showMessage(
                    'Image size must not exceed 5 MB.'
                );

                return;
            }


            const reader = new FileReader();

            reader.onload = function (event) {

                if (imgPreview) {
                    imgPreview.src = event.target.result;
                    imgPreview.style.display = 'block';
                }

                hideMessage();
            };

            reader.readAsDataURL(file);
        });
    }


    // =========================================================
    // CREATE / UPDATE
    // =========================================================

    if (menuItemForm) {

        menuItemForm.addEventListener('submit', async function (event) {

            event.preventDefault();

            if (isSaving) {
                return;
            }

            hideMessage();


            // -----------------------------
            // Client-side validation
            // -----------------------------

            const name = itemName
                ? itemName.value.trim()
                : '';

            const price = itemPrice
                ? Number(itemPrice.value)
                : NaN;

            const id = itemId
                ? Number(itemId.value)
                : 0;


            if (!name) {
                showMessage('Item name is required.');
                itemName?.focus();
                return;
            }


            if (!Number.isFinite(price) || price < 0) {
                showMessage('Please enter a valid price.');
                itemPrice?.focus();
                return;
            }


            if (price > 999999999.99) {
                showMessage('Price exceeds the allowed limit.');
                itemPrice?.focus();
                return;
            }


            const formData = new FormData(menuItemForm);

            const isEdit = id > 0;

            const endpoint = isEdit
                ? '/Seller/UpdateMenuItem'
                : '/Seller/CreateMenuItem';


            try {

                isSaving = true;

                setButtonLoading(
                    saveItem,
                    true,
                    isEdit ? 'Updating...' : 'Saving...'
                );


                const token = getAntiForgeryToken();

                if (!token) {
                    throw new Error(
                        'Security token was not found. Please refresh the page and try again.'
                    );
                }


                const response = await fetch(endpoint, {

                    method: 'POST',

                    headers: {
                        'RequestVerificationToken': token
                    },

                    body: formData,

                    credentials: 'same-origin'
                });


                const data = await parseResponse(response);


                if (!response.ok || data.success === false) {

                    throw new Error(
                        getErrorMessage(
                            data,
                            'Unable to save the menu item.'
                        )
                    );
                }


                // Successful database operation.
                closeModal();

                window.location.reload();

            }
            catch (error) {

                console.error(
                    'Menu item save error:',
                    error
                );

                showMessage(
                    error.message ||
                    'Something went wrong while saving the menu item.'
                );

            }
            finally {

                isSaving = false;

                setButtonLoading(
                    saveItem,
                    false
                );
            }

        });
    }


    // =========================================================
    // DELETE MENU ITEM
    // =========================================================

    async function deleteMenuItem(button) {

        if (isDeleting) {
            return;
        }

        const id = Number(button.dataset.id);

        if (!id || id <= 0) {
            alert('Invalid menu item.');
            return;
        }


        const row = button.closest('tr');

        const itemNameElement = row
            ? row.querySelector('.menu-item-text strong')
            : null;

        const name = itemNameElement
            ? itemNameElement.textContent.trim()
            : 'this menu item';


        const confirmed = window.confirm(
            `Are you sure you want to delete "${name}"?`
        );


        if (!confirmed) {
            return;
        }


        try {

            isDeleting = true;

            button.disabled = true;
            button.textContent = 'Deleting...';


            const token = getAntiForgeryToken();

            if (!token) {
                throw new Error(
                    'Security token was not found. Please refresh the page.'
                );
            }


            const response = await fetch(
                '/Seller/DeleteMenuItem',
                {
                    method: 'POST',

                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },

                    body: JSON.stringify({
                        id: id
                    }),

                    credentials: 'same-origin'
                }
            );


            const data = await parseResponse(response);


            if (!response.ok || data.success === false) {

                throw new Error(
                    getErrorMessage(
                        data,
                        'Unable to delete the menu item.'
                    )
                );
            }


            // Remove only after server confirms success.
            if (row) {
                row.remove();
            }


            handleEmptyMenu();

        }
        catch (error) {

            console.error(
                'Menu item delete error:',
                error
            );

            alert(
                error.message ||
                'Unable to delete the menu item.'
            );

            button.disabled = false;
            button.textContent = 'Delete';

        }
        finally {

            isDeleting = false;
        }
    }


    // =========================================================
    // AVAILABILITY TOGGLE
    // =========================================================

    async function toggleAvailability(button) {

        if (isToggling) {
            return;
        }


        const id = Number(button.dataset.id);

        if (!id || id <= 0) {
            alert('Invalid menu item.');
            return;
        }


        const currentAvailable =
            button.dataset.available === 'true';

        const newAvailable = !currentAvailable;


        try {

            isToggling = true;

            button.disabled = true;


            const token = getAntiForgeryToken();

            if (!token) {
                throw new Error(
                    'Security token was not found. Please refresh the page.'
                );
            }


            const response = await fetch(
                '/Seller/ToggleMenuItemAvailability',
                {
                    method: 'POST',

                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },

                    body: JSON.stringify({
                        id: id,
                        available: newAvailable
                    }),

                    credentials: 'same-origin'
                }
            );


            const data = await parseResponse(response);


            if (!response.ok || data.success === false) {

                throw new Error(
                    getErrorMessage(
                        data,
                        'Unable to update availability.'
                    )
                );
            }


            // Update UI only after DB confirms success.
            button.dataset.available =
                newAvailable.toString();

            button.textContent =
                newAvailable
                    ? 'Available'
                    : 'Unavailable';


            button.classList.toggle(
                'active',
                newAvailable
            );

        }
        catch (error) {

            console.error(
                'Availability update error:',
                error
            );

            alert(
                error.message ||
                'Unable to update availability.'
            );

        }
        finally {

            isToggling = false;

            button.disabled = false;
        }
    }


    // =========================================================
    // SEARCH + FILTER
    // =========================================================

    function filterMenu() {

        const query = (
            searchInput?.value || ''
        )
            .trim()
            .toLowerCase();


        const selectedCategory =
            categoryFilter?.value || 'All';


        const rows = menuBody.querySelectorAll(
            'tr[data-item-id]'
        );


        let visibleCount = 0;


        rows.forEach(row => {

            const name = (
                row.dataset.name || ''
            ).toLowerCase();

            const category = (
                row.dataset.category || ''
            ).toLowerCase();


            const categoryMatch =
                selectedCategory === 'All' ||
                category === selectedCategory.toLowerCase();


            const searchMatch =
                !query ||
                name.includes(query) ||
                category.includes(query);


            const shouldShow =
                categoryMatch &&
                searchMatch;


            row.style.display =
                shouldShow
                    ? ''
                    : 'none';


            if (shouldShow) {
                visibleCount++;
            }
        });


        const existingNoResults =
            document.getElementById('noMenuResults');


        if (visibleCount === 0 && rows.length > 0) {

            if (!existingNoResults) {

                const row = document.createElement('tr');

                row.id = 'noMenuResults';

                row.innerHTML = `
                    <td colspan="6"
                        style="text-align:center;padding:24px;color:#666;">
                        No menu items match your search.
                    </td>
                `;

                menuBody.appendChild(row);
            }

        }
        else if (existingNoResults) {

            existingNoResults.remove();
        }
    }


    // =========================================================
    // EMPTY MENU STATE
    // =========================================================

    function handleEmptyMenu() {

        const cards = menuBody.querySelectorAll(
            '.menu-item-card'
        );

        const existingEmptyRow =
            document.getElementById('emptyMenuRow');

        const btnAddEmpty = document.getElementById('btnAddEmpty');

        if (cards.length === 0) {

            if (!btnAddEmpty && !existingEmptyRow) {

                const emptyDiv = document.createElement('div');
                emptyDiv.id = 'emptyMenuRow';
                emptyDiv.style.gridColumn = '1 / -1';
                emptyDiv.style.textAlign = 'center';
                emptyDiv.style.padding = '60px 20px';
                emptyDiv.style.background = '#fff';
                emptyDiv.style.borderRadius = '12px';
                emptyDiv.style.cursor = 'pointer';
                emptyDiv.style.border = '2px dashed #ddd';

                emptyDiv.innerHTML = `
                    <h3 style="color:var(--muted); margin-bottom: 10px;">No menu items found</h3>
                    <p style="color: #888;">Click here or the "Add New Item" button to create your first dish!</p>
                `;

                menuBody.appendChild(emptyDiv);

                emptyDiv.addEventListener('click', openAdd);
            }
            else if (btnAddEmpty) {
                btnAddEmpty.style.display = 'block';
            }

        }
        else {
            if (existingEmptyRow) {
                existingEmptyRow.remove();
            }
            if (btnAddEmpty) {
                btnAddEmpty.style.display = 'none';
            }
        }
    }


    // =========================================================
    // EVENT DELEGATION
    // =========================================================

    menuBody.addEventListener(
        'click',
        function (event) {

            const editButton =
                event.target.closest('.btn-edit');

            if (editButton) {
                openEdit(editButton);
                return;
            }


            const deleteButton =
                event.target.closest('.btn-del');

            if (deleteButton) {
                deleteMenuItem(deleteButton);
                return;
            }


            const toggleButton =
                event.target.closest(
                    '.toggle-availability'
                );

            if (toggleButton) {
                toggleAvailability(toggleButton);
            }
        }
    );


    // =========================================================
    // BUTTON EVENTS
    // =========================================================

    if (btnAdd) {
        btnAdd.addEventListener(
            'click',
            openAdd
        );
    }


    if (btnAddEmpty) {
        btnAddEmpty.addEventListener(
            'click',
            openAdd
        );
    }


    if (cancelItem) {
        cancelItem.addEventListener(
            'click',
            closeModal
        );
    }


    if (btnCancelForm) {
        btnCancelForm.addEventListener(
            'click',
            closeModal
        );
    }


    // =========================================================
    // CLOSE MODAL
    // =========================================================

    if (modalBackdrop) {

        modalBackdrop.addEventListener(
            'click',
            function (event) {

                if (event.target === modalBackdrop) {
                    closeModal();
                }
            }
        );
    }


    document.addEventListener(
        'keydown',
        function (event) {

            if (
                event.key === 'Escape' &&
                modalBackdrop &&
                modalBackdrop.style.display === 'flex'
            ) {
                closeModal();
            }
        }
    );


    // =========================================================
    // SEARCH EVENTS
    // =========================================================

    if (searchInput) {

        searchInput.addEventListener(
            'input',
            filterMenu
        );
    }


    if (categoryFilter) {

        categoryFilter.addEventListener(
            'change',
            filterMenu
        );
    }


    // =========================================================
    // INITIALIZATION
    // =========================================================

    filterMenu();
    handleEmptyMenu();

})();