/**
 * T&JPork Admin Dashboard JavaScript
 * Sidebar, Chart.js Metrics, Modal Loaders, and Bulk Actions
 */

document.addEventListener('DOMContentLoaded', function () {
    initAdminSidebar();
    initAdminBulkActions();
    initAdminModals();
});

/* ==========================================================================
   1. Admin Sidebar Toggle & Mobile Responsiveness
   ========================================================================== */
function initAdminSidebar() {
    const toggleBtn = document.querySelector('#sidebarToggleBtn');
    const sidebar = document.querySelector('.admin-sidebar');
    const backdrop = document.querySelector('#adminSidebarBackdrop');

    if (!sidebar) return;

    function isMobile() {
        return window.innerWidth <= 1024;
    }

    function closeMobileSidebar() {
        sidebar.classList.remove('expanded', 'open');
        if (backdrop) backdrop.classList.remove('active');
        if (toggleBtn) {
            toggleBtn.setAttribute('aria-expanded', 'false');
            const icon = toggleBtn.querySelector('i');
            if (icon) {
                icon.className = 'fa-solid fa-bars';
            }
        }
    }

    function openMobileSidebar() {
        sidebar.classList.add('expanded', 'open');
        if (backdrop) backdrop.classList.add('active');
        if (toggleBtn) {
            toggleBtn.setAttribute('aria-expanded', 'true');
            const icon = toggleBtn.querySelector('i');
            if (icon) {
                icon.className = 'fa-solid fa-xmark';
            }
        }
    }

    function toggleSidebar() {
        if (isMobile()) {
            if (sidebar.classList.contains('expanded') || sidebar.classList.contains('open')) {
                closeMobileSidebar();
            } else {
                openMobileSidebar();
            }
        } else {
            // Desktop: toggle collapsed state
            const isCollapsed = sidebar.classList.toggle('collapsed');
            if (toggleBtn) {
                toggleBtn.setAttribute('aria-expanded', isCollapsed ? 'false' : 'true');
            }
            try {
                localStorage.setItem('tjp_admin_sidebar_collapsed', isCollapsed ? '1' : '0');
            } catch (_) {}
        }
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
        });
    }

    if (backdrop) {
        backdrop.addEventListener('click', function () {
            closeMobileSidebar();
        });
    }

    // Auto-close on mobile when clicking any navigation link
    const navLinks = sidebar.querySelectorAll('.admin-nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function () {
            if (isMobile()) {
                closeMobileSidebar();
            }
        });
    });

    // Close on Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isMobile() && (sidebar.classList.contains('expanded') || sidebar.classList.contains('open'))) {
            closeMobileSidebar();
        }
    });

    // Restore desktop preference if previously collapsed
    if (!isMobile()) {
        try {
            if (localStorage.getItem('tjp_admin_sidebar_collapsed') === '1') {
                sidebar.classList.add('collapsed');
            }
        } catch (_) {}
    }

    // Clear mobile classes if user resizes back to desktop
    window.addEventListener('resize', function () {
        if (!isMobile()) {
            sidebar.classList.remove('expanded', 'open');
            if (backdrop) backdrop.classList.remove('active');
            if (toggleBtn) {
                const icon = toggleBtn.querySelector('i');
                if (icon) icon.className = 'fa-solid fa-bars';
            }
        }
    });
}

/* ==========================================================================
   2. Bulk Actions Management
   ========================================================================== */
function initAdminBulkActions() {
    const selectAllCheckbox = document.querySelector('#selectAllProducts');
    const itemCheckboxes = document.querySelectorAll('.product-checkbox');
    const bulkActionForm = document.querySelector('#bulkActionForm');
    const actionSelect = document.querySelector('#bulkActionSelect');
    const priceAdjustInput = document.querySelector('#bulkPriceAdjustInput');
    const categorySelect = document.querySelector('#bulkCategorySelect');
    const selectedBadge = document.querySelector('#selectedCountBadge');
    const selectedNum = document.querySelector('#selectedCountNum');

    function updateSelectedCount() {
        const checkedCount = document.querySelectorAll('.product-checkbox:checked').length;
        if (selectedBadge && selectedNum) {
            selectedNum.textContent = checkedCount;
            selectedBadge.style.display = checkedCount > 0 ? 'inline-flex' : 'none';
        }
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = itemCheckboxes.length > 0 && checkedCount === itemCheckboxes.length;
            selectAllCheckbox.indeterminate = checkedCount > 0 && checkedCount < itemCheckboxes.length;
        }
    }

    if (selectAllCheckbox && itemCheckboxes.length > 0) {
        selectAllCheckbox.addEventListener('change', function () {
            itemCheckboxes.forEach(cb => {
                cb.checked = selectAllCheckbox.checked;
            });
            updateSelectedCount();
        });

        itemCheckboxes.forEach(cb => {
            cb.addEventListener('change', updateSelectedCount);
        });
    }

    if (actionSelect) {
        actionSelect.addEventListener('change', function () {
            const val = this.value;
            if (priceAdjustInput) priceAdjustInput.style.display = val === 'adjustPrice' ? 'block' : 'none';
            if (categorySelect) categorySelect.style.display = val === 'changeCategory' ? 'block' : 'none';
        });
    }

    if (bulkActionForm) {
        bulkActionForm.addEventListener('submit', function (e) {
            const checkedCount = document.querySelectorAll('.product-checkbox:checked').length;
            if (checkedCount === 0) {
                e.preventDefault();
                alert('Please select at least one product using the checkboxes.');
                return;
            }

            const actionVal = actionSelect ? actionSelect.value : '';
            if (!actionVal) {
                e.preventDefault();
                alert('Please choose an action from the Bulk Actions dropdown.');
                return;
            }

            if (actionVal === 'delete') {
                const confirmed = confirm(`Are you sure you want to permanently delete the ${checkedCount} selected product(s)? This cannot be undone.`);
                if (!confirmed) {
                    e.preventDefault();
                }
            }
        });
    }
}

/* ==========================================================================
   3. Order Details Modal Loader
   ========================================================================== */
function initAdminModals() {
    const orderModal = document.querySelector('#adminOrderModal');
    const viewOrderBtns = document.querySelectorAll('.view-order-btn');
    const modalCloseBtns = document.querySelectorAll('.admin-modal-close');

    viewOrderBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const orderId = this.dataset.orderId;
            if (orderModal && orderId) {
                const contentSlot = orderModal.querySelector('.admin-modal-body');
                contentSlot.innerHTML = `<div style="padding: 40px; text-align: center;"><i class="fa-solid fa-spinner fa-spin fa-2x" style="color: var(--accent-orange);"></i><p style="margin-top: 10px; color: var(--text-muted);">Loading order details...</p></div>`;
                orderModal.style.display = 'flex';

                fetch(`/Admin/OrderDetails/${orderId}`)
                    .then(res => res.text())
                    .then(html => {
                        contentSlot.innerHTML = html;
                    })
                    .catch(() => {
                        contentSlot.innerHTML = `<p style="color: var(--danger); padding: 20px;">Failed to load order #${orderId}.</p>`;
                    });
            }
        });
    });

    modalCloseBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            if (orderModal) orderModal.style.display = 'none';
        });
    });

    if (orderModal) {
        orderModal.addEventListener('click', function (e) {
            if (e.target === orderModal) {
                orderModal.style.display = 'none';
            }
        });
    }
}

/* ==========================================================================
   4. Chart.js Initializers for Overview Dashboard
   ========================================================================== */
function initSalesChart(canvasId, labels, dataPoints) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Revenue (R)',
                data: dataPoints,
                borderColor: '#E67E22',
                backgroundColor: 'rgba(230, 126, 34, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.35,
                pointBackgroundColor: '#FFFFFF',
                pointBorderColor: '#E67E22',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return ' R' + context.parsed.y.toFixed(2);
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: '#F1F5F9' },
                    ticks: {
                        callback: function (value) { return 'R' + value; }
                    }
                },
                x: {
                    grid: { display: false }
                }
            }
        }
    });
}

function initCategoryDoughnutChart(canvasId, labels, dataPoints) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: dataPoints,
                backgroundColor: [
                    '#E67E22',
                    '#2C3E50',
                    '#27AE60',
                    '#3498DB',
                    '#9B59B6',
                    '#F39C12'
                ],
                borderWidth: 2,
                borderColor: '#FFFFFF'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { boxWidth: 12, font: { size: 12 } }
                }
            },
            cutout: '65%'
        }
    });
}
