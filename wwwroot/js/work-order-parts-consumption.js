/**
 * ═══════════════════════════════════════════════════════════════════════════════
 * WORK ORDER PARTS CONSUMPTION VISIBILITY
 * Displays consumed parts and inventory movements for work orders
 * READ-ONLY: Does not modify inventory, only displays consumption history
 * ═══════════════════════════════════════════════════════════════════════════════
 */

(function() {
    'use strict';

    let currentWorkOrderId = null;

    // Hook into existing view details functionality
    document.addEventListener('DOMContentLoaded', function() {
        const viewDetailsLinks = document.querySelectorAll('.action-view-details');
        
        viewDetailsLinks.forEach(function(link) {
            link.addEventListener('click', function(e) {
                const woId = this.dataset.woId;
                currentWorkOrderId = woId;
                
                // Load parts consumption after modal opens
                setTimeout(function() {
                    loadPartsConsumption(woId);
                }, 300);
            });
        });
    });

    /**
     * Load parts consumption data for a work order
     */
    async function loadPartsConsumption(workOrderId) {
        try {
            const response = await fetch(`/admin/inventory-movements/work-order/${workOrderId}`);
            const data = await response.json();
            
            if (data.success) {
                renderConsumedParts(data.consumedParts, data.totalMaterialCost);
            } else {
                console.error('Failed to load parts consumption:', data.message);
            }
        } catch (error) {
            console.error('Error loading parts consumption:', error);
        }
    }

    /**
     * Render consumed parts in the table
     */
    function renderConsumedParts(parts, totalCost) {
        const tbody = document.getElementById('parts-used-tbody');
        const noPartsRow = document.getElementById('no-parts-row');
        const totalElement = document.getElementById('parts-used-total');
        const costPartsElement = document.getElementById('cost-parts');
        
        if (!tbody) return;

        // Clear existing rows except the no-parts row
        const existingRows = tbody.querySelectorAll('tr:not(#no-parts-row)');
        existingRows.forEach(row => row.remove());

        if (!parts || parts.length === 0) {
            if (noPartsRow) noPartsRow.style.display = '';
            if (totalElement) totalElement.textContent = '₱ 0.00';
            if (costPartsElement) costPartsElement.textContent = '₱ 0.00';
            return;
        }

        // Hide no-parts row
        if (noPartsRow) noPartsRow.style.display = 'none';

        // Render each consumed part
        parts.forEach(function(part) {
            const row = document.createElement('tr');
            
            const unitCostDisplay = part.unitCost != null ? `₱ ${parseFloat(part.unitCost).toFixed(2)}` : '-';
            const totalCostDisplay = part.totalCost != null ? `₱ ${parseFloat(part.totalCost).toFixed(2)}` : '-';
            const partNumberDisplay = part.partNumber || '-';
            
            row.innerHTML = `
                <td>
                    <div style="font-weight:500;color:var(--mx-text);">${escapeHtml(part.partName)}</div>
                    ${part.consumedAt ? `<div style="font-size:11px;color:var(--mx-muted);margin-top:2px;">Consumed: ${formatDateTime(part.consumedAt)}</div>` : ''}
                </td>
                <td style="font-size:12px;color:var(--mx-muted);">${escapeHtml(partNumberDisplay)}</td>
                <td style="text-align:right;font-weight:600;">${part.quantityUsed}</td>
                <td style="text-align:right;color:var(--mx-muted);">${unitCostDisplay}</td>
                <td style="text-align:right;font-weight:600;color:var(--mx-primary);">${totalCostDisplay}</td>
                <td style="text-align:center;">
                    <span style="display:inline-block;padding:3px 8px;font-size:10px;font-weight:600;border-radius:4px;background:rgba(34,197,94,0.12);color:#16A34A;">
                        ${escapeHtml(part.usageStatus)}
                    </span>
                </td>
            `;
            
            tbody.appendChild(row);
        });

        // Update total cost
        const formattedTotal = `₱ ${parseFloat(totalCost || 0).toFixed(2)}`;
        if (totalElement) totalElement.textContent = formattedTotal;
        if (costPartsElement) costPartsElement.textContent = formattedTotal;
    }

    /**
     * Format date/time for display
     */
    function formatDateTime(dateString) {
        if (!dateString) return '-';
        
        const date = new Date(dateString);
        const options = { 
            year: 'numeric', 
            month: 'short', 
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        };
        
        return date.toLocaleDateString('en-US', options);
    }

    /**
     * Escape HTML to prevent XSS
     */
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

})();
