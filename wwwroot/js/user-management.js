// ══════════════════════════════════════════════════════════════════════════════
// User Management JavaScript
// Handles modals, AJAX operations, filters, and user interactions
// ══════════════════════════════════════════════════════════════════════════════

// ── Global State ──────────────────────────────────────────────────────────────
let currentDropdown = null;

// ── Initialize ────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function() {
    initializeFilters();
    initializeDropdowns();
    initializeModals();
});

// ── Filter & Search ───────────────────────────────────────────────────────────
function initializeFilters() {
    const searchInput = document.getElementById('searchInput');
    const roleFilter = document.getElementById('roleFilter');
    const statusFilter = document.getElementById('statusFilter');
    const resetBtn = document.getElementById('reset-filters');

    if (searchInput) {
        searchInput.addEventListener('input', applyFilters);
    }
    if (roleFilter) {
        roleFilter.addEventListener('change', applyFilters);
    }
    if (statusFilter) {
        statusFilter.addEventListener('change', applyFilters);
    }
    if (resetBtn) {
        resetBtn.addEventListener('click', resetFilters);
    }
}

function resetFilters() {
    document.getElementById('searchInput').value = '';
    document.getElementById('roleFilter').value = '';
    document.getElementById('statusFilter').value = '';
    applyFilters();
}

function applyFilters() {
    const searchTerm = document.getElementById('searchInput')?.value.toLowerCase() || '';
    const roleFilter = document.getElementById('roleFilter')?.value.toLowerCase() || '';
    const statusFilter = document.getElementById('statusFilter')?.value.toLowerCase() || '';

    const rows = document.querySelectorAll('#userTable tbody tr');

    rows.forEach(row => {
        const name = row.querySelector('.td-equip')?.textContent.toLowerCase() || '';
        const email = row.querySelectorAll('td')[1]?.textContent.toLowerCase() || '';
        const roles = row.getAttribute('data-role')?.toLowerCase() || '';
        const status = row.getAttribute('data-status')?.toLowerCase() || '';

        const matchesSearch = name.includes(searchTerm) || email.includes(searchTerm);
        const matchesRole = !roleFilter || roles.includes(roleFilter);
        const matchesStatus = !statusFilter || status === statusFilter;

        if (matchesSearch && matchesRole && matchesStatus) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

// ── Dropdown Management ───────────────────────────────────────────────────────
function initializeDropdowns() {
    // Action menu dropdowns
    document.querySelectorAll('.action-trigger').forEach(trigger => {
        trigger.addEventListener('click', function(e) {
            e.stopPropagation();
            const menu = this.closest('.action-menu');
            const dropdown = menu.querySelector('.action-dropdown');
            
            // Close all other dropdowns
            document.querySelectorAll('.action-dropdown').forEach(d => {
                if (d !== dropdown) d.style.display = 'none';
            });
            
            // Toggle current dropdown
            dropdown.style.display = dropdown.style.display === 'block' ? 'none' : 'block';
        });
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.action-menu')) {
            document.querySelectorAll('.action-dropdown').forEach(d => {
                d.style.display = 'none';
            });
        }
    });

    // Prevent dropdown from closing when clicking inside
    document.querySelectorAll('.action-dropdown').forEach(dropdown => {
        dropdown.addEventListener('click', function(e) {
            e.stopPropagation();
        });
    });
}

function closeAllDropdowns() {
    document.querySelectorAll('.action-dropdown').forEach(menu => {
        menu.style.display = 'none';
    });
}

// ── Modal Management ──────────────────────────────────────────────────────────
function initializeModals() {
    // Close modal when clicking outside
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                closeModal(modal.id);
            }
        });
    });

    // Close modal on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.modal').forEach(modal => {
                if (modal.style.display === 'flex') {
                    closeModal(modal.id);
                }
            });
        }
    });
}

function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = '';
        
        // Clear form inputs
        modal.querySelectorAll('input[type="text"], input[type="password"]').forEach(input => {
            input.value = '';
        });
    }
}

// ── Change Role Modal ─────────────────────────────────────────────────────────
function openChangeRoleModal(userId, userName, currentRole) {
    document.getElementById('changeRoleUserId').value = userId;
    document.getElementById('changeRoleUserName').textContent = userName;
    document.getElementById('changeRoleCurrentRole').value = currentRole;
    
    // Set current role as selected
    const roleSelect = document.getElementById('changeRoleNewRole');
    if (roleSelect) {
        roleSelect.value = currentRole.split(',')[0]; // Get first role if multiple
    }
    
    openModal('changeRoleModal');
    closeAllDropdowns();
}

function submitChangeRole() {
    const userId = document.getElementById('changeRoleUserId').value;
    const newRole = document.getElementById('changeRoleNewRole').value;
    const currentRole = document.getElementById('changeRoleCurrentRole').value;

    if (!newRole) {
        showNotification('Please select a role', 'error');
        return;
    }

    if (newRole === currentRole) {
        showNotification('User already has this role', 'warning');
        return;
    }

    // Show loading state
    const submitBtn = event.target;
    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = 'Changing...';

    fetch(`/admin/users/${userId}/change-role`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },
        body: JSON.stringify({
            userId: userId,
            newRole: newRole
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message || 'Role changed successfully', 'success');
            closeModal('changeRoleModal');
            setTimeout(() => location.reload(), 1000);
        } else {
            showNotification(data.message || 'Failed to change role', 'error');
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while changing the role', 'error');
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
    });
}

// ── Reset Password Modal ──────────────────────────────────────────────────────
function openResetPasswordModal(userId, userName, email) {
    document.getElementById('resetPasswordUserId').value = userId;
    document.getElementById('resetPasswordUserName').textContent = userName;
    document.getElementById('resetPasswordEmail').textContent = email;
    document.getElementById('resetPasswordNew').value = '';
    document.getElementById('resetPasswordConfirm').value = '';
    
    openModal('resetPasswordModal');
    closeAllDropdowns();
}

function submitResetPassword() {
    const userId = document.getElementById('resetPasswordUserId').value;
    const newPassword = document.getElementById('resetPasswordNew').value;
    const confirmPassword = document.getElementById('resetPasswordConfirm').value;

    // Validation
    if (!newPassword || !confirmPassword) {
        showNotification('Please fill in all password fields', 'error');
        return;
    }

    if (newPassword.length < 6) {
        showNotification('Password must be at least 6 characters', 'error');
        return;
    }

    if (newPassword !== confirmPassword) {
        showNotification('Passwords do not match', 'error');
        return;
    }

    // Show loading state
    const submitBtn = event.target;
    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = 'Resetting...';

    fetch(`/admin/users/${userId}/reset-password`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },
        body: JSON.stringify({
            userId: userId,
            newPassword: newPassword,
            confirmPassword: confirmPassword
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message || 'Password reset successfully', 'success');
            closeModal('resetPasswordModal');
        } else {
            showNotification(data.message || 'Failed to reset password', 'error');
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while resetting the password', 'error');
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
    });
}

// ── Deactivate User ───────────────────────────────────────────────────────────
function deactivateUser(userId, userName) {
    if (!confirm(`Are you sure you want to deactivate ${userName}?\n\nThis will prevent them from logging in.`)) {
        return;
    }

    closeAllDropdowns();

    fetch(`/admin/users/${userId}/deactivate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message || 'User deactivated successfully', 'success');
            setTimeout(() => location.reload(), 1000);
        } else {
            showNotification(data.message || 'Failed to deactivate user', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while deactivating the user', 'error');
    });
}

// ── Reactivate User ───────────────────────────────────────────────────────────
function reactivateUser(userId, userName) {
    if (!confirm(`Are you sure you want to reactivate ${userName}?`)) {
        return;
    }

    closeAllDropdowns();

    fetch(`/admin/users/${userId}/reactivate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showNotification(data.message || 'User reactivated successfully', 'success');
            setTimeout(() => location.reload(), 1000);
        } else {
            showNotification(data.message || 'Failed to reactivate user', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('An error occurred while reactivating the user', 'error');
    });
}

// ── Notification System ───────────────────────────────────────────────────────
function showNotification(message, type = 'info') {
    // Remove existing notifications
    const existing = document.querySelector('.mx-toast-success');
    if (existing) {
        existing.remove();
    }

    // Create notification
    const notification = document.createElement('div');
    notification.className = 'mx-toast-success';
    
    const icon = type === 'success' ? '✅' : type === 'error' ? '❌' : type === 'warning' ? '⚠️' : 'ℹ️';
    const color = type === 'error' ? '#EF4444' : type === 'warning' ? '#F47920' : '#22C55E';
    
    notification.style.borderLeftColor = color;
    notification.innerHTML = `<span class="toast-icon">${icon}</span>${message}`;

    document.body.appendChild(notification);

    // Animate in
    setTimeout(() => notification.style.opacity = '1', 10);

    // Auto remove after 4 seconds
    setTimeout(() => {
        notification.style.opacity = '0';
        setTimeout(() => notification.remove(), 300);
    }, 4000);
}
