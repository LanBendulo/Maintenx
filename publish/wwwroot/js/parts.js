// ═══════════════════════════════════════════════════════════
// PARTS INVENTORY MANAGEMENT
// ═══════════════════════════════════════════════════════════

(function() {
    'use strict';

    let currentPartId = null;
    let isEditMode = false;

    // ═══════════════════════════════════════════════════════════
    // FILTERS
    // ═══════════════════════════════════════════════════════════

    // Search filter
    document.getElementById('part-search')?.addEventListener('input', function() {
        applyFilters();
    });

    // Status filter
    document.getElementById('filter-status')?.addEventListener('change', function() {
        applyFilters();
    });

    // Low stock filter
    document.getElementById('filter-low-stock')?.addEventListener('change', function() {
        applyFilters();
    });

    // Reset filters
    document.getElementById('reset-filters')?.addEventListener('click', function() {
        document.getElementById('part-search').value = '';
        document.getElementById('filter-status').value = 'all';
        document.getElementById('filter-low-stock').checked = false;
        applyFilters();
    });

    function applyFilters() {
        const searchTerm = document.getElementById('part-search').value.toLowerCase();
        const statusFilter = document.getElementById('filter-status').value;
        const lowStockOnly = document.getElementById('filter-low-stock').checked;
        const rows = document.querySelectorAll('#parts-tbody tr');
        let visibleCount = 0;

        rows.forEach(function(row) {
            if (row.cells.length === 1) return; // Skip empty state row

            const text = row.textContent.toLowerCase();
            const status = row.dataset.status;
            const isLowStock = row.dataset.lowStock === 'true';

            let show = true;

            // Search filter
            if (searchTerm && !text.includes(searchTerm)) {
                show = false;
            }

            // Status filter
            if (statusFilter !== 'all') {
                if (statusFilter === 'active' && status !== 'done') show = false;
                if (statusFilter === 'inactive' && status !== 'cancelled') show = false;
            }

            // Low stock filter
            if (lowStockOnly && !isLowStock) {
                show = false;
            }

            row.style.display = show ? '' : 'none';
            if (show) visibleCount++;
        });

        document.querySelector('#row-count strong').textContent = visibleCount;
    }

    // ═══════════════════════════════════════════════════════════
    // ACTION MENU TOGGLE
    // ═══════════════════════════════════════════════════════════

    document.querySelectorAll('.action-trigger').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            const dropdown = this.nextElementSibling;
            const isOpen = dropdown.classList.contains('show');
            
            // Close all dropdowns
            document.querySelectorAll('.action-dropdown').forEach(function(d) {
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
        document.querySelectorAll('.action-dropdown').forEach(function(d) {
            d.classList.remove('show');
        });
    });

    // ═══════════════════════════════════════════════════════════
    // CREATE PART MODAL
    // ═══════════════════════════════════════════════════════════

    document.getElementById('openPartModal')?.addEventListener('click', function() {
        isEditMode = false;
        currentPartId = null;
        document.getElementById('part-modal-title').textContent = 'Add Part';
        document.getElementById('partForm').reset();
        document.getElementById('partId').value = '';
        openModal('partModal');
    });

    document.getElementById('closePartModal')?.addEventListener('click', function() {
        closeModal('partModal');
    });

    document.getElementById('cancelPartBtn')?.addEventListener('click', function() {
        closeModal('partModal');
    });

    // ═══════════════════════════════════════════════════════════
    // EDIT PART
    // ═══════════════════════════════════════════════════════════

    document.querySelectorAll('.action-edit-part').forEach(function(link) {
        link.addEventListener('click', async function(e) {
            e.preventDefault();
            const partId = this.dataset.partId;
            
            try {
                const response = await fetch(`/admin/parts/${partId}`);
                const data = await response.json();
                
                if (data) {
                    isEditMode = true;
                    currentPartId = partId;
                    
                    document.getElementById('part-modal-title').textContent = 'Edit Part';
                    document.getElementById('partId').value = data.partId;
                    document.getElementById('partName').value = data.partName || '';
                    document.getElementById('partNumber').value = data.partNumber || '';
                    document.getElementById('description').value = data.description || '';
                    document.getElementById('quantity').value = data.quantity || 0;
                    document.getElementById('unitCost').value = data.unitCost || '';
                    document.getElementById('reorderLevel').value = data.reorderLevel || '';
                    document.getElementById('location').value = data.location || '';
                    
                    openModal('partModal');
                }
            } catch (error) {
                showToast('Failed to load part details', false);
                console.error(error);
            }
        });
    });

    // ═══════════════════════════════════════════════════════════
    // SAVE PART (CREATE/UPDATE)
    // ═══════════════════════════════════════════════════════════

    document.getElementById('savePartBtn')?.addEventListener('click', async function() {
        // Clear previous errors
        document.querySelectorAll('.input-error').forEach(el => el.style.display = 'none');

        const partName = document.getElementById('partName').value.trim();
        const partNumber = document.getElementById('partNumber').value.trim();
        const description = document.getElementById('description').value.trim();
        const quantity = parseInt(document.getElementById('quantity').value) || 0;
        const unitCost = parseFloat(document.getElementById('unitCost').value) || null;
        const reorderLevel = parseInt(document.getElementById('reorderLevel').value) || null;
        const location = document.getElementById('location').value.trim();

        // Validation
        let hasError = false;

        if (!partName) {
            document.getElementById('err-part-name').style.display = 'block';
            hasError = true;
        }

        if (quantity < 0) {
            document.getElementById('err-quantity').textContent = 'Quantity cannot be negative';
            document.getElementById('err-quantity').style.display = 'block';
            hasError = true;
        }

        if (hasError) {
            return;
        }

        const partData = {
            partName: partName,
            partNumber: partNumber || null,
            description: description || null,
            quantity: quantity,
            unitCost: unitCost,
            reorderLevel: reorderLevel,
            location: location || null
        };

        try {
            let response;
            
            if (isEditMode && currentPartId) {
                // Update existing part
                response = await fetch(`/admin/parts/${currentPartId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(partData)
                });
            } else {
                // Create new part
                response = await fetch('/admin/parts/create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(partData)
                });
            }

            const result = await response.json();

            if (result.success) {
                showToast(result.message, true);
                closeModal('partModal');
                setTimeout(function() { location.reload(); }, 1500);
            } else {
                showToast(result.message || 'Operation failed', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    });

    // ═══════════════════════════════════════════════════════════
    // ADJUST QUANTITY
    // ═══════════════════════════════════════════════════════════

    document.querySelectorAll('.action-adjust-qty').forEach(function(link) {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const partId = this.dataset.partId;
            const partName = this.dataset.partName;
            const currentQty = this.dataset.currentQty;
            
            document.getElementById('adjustPartId').value = partId;
            document.getElementById('adjust-part-name').textContent = partName;
            document.getElementById('adjust-current-qty').textContent = currentQty;
            document.getElementById('adjustmentAmount').value = '';
            document.getElementById('adjustmentReason').value = '';
            
            openModal('adjustQtyModal');
        });
    });

    document.getElementById('closeAdjustQtyModal')?.addEventListener('click', function() {
        closeModal('adjustQtyModal');
    });

    document.getElementById('cancelAdjustBtn')?.addEventListener('click', function() {
        closeModal('adjustQtyModal');
    });

    document.getElementById('saveAdjustBtn')?.addEventListener('click', async function() {
        // Clear previous errors
        document.querySelectorAll('.input-error').forEach(el => el.style.display = 'none');

        const partId = document.getElementById('adjustPartId').value;
        const adjustmentAmount = parseInt(document.getElementById('adjustmentAmount').value);
        const reason = document.getElementById('adjustmentReason').value.trim();

        // Validation
        if (isNaN(adjustmentAmount) || adjustmentAmount === 0) {
            document.getElementById('err-adjustment').textContent = 'Please enter a valid adjustment amount (cannot be zero)';
            document.getElementById('err-adjustment').style.display = 'block';
            return;
        }

        try {
            const response = await fetch(`/admin/parts/${partId}/adjust-quantity`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    adjustmentAmount: adjustmentAmount,
                    reason: reason || null
                })
            });

            const result = await response.json();

            if (result.success) {
                showToast(result.message, true);
                closeModal('adjustQtyModal');
                setTimeout(function() { location.reload(); }, 1500);
            } else {
                showToast(result.message || 'Adjustment failed', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    });

    // ═══════════════════════════════════════════════════════════
    // TOGGLE STATUS
    // ═══════════════════════════════════════════════════════════

    document.querySelectorAll('.action-toggle-status').forEach(function(link) {
        link.addEventListener('click', async function(e) {
            e.preventDefault();
            const partId = this.dataset.partId;
            const currentStatus = this.dataset.currentStatus === 'true';
            const action = currentStatus ? 'deactivate' : 'activate';
            
            if (!confirm(`Are you sure you want to ${action} this part?`)) {
                return;
            }

            try {
                const response = await fetch(`/admin/parts/${partId}/toggle-status`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' }
                });

                const result = await response.json();

                if (result.success) {
                    showToast(result.message, true);
                    setTimeout(function() { location.reload(); }, 1500);
                } else {
                    showToast(result.message || 'Status toggle failed', false);
                }
            } catch (error) {
                showToast('An error occurred', false);
                console.error(error);
            }
        });
    });

    // ═══════════════════════════════════════════════════════════
    // MODAL FUNCTIONS
    // ═══════════════════════════════════════════════════════════

    function openModal(id) {
        document.getElementById(id).classList.add('open');
    }

    function closeModal(id) {
        document.getElementById(id).classList.remove('open');
    }

    // Close modal on outside click
    window.addEventListener('click', function(event) {
        if (event.target.classList.contains('mx-modal-overlay')) {
            event.target.classList.remove('open');
        }
    });

    // ═══════════════════════════════════════════════════════════
    // TOAST FUNCTION
    // ═══════════════════════════════════════════════════════════

    function showToast(message, success) {
        const toast = document.getElementById('part-toast');
        const messageEl = document.getElementById('toast-message');
        
        messageEl.textContent = message;
        toast.style.borderLeftColor = success ? '#22C55E' : '#EF4444';
        toast.querySelector('.toast-icon').textContent = success ? '✅' : '❌';
        
        toast.classList.add('show');
        
        setTimeout(function() {
            toast.classList.remove('show');
        }, 3000);
    }

})();
