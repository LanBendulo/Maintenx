/**
 * ═══════════════════════════════════════════════════════════════════════════════
 * TECHNICIAN PARTS WORKFLOW
 * Lightweight parts staging UI for technicians
 * Integrates with existing work order details modal
 * ═══════════════════════════════════════════════════════════════════════════════
 */

(function() {
    'use strict';

    // ═══════════════════════════════════════════════════════════════════════════
    // STATE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    
    let currentWorkOrderId = null;
    let currentWorkOrderStatus = null;
    let availableParts = [];
    let stagedParts = [];
    let editingPartId = null;

    // ═══════════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════════

    // Hook into existing view details functionality
    const originalViewDetails = document.querySelectorAll('.action-view-details');
    originalViewDetails.forEach(function(link) {
        link.addEventListener('click', function(e) {
            const woId = this.dataset.woId;
            currentWorkOrderId = woId;
            
            // Load parts after a short delay to let the modal open
            setTimeout(function() {
                loadWorkOrderParts(woId);
                loadAvailableParts();
            }, 300);
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // LOAD PARTS DATA
    // ═══════════════════════════════════════════════════════════════════════════

    async function loadWorkOrderParts(workOrderId) {
        const container = document.getElementById('partsListContainer');
        
        try {
            const response = await fetch(`/dashboard/work-orders/${workOrderId}/parts`);
            const data = await response.json();
            
            if (data.success) {
                stagedParts = data.parts || [];
                renderPartsList(stagedParts);
                
                // Show/hide Add Part button based on work order status
                const detailsStatus = document.getElementById('details-status').textContent.trim();
                currentWorkOrderStatus = detailsStatus;
                const canAddParts = detailsStatus === 'Pending' || detailsStatus === 'In Progress';
                document.getElementById('btnAddPart').style.display = canAddParts ? 'inline-flex' : 'none';
            } else {
                container.innerHTML = `<div style="text-align:center;padding:20px;color:#EF4444;font-size:13px;">${data.message || 'Failed to load parts'}</div>`;
            }
        } catch (error) {
            console.error('Error loading parts:', error);
            container.innerHTML = '<div style="text-align:center;padding:20px;color:#EF4444;font-size:13px;">Error loading parts</div>';
        }
    }

    async function loadAvailableParts() {
        try {
            const response = await fetch('/dashboard/parts/available');
            const data = await response.json();
            
            if (data.success) {
                availableParts = data.parts || [];
                populatePartSelect();
            }
        } catch (error) {
            console.error('Error loading available parts:', error);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RENDER PARTS LIST
    // ═══════════════════════════════════════════════════════════════════════════

    function renderPartsList(parts) {
        const container = document.getElementById('partsListContainer');
        
        if (!parts || parts.length === 0) {
            container.innerHTML = `
                <div style="text-align:center;padding:20px;color:var(--mx-muted);font-size:13px;">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:32px;height:32px;margin:0 auto 8px;opacity:0.3;">
                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><line x1="9" y1="9" x2="15" y2="15"/><line x1="15" y1="9" x2="9" y2="15"/>
                    </svg>
                    <p>No parts added yet</p>
                </div>
            `;
            return;
        }

        let html = '<div style="overflow-x:auto;"><table style="width:100%;font-size:13px;border-collapse:collapse;">';
        html += '<thead><tr style="border-bottom:1.5px solid var(--mx-border);text-align:left;">';
        html += '<th style="padding:8px 12px;font-weight:600;color:var(--mx-muted);font-size:11px;text-transform:uppercase;letter-spacing:0.5px;">Part</th>';
        html += '<th style="padding:8px 12px;font-weight:600;color:var(--mx-muted);font-size:11px;text-transform:uppercase;letter-spacing:0.5px;">Quantity</th>';
        html += '<th style="padding:8px 12px;font-weight:600;color:var(--mx-muted);font-size:11px;text-transform:uppercase;letter-spacing:0.5px;">Status</th>';
        html += '<th style="padding:8px 12px;font-weight:600;color:var(--mx-muted);font-size:11px;text-transform:uppercase;letter-spacing:0.5px;">Added By</th>';
        html += '<th style="padding:8px 12px;font-weight:600;color:var(--mx-muted);font-size:11px;text-transform:uppercase;letter-spacing:0.5px;text-align:right;">Actions</th>';
        html += '</tr></thead><tbody>';

        parts.forEach(function(part) {
            const statusBadge = getStatusBadge(part.usageStatus);
            const canEdit = part.canEdit;
            
            html += '<tr style="border-bottom:1px solid var(--mx-border);">';
            html += `<td style="padding:10px 12px;">
                        <div style="font-weight:500;color:var(--mx-text);">${escapeHtml(part.partName)}</div>
                        ${part.partNumber ? `<div style="font-size:11px;color:var(--mx-muted);margin-top:2px;">${escapeHtml(part.partNumber)}</div>` : ''}
                     </td>`;
            html += `<td style="padding:10px 12px;font-weight:500;">${part.quantityUsed}</td>`;
            html += `<td style="padding:10px 12px;">${statusBadge}</td>`;
            html += `<td style="padding:10px 12px;color:var(--mx-muted);">${escapeHtml(part.addedBy || 'N/A')}</td>`;
            html += '<td style="padding:10px 12px;text-align:right;">';
            
            if (canEdit) {
                html += `<button class="btn-edit-part" data-part-id="${part.id}" data-part-name="${escapeHtml(part.partName)}" data-quantity="${part.quantityUsed}" 
                                style="padding:4px 8px;margin-right:4px;font-size:11px;background:var(--mx-blue);color:white;border:none;border-radius:4px;cursor:pointer;">
                            Edit
                         </button>`;
                html += `<button class="btn-remove-part" data-part-id="${part.id}" data-part-name="${escapeHtml(part.partName)}"
                                style="padding:4px 8px;font-size:11px;background:#EF4444;color:white;border:none;border-radius:4px;cursor:pointer;">
                            Remove
                         </button>`;
            } else {
                html += '<span style="font-size:11px;color:var(--mx-muted);">—</span>';
            }
            
            html += '</td></tr>';
        });

        html += '</tbody></table></div>';
        container.innerHTML = html;

        // Attach event listeners
        attachPartActionListeners();
    }

    function getStatusBadge(status) {
        const badges = {
            'Pending': '<span style="display:inline-block;padding:3px 8px;font-size:11px;font-weight:600;border-radius:4px;background:rgba(251,146,60,.15);color:#F97316;">Pending</span>',
            'Consumed': '<span style="display:inline-block;padding:3px 8px;font-size:11px;font-weight:600;border-radius:4px;background:rgba(34,197,94,.15);color:#22C55E;">Consumed</span>',
            'Cancelled': '<span style="display:inline-block;padding:3px 8px;font-size:11px;font-weight:600;border-radius:4px;background:rgba(148,163,184,.15);color:#94A3B8;">Cancelled</span>'
        };
        return badges[status] || status;
    }

    function attachPartActionListeners() {
        // Edit buttons
        document.querySelectorAll('.btn-edit-part').forEach(function(btn) {
            btn.addEventListener('click', function() {
                const partId = this.dataset.partId;
                const partName = this.dataset.partName;
                const quantity = this.dataset.quantity;
                openEditQuantityModal(partId, partName, quantity);
            });
        });

        // Remove buttons
        document.querySelectorAll('.btn-remove-part').forEach(function(btn) {
            btn.addEventListener('click', function() {
                const partId = this.dataset.partId;
                const partName = this.dataset.partName;
                confirmRemovePart(partId, partName);
            });
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ADD PART MODAL
    // ═══════════════════════════════════════════════════════════════════════════

    document.getElementById('btnAddPart').addEventListener('click', function() {
        openAddPartModal();
    });

    function openAddPartModal() {
        document.getElementById('addPartModal').classList.add('show');
        document.getElementById('partQuantity').value = '';
        document.getElementById('quantityError').style.display = 'none';
        document.getElementById('partStockInfo').style.display = 'none';
    }

    function closeAddPartModal() {
        document.getElementById('addPartModal').classList.remove('show');
    }

    function populatePartSelect() {
        const select = document.getElementById('partSelect');
        
        if (availableParts.length === 0) {
            select.innerHTML = '<option value="">No parts available</option>';
            return;
        }

        let html = '<option value="">Select a part...</option>';
        availableParts.forEach(function(part) {
            const label = part.partNumber 
                ? `${part.partName} (${part.partNumber}) - ${part.quantity} available`
                : `${part.partName} - ${part.quantity} available`;
            html += `<option value="${part.partId}" data-quantity="${part.quantity}" data-location="${escapeHtml(part.location || '')}">${escapeHtml(label)}</option>`;
        });
        
        select.innerHTML = html;
    }

    // Part selection change - show stock info
    document.getElementById('partSelect').addEventListener('change', function() {
        const selectedOption = this.options[this.selectedIndex];
        const stockInfo = document.getElementById('partStockInfo');
        
        if (this.value) {
            const quantity = selectedOption.dataset.quantity;
            const location = selectedOption.dataset.location;
            
            document.getElementById('partStockQty').textContent = quantity;
            document.getElementById('partLocation').textContent = location ? `Location: ${location}` : '';
            stockInfo.style.display = 'block';
        } else {
            stockInfo.style.display = 'none';
        }
        
        document.getElementById('quantityError').style.display = 'none';
    });

    // Quantity input validation
    document.getElementById('partQuantity').addEventListener('input', function() {
        const selectedOption = document.getElementById('partSelect').options[document.getElementById('partSelect').selectedIndex];
        const availableQty = parseInt(selectedOption.dataset.quantity || 0);
        const requestedQty = parseInt(this.value || 0);
        const errorDiv = document.getElementById('quantityError');
        
        if (requestedQty > availableQty) {
            errorDiv.textContent = `Only ${availableQty} units available in stock`;
            errorDiv.style.display = 'block';
        } else {
            errorDiv.style.display = 'none';
        }
    });

    // Confirm add part
    document.getElementById('confirmAddPart').addEventListener('click', async function() {
        const partId = document.getElementById('partSelect').value;
        const quantity = parseInt(document.getElementById('partQuantity').value || 0);
        
        if (!partId) {
            showToast('Please select a part', false);
            return;
        }
        
        if (quantity <= 0) {
            showToast('Please enter a valid quantity', false);
            return;
        }

        const selectedOption = document.getElementById('partSelect').options[document.getElementById('partSelect').selectedIndex];
        const availableQty = parseInt(selectedOption.dataset.quantity || 0);
        
        if (quantity > availableQty) {
            showToast(`Only ${availableQty} units available`, false);
            return;
        }

        try {
            const response = await fetch(`/dashboard/work-orders/${currentWorkOrderId}/add-part`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ partId: parseInt(partId), quantity: quantity })
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message || 'Part added successfully', true);
                closeAddPartModal();
                loadWorkOrderParts(currentWorkOrderId);
            } else {
                showToast(result.message || 'Failed to add part', false);
            }
        } catch (error) {
            console.error('Error adding part:', error);
            showToast('An error occurred while adding the part', false);
        }
    });

    // Cancel add part
    document.getElementById('cancelAddPart').addEventListener('click', closeAddPartModal);
    document.getElementById('closeAddPartModal').addEventListener('click', closeAddPartModal);

    // ═══════════════════════════════════════════════════════════════════════════
    // EDIT QUANTITY MODAL
    // ═══════════════════════════════════════════════════════════════════════════

    function openEditQuantityModal(partId, partName, currentQuantity) {
        editingPartId = partId;
        document.getElementById('edit-qty-part-name').textContent = partName;
        document.getElementById('editPartQuantity').value = currentQuantity;
        document.getElementById('editQuantityError').style.display = 'none';
        document.getElementById('editQuantityModal').classList.add('show');
    }

    function closeEditQuantityModal() {
        document.getElementById('editQuantityModal').classList.remove('show');
        editingPartId = null;
    }

    // Confirm edit quantity
    document.getElementById('confirmEditQuantity').addEventListener('click', async function() {
        const newQuantity = parseInt(document.getElementById('editPartQuantity').value || 0);
        
        if (newQuantity <= 0) {
            showToast('Please enter a valid quantity', false);
            return;
        }

        try {
            const response = await fetch(`/dashboard/work-orders/${currentWorkOrderId}/parts/${editingPartId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ quantity: newQuantity })
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message || 'Quantity updated successfully', true);
                closeEditQuantityModal();
                loadWorkOrderParts(currentWorkOrderId);
            } else {
                showToast(result.message || 'Failed to update quantity', false);
            }
        } catch (error) {
            console.error('Error updating quantity:', error);
            showToast('An error occurred while updating quantity', false);
        }
    });

    // Cancel edit quantity
    document.getElementById('cancelEditQuantity').addEventListener('click', closeEditQuantityModal);
    document.getElementById('closeEditQuantityModal').addEventListener('click', closeEditQuantityModal);

    // ═══════════════════════════════════════════════════════════════════════════
    // REMOVE PART
    // ═══════════════════════════════════════════════════════════════════════════

    function confirmRemovePart(partId, partName) {
        if (!confirm(`Remove ${partName} from this work order?`)) {
            return;
        }
        
        removePart(partId);
    }

    async function removePart(partId) {
        try {
            const response = await fetch(`/dashboard/work-orders/${currentWorkOrderId}/remove-part/${partId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message || 'Part removed successfully', true);
                loadWorkOrderParts(currentWorkOrderId);
            } else {
                showToast(result.message || 'Failed to remove part', false);
            }
        } catch (error) {
            console.error('Error removing part:', error);
            showToast('An error occurred while removing the part', false);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UTILITY FUNCTIONS
    // ═══════════════════════════════════════════════════════════════════════════

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showToast(message, success) {
        const toast = document.getElementById('wo-toast');
        const messageEl = document.getElementById('toast-message');
        
        if (!toast || !messageEl) return;
        
        messageEl.textContent = message;
        toast.style.borderLeftColor = success ? '#22C55E' : '#EF4444';
        toast.querySelector('.toast-icon').textContent = success ? '✅' : '❌';
        
        toast.classList.add('show');
        
        setTimeout(function() {
            toast.classList.remove('show');
        }, 3000);
    }

    // Close modals on outside click
    window.addEventListener('click', function(event) {
        if (event.target.id === 'addPartModal') {
            closeAddPartModal();
        }
        if (event.target.id === 'editQuantityModal') {
            closeEditQuantityModal();
        }
    });

})();
