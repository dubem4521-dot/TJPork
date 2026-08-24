/**
 * T&JPork Customer-Facing JavaScript
 * Interactive Search, Cart Drawer, Password Strength, and Review Modals
 */

document.addEventListener('DOMContentLoaded', function () {
    initSearchAutocomplete();
    initCartDrawer();
    initHeroSlider();
    initPasswordStrength();
    initReviewModal();
    initNotifications();
});

/* ==========================================================================
   1. Real-time Search Autocomplete
   ========================================================================== */
function initSearchAutocomplete() {
    const searchInput = document.querySelector('.tjp-search-input');
    const suggestionsBox = document.querySelector('.tjp-search-suggestions');

    if (!searchInput || !suggestionsBox) return;

    let debounceTimer;

    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        const query = this.value.trim();

        if (query.length < 2) {
            suggestionsBox.style.display = 'none';
            suggestionsBox.innerHTML = '';
            return;
        }

        debounceTimer = setTimeout(() => {
            fetch(`/Home/SearchSuggestions?query=${encodeURIComponent(query)}`)
                .then(res => res.json())
                .then(items => {
                    if (!items || items.length === 0) {
                        suggestionsBox.innerHTML = `<div class="tjp-search-item" style="color: var(--text-muted);">No cuts found matching "${query}"</div>`;
                        suggestionsBox.style.display = 'block';
                        return;
                    }

                    suggestionsBox.innerHTML = items.map(item => `
                        <a href="/Shop/Details?slug=${item.slug}" class="tjp-search-item">
                            <img src="${item.imageUrl}" alt="${item.name}" onerror="this.src='/images/products/placeholder.jpg'">
                            <div>
                                <div style="font-weight: 600; color: var(--text-primary); font-size: 0.92rem;">${item.name}</div>
                                <div style="font-size: 0.82rem; color: var(--text-muted);">${item.category || 'Pork'} &bull; <strong style="color: var(--accent-orange); font-family: var(--font-mono);">R${item.price.toFixed(2)}</strong></div>
                            </div>
                        </a>
                    `).join('');
                    suggestionsBox.style.display = 'block';
                })
                .catch(() => {
                    suggestionsBox.style.display = 'none';
                });
        }, 250);
    });

    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !suggestionsBox.contains(e.target)) {
            suggestionsBox.style.display = 'none';
        }
    });
}

/* ==========================================================================
   2. Shopping Cart & Drawer
   ========================================================================== */
function initCartDrawer() {
    const cartToggleBtns = document.querySelectorAll('.tjp-cart-trigger');
    const drawerOverlay = document.querySelector('.tjp-cart-drawer-overlay');
    const drawer = document.querySelector('.tjp-cart-drawer');
    const closeBtn = document.querySelector('.tjp-drawer-close');

    function openDrawer() {
        if (!drawer || !drawerOverlay) return;
        fetchCartDrawerContent();
        drawerOverlay.classList.add('open');
        drawer.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeDrawer() {
        if (!drawer || !drawerOverlay) return;
        drawerOverlay.classList.remove('open');
        drawer.classList.remove('open');
        document.body.style.overflow = '';
    }

    cartToggleBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            openDrawer();
        });
    });

    if (closeBtn) closeBtn.addEventListener('click', closeDrawer);
    if (drawerOverlay) drawerOverlay.addEventListener('click', closeDrawer);

    // Global Add To Cart Delegations
    document.addEventListener('click', function (e) {
        const addBtn = e.target.closest('.tjp-add-to-cart-btn');
        if (!addBtn) return;

        e.preventDefault();
        const productId = addBtn.dataset.productId;
        const quantity = parseInt(addBtn.dataset.quantity || '1', 10);

        addToCartAjax(productId, quantity, addBtn);
    });
}

function fetchCartDrawerContent() {
    const drawerBody = document.querySelector('.tjp-drawer-content-slot');
    if (!drawerBody) return;

    fetch('/Cart/GetCartDrawer')
        .then(res => res.text())
        .then(html => {
            drawerBody.innerHTML = html;
        });
}

function addToCartAjax(productId, quantity, triggerBtn) {
    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', quantity);

    if (triggerBtn) {
        triggerBtn.disabled = true;
        triggerBtn.innerHTML = `<i class="fa-solid fa-spinner fa-spin"></i> Adding...`;
    }

    fetch('/Cart/AddToCart', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                updateCartBadge(data.itemCount);
                showToast(data.message || 'Added to cart!', 'success');

                // Animate trigger button
                if (triggerBtn) {
                    triggerBtn.innerHTML = `<i class="fa-solid fa-check"></i> Added!`;
                    setTimeout(() => {
                        triggerBtn.disabled = false;
                        triggerBtn.innerHTML = `<i class="fa-solid fa-cart-shopping"></i> Add to Cart`;
                    }, 1400);
                }

                // Refresh open drawer if visible
                const drawer = document.querySelector('.tjp-cart-drawer');
                if (drawer && drawer.classList.contains('open')) {
                    fetchCartDrawerContent();
                }
            }
        })
        .catch(() => {
            if (triggerBtn) {
                triggerBtn.disabled = false;
                triggerBtn.innerHTML = `<i class="fa-solid fa-cart-shopping"></i> Add to Cart`;
            }
            showToast('Unable to add item. Please try again.', 'error');
        });
}

function updateCartQuantityAjax(productId, quantity) {
    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', quantity);

    fetch('/Cart/UpdateQuantity', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                updateCartBadge(data.itemCount);
                fetchCartDrawerContent();
                if (window.location.pathname.toLowerCase().includes('/cart')) {
                    window.location.reload();
                }
            }
        });
}

function removeCartItemAjax(productId) {
    const formData = new FormData();
    formData.append('productId', productId);

    fetch('/Cart/RemoveFromCart', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                updateCartBadge(data.itemCount);
                fetchCartDrawerContent();
                showToast('Item removed from cart.', 'info');
                if (window.location.pathname.toLowerCase().includes('/cart')) {
                    window.location.reload();
                }
            }
        });
}

function updateCartBadge(count) {
    const badges = document.querySelectorAll('.tjp-cart-count-badge');
    badges.forEach(badge => {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'flex' : 'none';
        badge.classList.add('pulse');
        setTimeout(() => badge.classList.remove('pulse'), 400);
    });
}

/* ==========================================================================
   3. Hero Slideshow Carousel
   ========================================================================== */
function initHeroSlider() {
    const slides = document.querySelectorAll('.tjp-hero-slide');
    if (slides.length <= 1) return;

    let currentSlide = 0;
    const totalSlides = slides.length;
    let slideInterval;

    function showSlide(index) {
        slides.forEach((s, idx) => {
            s.classList.toggle('active', idx === index);
        });
    }

    function nextSlide() {
        currentSlide = (currentSlide + 1) % totalSlides;
        showSlide(currentSlide);
    }

    function prevSlide() {
        currentSlide = (currentSlide - 1 + totalSlides) % totalSlides;
        showSlide(currentSlide);
    }

    const prevBtn = document.querySelector('.tjp-hero-prev');
    const nextBtn = document.querySelector('.tjp-hero-next');

    if (prevBtn) prevBtn.addEventListener('click', () => { prevSlide(); resetInterval(); });
    if (nextBtn) nextBtn.addEventListener('click', () => { nextSlide(); resetInterval(); });

    function startInterval() {
        slideInterval = setInterval(nextSlide, 5500);
    }

    function resetInterval() {
        clearInterval(slideInterval);
        startInterval();
    }

    startInterval();
}

/* ==========================================================================
   4. Password Strength Evaluator
   ========================================================================== */
function initPasswordStrength() {
    const passInput = document.querySelector('.tjp-password-input');
    const strengthBar = document.querySelector('.tjp-strength-bar');
    const strengthText = document.querySelector('.tjp-strength-text');

    if (!passInput || !strengthBar) return;

    passInput.addEventListener('input', function () {
        const val = this.value;
        let score = 0;

        if (val.length >= 6) score += 20;
        if (val.length >= 10) score += 20;
        if (/[A-Z]/.test(val)) score += 20;
        if (/[0-9]/.test(val)) score += 20;
        if (/[^A-Za-z0-9]/.test(val)) score += 20;

        strengthBar.style.width = score + '%';

        if (score < 40) {
            strengthBar.style.backgroundColor = 'var(--danger)';
            if (strengthText) strengthText.textContent = 'Weak password';
        } else if (score < 80) {
            strengthBar.style.backgroundColor = 'var(--accent-orange)';
            if (strengthText) strengthText.textContent = 'Moderate password';
        } else {
            strengthBar.style.backgroundColor = 'var(--accent-green)';
            if (strengthText) strengthText.textContent = 'Strong & secure password';
        }
    });
}

/* ==========================================================================
   5. Reviews & Star Selector
   ========================================================================== */
function initReviewModal() {
    const modal = document.querySelector('#writeReviewModal');
    const openBtns = document.querySelectorAll('.tjp-write-review-btn');
    const closeBtns = document.querySelectorAll('.tjp-modal-close');
    const starInputs = document.querySelectorAll('.tjp-star-picker i');
    const ratingInput = document.querySelector('#reviewRatingInput');

    openBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const prodId = this.dataset.productId;
            const prodName = this.dataset.productName;
            if (modal) {
                if (prodId) {
                    const idField = modal.querySelector('#reviewProductId');
                    if (idField) idField.value = prodId;
                }
                if (prodName) {
                    const titleElem = modal.querySelector('#reviewProductTitle');
                    if (titleElem) titleElem.textContent = prodName;
                }
                modal.style.display = 'flex';
            }
        });
    });

    closeBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            if (modal) modal.style.display = 'none';
        });
    });

    // Star Selection
    starInputs.forEach(star => {
        star.addEventListener('click', function () {
            const val = parseInt(this.dataset.value, 10);
            if (ratingInput) ratingInput.value = val;

            starInputs.forEach((s, idx) => {
                if (idx < val) {
                    s.classList.remove('fa-regular');
                    s.classList.add('fa-solid');
                } else {
                    s.classList.remove('fa-solid');
                    s.classList.add('fa-regular');
                }
            });
        });
    });
}

/* ==========================================================================
   6. Notification Bell Dropdown
   ========================================================================== */
function initNotifications() {
    const bellBtn = document.querySelector('.tjp-bell-btn');
    const bellDropdown = document.querySelector('.tjp-bell-dropdown');

    if (!bellBtn || !bellDropdown) return;

    bellBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        const isOpen = bellDropdown.style.display === 'block';
        bellDropdown.style.display = isOpen ? 'none' : 'block';

        if (!isOpen) {
            fetchNotifications();
        }
    });

    document.addEventListener('click', function (e) {
        if (!bellDropdown.contains(e.target) && !bellBtn.contains(e.target)) {
            bellDropdown.style.display = 'none';
        }
    });
}

function fetchNotifications() {
    const list = document.querySelector('.tjp-bell-list');
    if (!list) return;

    fetch('/Profile/GetNotifications')
        .then(res => res.json())
        .then(data => {
            const badge = document.querySelector('.tjp-bell-badge');
            if (badge) {
                badge.textContent = data.unreadCount;
                badge.style.display = data.unreadCount > 0 ? 'flex' : 'none';
            }

            if (!data.notifications || data.notifications.length === 0) {
                list.innerHTML = `<div style="padding: 16px; text-align: center; color: var(--text-muted); font-size: 0.88rem;">No new notifications</div>`;
                return;
            }

            list.innerHTML = data.notifications.map(n => `
                <div class="tjp-notif-item ${n.isRead ? '' : 'unread'}" onclick="markNotificationRead(${n.id}, '${n.linkUrl || '#'}')">
                    <i class="${n.iconClass || 'fa-solid fa-bell'}" style="color: var(--accent-orange); font-size: 1.1rem;"></i>
                    <div style="flex: 1;">
                        <div style="font-weight: 600; font-size: 0.88rem; color: var(--text-primary);">${n.title}</div>
                        <div style="font-size: 0.82rem; color: var(--text-secondary);">${n.message}</div>
                        <div style="font-size: 0.72rem; color: var(--text-muted); margin-top: 4px;">${n.timeAgo}</div>
                    </div>
                </div>
            `).join('');
        });
}

function markNotificationRead(id, linkUrl) {
    const formData = new FormData();
    formData.append('id', id);

    fetch('/Profile/MarkNotificationRead', {
        method: 'POST',
        body: formData
    }).then(() => {
        if (linkUrl && linkUrl !== '#') {
            window.location.href = linkUrl;
        }
    });
}

/* ==========================================================================
   Toast Alert Helper
   ========================================================================== */
function showToast(message, type = 'info') {
    let container = document.querySelector('.tjp-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'tjp-toast-container';
        container.style.cssText = 'position: fixed; bottom: 24px; right: 24px; z-index: 9999; display: flex; flex-direction: column; gap: 10px; pointer-events: none;';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    const icon = type === 'success' ? 'fa-circle-check' : (type === 'error' ? 'fa-circle-exclamation' : 'fa-circle-info');
    const color = type === 'success' ? 'var(--accent-green)' : (type === 'error' ? 'var(--danger)' : 'var(--accent-orange)');

    toast.style.cssText = `
        background: #FFFFFF;
        color: var(--text-primary);
        padding: 14px 20px;
        border-radius: var(--radius-md);
        box-shadow: var(--shadow-raised-hover);
        border-left: 5px solid ${color};
        display: flex;
        align-items: center;
        gap: 12px;
        font-weight: 500;
        font-size: 0.92rem;
        pointer-events: auto;
        animation: slideInRight 0.3s ease;
    `;

    toast.innerHTML = `<i class="fa-solid ${icon}" style="color: ${color}; font-size: 1.2rem;"></i><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(10px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}
