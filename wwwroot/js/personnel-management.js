// ═══════════════════════════════════════════════════════════════════════════
// Personnel Management JavaScript
// Handles filtering, search, modals, and AJAX operations
// ═══════════════════════════════════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', function() {
    initializeFilters();
    initializeActionMenus();
});

// ═══════════════════════════════════════════════════════════════════════════
// FILTER & SEARCH
// ═══════════════════════════════════════════════════════════════════════════

function initializeFilters() {
    const searchInput = document.getElementById('searchInput');
    const departmentFilter = document.getElementById('departmentFilter');
    const employmentTypeFilter = document.getElementById('employmentTypeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const accountFilter = document.getElementById('accountFilter');
    const resetButton = document.getElementById('reset-filters');

    if (searchInput) {
        searchInput.addEventListener('input', filterTable);
    }

    if (departmentFilter) {
        departmentFilter.addEventListener('change', filterTable);
    }

    if (employmentTypeFilter) {
        employmentTypeFilter.addEventListener('change', filterTable);
    }

    if (statusFilter) {
        statusFilter.addEventListener('change', filterTable);
    }

    if (accountFilter) {
        accountFilter.addEventListener('change', filterTable);
    }

    if (resetButton) {
        resetButton.addEventListener('click', resetFilters);
    }
}

function filterTable() {
    const searchTerm = document.getElementById('searchInput')?.value.toLowerCase() || '';
    const departmentValue = document.getElementById('departmentFilter')?.value || '';
    const employmentTypeValue = document.getElementById('employmentTypeFilter')?.value || '';
    const statusValue = document.getElementById('statusFilter')?.value || '';
    const accountValue = document.getElementById('accountFilter')?.value || '';

    const table = document.getElementById('personnelTable');
    if (!table) return;

    const rows = table.querySelectorAll('tbody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        const name = row.querySelector('.td-staff')?.textContent.toLowerCase() || '';
        const employeeId = row.querySelector('td:nth-child(2)')?.textContent.toLowerCase() || '';
        const department = row.getAttribute('data-department') || '';
        const employmentType = row.getAttribute('data-employment-type') || '';
        const status = row.getAttribute('data-status') || '';
        const account = row.getAttribute('data-account') || '';

        const matchesSearch = name.includes(searchTerm) || employeeId.includes(searchTerm);
        const matchesDepartment = !departmentValue || department === departmentValue;
        const matchesEmploymentType = !employmentTypeValue || employmentType === employmentTypeValue;
        const matchesStatus = !statusValue || status === statusValue;
        const matchesAccount = !accountValue || account === accountValue;

        if (matchesSearch && matchesDepartment && matchesEmploymentType && matchesStatus && matchesAccount) {
            row.style.display = '';
            visibleCount++;
        } else {
            row.style.display = 'none';
        }
    });

    // Update row count
    const rowCount = document.getElementById('row-count');
    if (rowCount) {
        rowCount.innerHTML = `Showing <strong>${visibleCount}</strong> personnel`;
    }
}

function resetFilters() {
    const searchInput = document.getElementById('searchInput');
    const departmentFilter = document.getElementById('departmentFilter');
    const employmentTypeFilter = document.getElementById('employmentTypeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const accountFilter = document.getElementById('accountFilter');

    if (searchInput) searchInput.value = '';
    if (departmentFilter) departmentFilter.value = '';
    if (employmentTypeFilter) employmentTypeFilter.value = '';
    if (statusFilter) statusFilter.value = '';
    if (accountFilter) accountFilter.value = '';

    filterTable();
}

// ═══════════════════════════════════════════════════════════════════════════
// ACTION MENUS
// ═══════════════════════════════════════════════════════════════════════════

function initializeActionMenus() {
    document.querySelectorAll('.action-trigger').forEach(trigger => {
        trigger.addEventListener('click', function(e) {
            e.stopPropagation();
            const dropdown = this.nextElementSibling;
            const isOpen = dropdown.classList.contains('show');

            // Close all dropdowns
            document.querySelectorAll('.action-dropdown').forEach(d => {
                d.classList.remove('show');
            });

            // Toggle current dropdown
            if (!isOpen) {
                dropdown.classList.add('show');
            }
        });
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', function() {
        document.querySelectorAll('.action-dropdown').forEach(dropdown => {
            dropdown.classList.remove('show');
        });
    });
}

// ═══════════════════════════════════════════════════════════════════════════
// PERSONNEL ACTIONS
// ═══════════════════════════════════════════════════════════════════════════

function archivePersonnel(personnelId, personnelName) {
    if (!confirm(`Are you sure you want to archive ${personnelName}?\n\nThis will set their status to Inactive but preserve all history.`)) {
        return;
    }

    fetch(`/admin/personnel/${personnelId}/archive`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message, 'success');
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(data.message, 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while archiving the personnel', 'error');
    });
}

function reactivatePersonnel(personnelId, personnelName) {
    if (!confirm(`Are you sure you want to reactivate ${personnelName}?`)) {
        return;
    }

    fetch(`/admin/personnel/${personnelId}/reactivate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message, 'success');
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(data.message, 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while reactivating the personnel', 'error');
    });
}

function unlinkUser(personnelId, personnelName) {
    if (!confirm(`Are you sure you want to unlink the user account from ${personnelName}?\n\nThis will not delete the user account, only remove the link.`)) {
        return;
    }

    fetch(`/admin/personnel/${personnelId}/unlink-user`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message, 'success');
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else {
            showNotification(data.message, 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while unlinking the user account', 'error');
    });
}

function openLinkUserModal(personnelId, personnelName) {
    // Placeholder for future implementation
    alert(`Link User Account feature for ${personnelName} will be implemented in a future update.`);
}

// ═══════════════════════════════════════════════════════════════════════════
// NOTIFICATIONS
// ═══════════════════════════════════════════════════════════════════════════

function showNotification(message, type = 'success') {
    // Remove existing notifications
    const existing = document.querySelector('.mx-toast-success, .mx-toast-error');
    if (existing) {
        existing.remove();
    }

    // Create notification
    const notification = document.createElement('div');
    notification.className = type === 'success' ? 'mx-toast-success' : 'mx-toast-error';
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? '#10b981' : '#ef4444'};
        color: white;
        padding: 16px 24px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        z-index: 10000;
        font-weight: 500;
        animation: slideIn 0.3s ease-out;
    `;
    notification.textContent = message;

    document.body.appendChild(notification);

    // Auto-remove after 3 seconds
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 3000);
}

// Add animation styles
if (!document.getElementById('notification-styles')) {
    const style = document.createElement('style');
    style.id = 'notification-styles';
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
}
