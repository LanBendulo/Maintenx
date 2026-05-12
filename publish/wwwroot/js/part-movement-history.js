/**
 * ═══════════════════════════════════════════════════════════════════════════════
 * PART MOVEMENT HISTORY
 * Displays inventory movement audit trail for individual parts
 * READ-ONLY: Does not modify inventory, only displays movement history
 * ═══════════════════════════════════════════════════════════════════════════════
 */

(function() {
    'use strict';

    // Attach event listeners to movement history links
    document.addEventListener('DOMContentLoaded', function() {
        attachMovementHistoryListeners();
    });

    function attachMovementHistoryListeners() {
        const links = document.querySelectorAll('.action-view-movement-history');
        
        links.forEach(function(link) {
            link.addEventListener('click', async function(e) {
                e.preventDefault();
                
                const partId = this.dataset.partId;
                const partName = this.dataset.partName;
                
                openMovementHistoryModal(partId, partName);
            });
        });
    }

    async function openMovementHistoryModal(partId, partName) {
        const modal = document.getElementById('movementHistoryModal');
        const partNameElement = document.getElementById('movement-history-part-name');
        const container = document.getElementById('movementHistoryContainer');
        
        // Set part name
        if (partNameElement) {
            partNameElement.textContent = partName;
        }
        
        // Show loading state
        if (container) {
            container.innerHTML = '<div style="text-align:center;padding:40px;color:var(--mx-muted);">Loading movement history...</div>';
        }
        
        // Open modal
        if (modal) {
            modal.classList.add('show');
        }
        
        // Load movement history
        try {
            const response = await fetch(`/admin/inventory-movements/part/${partId}`);
            const data = await response.json();
            
            if (data.success) {
                renderMovementHistory(data.movements);
            } else {
                container.innerHTML = `<div style="text-align:center;padding:40px;color:#EF4444;">${data.message || 'Failed to load movement history'}</div>`;
            }
        } catch (error) {
            console.error('Error loading movement history:', error);
            container.innerHTML = '<div style="text-align:center;padding:40px;color:#EF4444;">Error loading movement history</div>';
        }
    }

    function renderMovementHistory(movements) {
        const container = document.getElementById('movementHistoryContainer');
        
        if (!movements || movements.length === 0) {
            container.innerHTML = `
                <div style="text-align:center;padding:40px;color:var(--mx-muted);">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width:48px;height:48px;margin:0 auto 16px;opacity:0.3;">
                        <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
                    </svg>
                    <p>No movement history found for this part.</p>
                </div>
            `;
            return;
        }

        let html = '<div style="overflow-x:auto;"><table class="mx-table" style="font-size:13px;">';
        html += '<thead><tr>';
        html += '<th>Date/Time</th>';
        html += '<th>Movement Type</th>';
        html += '<th style="text-align:right;">Qty Changed</th>';
        html += '<th style="text-align:right;">Previous</th>';
        html += '<th style="text-align:right;">New</th>';
        html += '<th>Work Order</th>';
        html += '<th>Performed By</th>';
        html += '<th>Notes</th>';
        html += '</tr></thead><tbody>';

        movements.forEach(function(movement) {
            const movementTypeClass = movement.movementType?.toLowerCase() || 'default';
            const quantityClass = movement.quantityChanged < 0 ? 'negative' : 'positive';
            const movementBadge = getMovementBadge(movement.movementType);
            
            html += '<tr>';
            html += `<td style="font-size:12px;white-space:nowrap;">
                        ${formatDate(movement.createdAt)}
                        <div style="font-size:11px;color:var(--mx-muted);margin-top:2px;">
                            ${formatTime(movement.createdAt)}
                        </div>
                     </td>`;
            html += `<td>${movementBadge}</td>`;
            html += `<td style="text-align:right;font-weight:600;font-size:14px;" class="qty-${quantityClass}">
                        ${movement.quantityChanged > 0 ? '+' : ''}${movement.quantityChanged}
                     </td>`;
            html += `<td style="text-align:right;color:var(--mx-muted);">${movement.previousQuantity}</td>`;
            html += `<td style="text-align:right;font-weight:500;">${movement.newQuantity}</td>`;
            html += '<td>';
            if (movement.workOrderId) {
                html += `<a href="/admin/work-orders" style="color:var(--mx-primary);text-decoration:none;font-weight:500;">${escapeHtml(movement.workOrderNumber)}</a>`;
            } else {
                html += '<span style="color:var(--mx-muted);">-</span>';
            }
            html += '</td>';
            html += `<td style="font-size:12px;color:var(--mx-muted);">${escapeHtml(movement.performedBy)}</td>`;
            html += `<td style="font-size:12px;color:var(--mx-muted);max-width:200px;">
                        <div style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="${escapeHtml(movement.notes || '')}">
                            ${escapeHtml(movement.notes || '-')}
                        </div>
                     </td>`;
            html += '</tr>';
        });

        html += '</tbody></table></div>';
        container.innerHTML = html;
    }

    function getMovementBadge(movementType) {
        const badges = {
            'Consumption': '<span class="movement-badge movement-consumption">Consumption</span>',
            'Restock': '<span class="movement-badge movement-restock">Restock</span>',
            'Adjustment': '<span class="movement-badge movement-adjustment">Adjustment</span>',
            'Correction': '<span class="movement-badge movement-correction">Correction</span>',
            'InitialStock': '<span class="movement-badge movement-initialstock">Initial Stock</span>',
            'Return': '<span class="movement-badge movement-return">Return</span>',
            'Transfer': '<span class="movement-badge movement-transfer">Transfer</span>'
        };
        return badges[movementType] || `<span class="movement-badge movement-default">${movementType}</span>`;
    }

    function formatDate(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
    }

    function formatTime(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Close modal handlers
    document.getElementById('closeMovementHistoryModal')?.addEventListener('click', function() {
        document.getElementById('movementHistoryModal')?.classList.remove('show');
    });

    document.getElementById('closeMovementHistoryBtn')?.addEventListener('click', function() {
        document.getElementById('movementHistoryModal')?.classList.remove('show');
    });

    // Close on outside click
    window.addEventListener('click', function(event) {
        if (event.target.id === 'movementHistoryModal') {
            document.getElementById('movementHistoryModal')?.classList.remove('show');
        }
    });

})();
