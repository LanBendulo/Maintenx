// Maintenance Requests Page - Database Integration

(function () {
    'use strict';

    // ========================================
    // MODAL MANAGEMENT
    // ========================================
    const overlay = document.getElementById('mrModal');
    const openBtn = document.getElementById('openMrModal');
    const closeBtn = document.getElementById('closeMrModal');
    const cancelBtn = document.getElementById('cancelMrModal');
    const submitBtn = document.getElementById('submitMrForm');
    const form = document.getElementById('mrForm');

    function openModal() {
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
        loadAssets();
    }

    function closeModal() {
        overlay.classList.remove('open');
        document.body.style.overflow = '';
        form.reset();
        clearErrors();
    }

    openBtn.addEventListener('click', openModal);
    closeBtn.addEventListener('click', closeModal);
    cancelBtn.addEventListener('click', closeModal);
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeModal();
    });

    // ========================================
    // LOAD ASSETS
    // ========================================
    async function loadAssets() {
        console.log('[ASSET LOADING] Starting asset load...');
        
        try {
            const response = await fetch('/admin/maintenance-requests/available-assets');
            
            console.log('[ASSET LOADING] Response status:', response.status);
            console.log('[ASSET LOADING] Response ok:', response.ok);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('[ASSET LOADING] Error response:', errorText);
                throw new Error(`Failed to load assets: ${response.status} ${response.statusText}`);
            }
            
            const assets = await response.json();
            console.log('[ASSET LOADING] Loaded assets:', assets.length);
            
            const select = document.getElementById('mr-asset');
            
            select.innerHTML = '<option value="">Select equipment…</option>';
            
            if (assets.length === 0) {
                console.warn('[ASSET LOADING] No assets available');
                const option = document.createElement('option');
                option.value = '';
                option.textContent = 'No equipment available';
                option.disabled = true;
                select.appendChild(option);
            } else {
                assets.forEach(asset => {
                    const option = document.createElement('option');
                    option.value = asset.value;
                    option.textContent = asset.text;
                    select.appendChild(option);
                });
                console.log('[ASSET LOADING] Successfully populated dropdown');
            }
        } catch (error) {
            console.error('[ASSET LOADING] Error loading assets:', error);
            showToast('Failed to load equipment list', 'error');
        }
    }

    // ========================================
    // FORM VALIDATION
    // ========================================
    function validateForm() {
        let isValid = true;
        clearErrors();

        const title = document.getElementById('mr-title');
        if (!title.value.trim()) {
            showError('err-title');
            isValid = false;
        }

        const asset = document.getElementById('mr-asset');
        if (!asset.value) {
            showError('err-asset');
            isValid = false;
        }

        const description = document.getElementById('mr-description');
        if (!description.value.trim()) {
            showError('err-description');
            isValid = false;
        }

        return isValid;
    }

    function showError(errorId) {
        const errorElement = document.getElementById(errorId);
        if (errorElement) {
            errorElement.style.display = 'block';
        }
    }

    function clearErrors() {
        document.querySelectorAll('.input-error').forEach(el => {
            el.style.display = 'none';
        });
    }

    // ========================================
    // SUBMIT REQUEST
    // ========================================
    submitBtn.addEventListener('click', async function (e) {
        e.preventDefault();

        if (!validateForm()) {
            return;
        }

        const priority = document.querySelector('input[name="mr-priority"]:checked');
        const attachmentFile = document.getElementById('mr-attachment').files[0];
        
        // Create FormData for file upload
        const formData = new FormData();
        formData.append('Title', document.getElementById('mr-title').value.trim());
        formData.append('AssetId', document.getElementById('mr-asset').value);
        formData.append('Description', document.getElementById('mr-description').value.trim());
        formData.append('Priority', priority ? priority.value : 'Medium');
        
        const category = document.getElementById('mr-category').value;
        if (category) {
            formData.append('Category', category);
        }
        
        const location = document.getElementById('mr-location').value.trim();
        if (location) {
            formData.append('Location', location);
        }
        
        if (attachmentFile) {
            formData.append('Attachment', attachmentFile);
        }

        submitBtn.disabled = true;
        submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> Submitting...';

        try {
            const response = await fetch('/admin/maintenance-requests/create', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (response.ok && result.success) {
                closeModal();
                showToast(result.message || 'Request submitted successfully!', 'success');
                
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                console.error('Server error:', result);
                const errorMessage = result.errors ? result.errors.join(', ') : result.message || 'Failed to submit request';
                showToast(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Error submitting request:', error);
            showToast('An error occurred. Please try again.', 'error');
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Submit Request';
        }
    });

    // ========================================
    // VIEW DETAILS
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-view-mr')) {
            e.preventDefault();
            const link = e.target.closest('.action-view-mr');
            const mrId = link.getAttribute('data-mr-id');
            
            try {
                const response = await fetch(`/admin/maintenance-requests/${mrId}`);
                if (!response.ok) throw new Error('Failed to load request');
                
                const mr = await response.json();
                
                document.getElementById('mr-details-number').textContent = mr.requestNumber;
                document.getElementById('mr-details-title').textContent = mr.title || 'N/A';
                document.getElementById('mr-details-asset').textContent = mr.assetName || 'N/A';
                document.getElementById('mr-details-description').textContent = mr.description || 'No description';
                document.getElementById('mr-details-priority').textContent = mr.priority || 'N/A';
                document.getElementById('mr-details-status').textContent = mr.status || 'N/A';
                document.getElementById('mr-details-requested-by').textContent = mr.requestedBy || 'N/A';
                document.getElementById('mr-details-category').textContent = mr.category || 'N/A';
                document.getElementById('mr-details-location').textContent = mr.location || 'N/A';
                document.getElementById('mr-details-created').textContent = mr.createdAt ? new Date(mr.createdAt).toLocaleDateString() : 'N/A';
                
                // Handle attachment
                const attachmentContainer = document.getElementById('mr-details-attachment-container');
                const attachmentLink = document.getElementById('mr-details-attachment');
                if (mr.attachmentUrl) {
                    attachmentLink.href = mr.attachmentUrl;
                    attachmentContainer.style.display = 'block';
                } else {
                    attachmentContainer.style.display = 'none';
                }
                
                document.getElementById('mrDetailsModal').classList.add('open');
                document.body.style.overflow = 'hidden';
            } catch (error) {
                console.error('Error loading request:', error);
                showToast('Failed to load request details', 'error');
            }
        }
    });

    const closeMrDetailsModal = () => {
        document.getElementById('mrDetailsModal').classList.remove('open');
        document.body.style.overflow = '';
    };
    
    document.getElementById('closeMrDetailsModal')?.addEventListener('click', closeMrDetailsModal);
    document.getElementById('closeMrDetailsBtn')?.addEventListener('click', closeMrDetailsModal);

    // ========================================
    // APPROVE REQUEST
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-approve-mr')) {
            e.preventDefault();
            const link = e.target.closest('.action-approve-mr');
            const mrId = link.getAttribute('data-mr-id');
            
            if (!confirm('Are you sure you want to approve this request?')) return;
            
            try {
                const response = await fetch(`/admin/maintenance-requests/${mrId}/approve`, {
                    method: 'PUT'
                });
                
                if (!response.ok) throw new Error('Failed to approve request');
                
                showToast('Request approved successfully!', 'success');
                setTimeout(() => window.location.reload(), 1500);
            } catch (error) {
                console.error('Error approving request:', error);
                showToast('Failed to approve request', 'error');
            }
        }
    });

    // ========================================
    // REJECT REQUEST
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-reject-mr')) {
            e.preventDefault();
            const link = e.target.closest('.action-reject-mr');
            const mrId = link.getAttribute('data-mr-id');
            
            if (!confirm('Are you sure you want to reject this request?')) return;
            
            try {
                const response = await fetch(`/admin/maintenance-requests/${mrId}/reject`, {
                    method: 'PUT'
                });
                
                if (!response.ok) throw new Error('Failed to reject request');
                
                showToast('Request rejected successfully!', 'success');
                setTimeout(() => window.location.reload(), 1500);
            } catch (error) {
                console.error('Error rejecting request:', error);
                showToast('Failed to reject request', 'error');
            }
        }
    });

    // ========================================
    // CLOSE REQUEST
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-close-mr')) {
            e.preventDefault();
            const link = e.target.closest('.action-close-mr');
            const mrId = link.getAttribute('data-mr-id');
            
            if (!confirm('Are you sure you want to close this request without conversion?')) return;
            
            try {
                const response = await fetch(`/admin/maintenance-requests/${mrId}/close`, {
                    method: 'PUT'
                });
                
                const result = await response.json();
                
                if (response.ok && result.success) {
                    showToast(result.message || 'Request closed successfully!', 'success');
                    setTimeout(() => window.location.reload(), 1500);
                } else {
                    showToast(result.message || 'Failed to close request', 'error');
                }
            } catch (error) {
                console.error('Error closing request:', error);
                showToast('Failed to close request', 'error');
            }
        }
    });

    // ========================================
    // CONVERT TO WORK ORDER
    // ========================================
    // ========================================
    // CONVERT TO WORK ORDER
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-convert-mr')) {
            e.preventDefault();
            const link = e.target.closest('.action-convert-mr');
            const mrId = link.getAttribute('data-mr-id');
            
            console.log('=== CONVERT REQUEST TO WORK ORDER ===');
            console.log('Request ID:', mrId);
            
            // Redirect to work orders page with convertRequestId parameter
            window.location.href = `/admin/work-orders?convertRequestId=${mrId}`;
        }
    });

    // ========================================
    // TOAST NOTIFICATIONS
    // ========================================
    function showToast(message, type = 'success') {
        const toast = document.getElementById('mr-toast');
        if (!toast) return;

        toast.textContent = '';
        
        const icon = document.createElement('span');
        icon.className = 'toast-icon';
        icon.textContent = type === 'success' ? '✅' : '❌';
        toast.appendChild(icon);
        
        const messageText = document.createTextNode(message);
        toast.appendChild(messageText);
        
        toast.className = type === 'success' ? 'mx-toast-success' : 'mx-toast-error';
        toast.classList.add('show');

        setTimeout(() => {
            toast.classList.remove('show');
        }, 3000);
    }

    // ========================================
    // FILTER FUNCTIONALITY
    // ========================================
    const searchInput = document.getElementById('mr-search');
    const statusFilter = document.getElementById('filter-status');
    const priorityFilter = document.getElementById('filter-priority');
    const resetBtn = document.getElementById('reset-filters');
    const tableBody = document.getElementById('mr-tbody');

    function filterTable() {
        const searchTerm = searchInput.value.toLowerCase();
        const statusValue = statusFilter.value;
        const priorityValue = priorityFilter.value;

        const rows = tableBody.querySelectorAll('tr');
        let visibleCount = 0;

        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const rowStatus = row.getAttribute('data-status');
            const rowPriority = row.getAttribute('data-priority');

            const matchesSearch = text.includes(searchTerm);
            const matchesStatus = !statusValue || rowStatus === statusValue;
            const matchesPriority = !priorityValue || rowPriority === priorityValue;

            if (matchesSearch && matchesStatus && matchesPriority) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        const countElement = document.getElementById('row-count');
        if (countElement) {
            countElement.innerHTML = `Showing <strong>${visibleCount}</strong> result${visibleCount !== 1 ? 's' : ''}`;
        }
    }

    if (searchInput) searchInput.addEventListener('input', filterTable);
    if (statusFilter) statusFilter.addEventListener('change', filterTable);
    if (priorityFilter) priorityFilter.addEventListener('change', filterTable);

    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            searchInput.value = '';
            statusFilter.value = '';
            priorityFilter.value = '';
            filterTable();
        });
    }

    // ========================================
    // ACTION MENU DROPDOWNS
    // ========================================
    document.querySelectorAll('.action-trigger').forEach(trigger => {
        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            const dropdown = this.nextElementSibling;
            
            document.querySelectorAll('.action-dropdown').forEach(dd => {
                if (dd !== dropdown) dd.classList.remove('show');
            });
            
            dropdown.classList.toggle('show');
        });
    });

    document.addEventListener('click', () => {
        document.querySelectorAll('.action-dropdown').forEach(dd => {
            dd.classList.remove('show');
        });
    });

})();


// ========================================
// ARCHIVE / UNARCHIVE FUNCTIONALITY
// ========================================

// Archive filter dropdown
document.getElementById('archiveFilterSelect')?.addEventListener('change', function() {
    const filter = this.value;
    window.location.href = `/admin/maintenance-requests?filter=${filter}`;
});

// Archive request
document.addEventListener('click', async function(e) {
    if (e.target.closest('.action-archive-mr')) {
        e.preventDefault();
        const link = e.target.closest('.action-archive-mr');
        const mrId = link.getAttribute('data-mr-id');
        
        if (!confirm('Are you sure you want to archive this request? Only rejected or converted requests can be archived.')) {
            return;
        }
        
        try {
            const response = await fetch(`/admin/maintenance-requests/${mrId}/archive`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            const result = await response.json();
            
            if (response.ok && result.success) {
                showToast(result.message || 'Request archived successfully!', 'success');
                setTimeout(() => window.location.reload(), 1500);
            } else {
                showToast(result.message || 'Failed to archive request', 'error');
            }
        } catch (error) {
            console.error('Error archiving request:', error);
            showToast('An error occurred while archiving the request', 'error');
        }
    }
});

// Unarchive (Restore) request
document.addEventListener('click', async function(e) {
    if (e.target.closest('.action-unarchive-mr')) {
        e.preventDefault();
        const link = e.target.closest('.action-unarchive-mr');
        const mrId = link.getAttribute('data-mr-id');
        
        if (!confirm('Are you sure you want to restore this request from the archive?')) {
            return;
        }
        
        try {
            const response = await fetch(`/admin/maintenance-requests/${mrId}/unarchive`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            const result = await response.json();
            
            if (response.ok && result.success) {
                showToast(result.message || 'Request restored successfully!', 'success');
                setTimeout(() => window.location.reload(), 1500);
            } else {
                showToast(result.message || 'Failed to restore request', 'error');
            }
        } catch (error) {
            console.error('Error restoring request:', error);
            showToast('An error occurred while restoring the request', 'error');
        }
    }
});
