// ═══════════════════════════════════════════════════════════════
// PREVENTIVE MAINTENANCE SCHEDULING
// Handles CRUD operations and work order generation
// ═══════════════════════════════════════════════════════════════

(function() {
    let isEditMode = false;
    let currentScheduleId = null;

    // ── Initialize ──────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function() {
        console.log('PM Script loaded');
        console.log('Button exists:', document.getElementById('openScheduleModal'));
        
        // Set minimum date for next due date input (today)
        const nextDueDateInput = document.getElementById('schedule-next-due');
        if (nextDueDateInput) {
            const today = new Date().toISOString().split('T')[0];
            nextDueDateInput.setAttribute('min', today);
            console.log('[PM] Set minimum date for next due date:', today);
        }
        
        loadAssets();
        loadTechnicians();
        setupEventListeners();
        initializeGovernanceChecks(); // ← GOVERNANCE: Check generation eligibility on load
    });

    // ── Event Listeners ─────────────────────────────────────────
    function setupEventListeners() {
        // Open create modal
        const openBtn = document.getElementById('openScheduleModal');
        if (openBtn) {
            openBtn.addEventListener('click', function() {
                console.log('Button clicked!');
                openCreateModal();
            });
        } else {
            console.error('openScheduleModal button not found');
        }

        // Close modal
        document.getElementById('closeScheduleModal').addEventListener('click', closeModal);
        document.getElementById('cancelScheduleModal').addEventListener('click', closeModal);

        // Submit form
        document.getElementById('submitScheduleForm').addEventListener('click', handleSubmit);

        // Filter: Search
        document.getElementById('pm-search').addEventListener('input', handleSearch);

        // Filter: Status
        document.getElementById('filter-status').addEventListener('change', function() {
            window.location.href = '/admin/preventive-maintenance?filter=' + this.value;
        });

        // Reset filters
        document.getElementById('reset-filters').addEventListener('click', resetFilters);

        // Action menu toggles
        document.querySelectorAll('.action-trigger').forEach(function(btn) {
            btn.addEventListener('click', function(e) {
                e.stopPropagation();
                toggleActionMenu(this);
            });
        });

        // Close dropdowns on outside click
        document.addEventListener('click', function() {
            document.querySelectorAll('.action-dropdown').forEach(function(d) {
                d.classList.remove('show');
            });
        });

        // Generate Work Order
        document.querySelectorAll('.action-generate-wo').forEach(function(link) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                generateWorkOrder(this.dataset.scheduleId);
            });
        });

        // Edit
        document.querySelectorAll('.action-edit').forEach(function(link) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                openEditModal(this.dataset.scheduleId);
            });
        });

        // Toggle Status
        document.querySelectorAll('.action-toggle-status').forEach(function(link) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                toggleStatus(this.dataset.scheduleId, this.dataset.isActive === 'true');
            });
        });

        // Delete
        document.querySelectorAll('.action-delete').forEach(function(link) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                deleteSchedule(this.dataset.scheduleId);
            });
        });

        // Close modal on outside click
        window.addEventListener('click', function(event) {
            if (event.target.classList.contains('mx-modal-overlay')) {
                closeModal();
            }
        });
    }

    // ── Load Assets ─────────────────────────────────────────────
    async function loadAssets() {
        try {
            const response = await fetch('/admin/preventive-maintenance/assets/list');
            const assets = await response.json();
            
            const select = document.getElementById('schedule-asset');
            select.innerHTML = '<option value="">Select asset…</option>';
            
            assets.forEach(function(asset) {
                const option = document.createElement('option');
                option.value = asset.value;
                option.textContent = asset.text;
                select.appendChild(option);
            });
        } catch (error) {
            console.error('Failed to load assets:', error);
        }
    }

    // ── Load Technicians ────────────────────────────────────────
    async function loadTechnicians() {
        try {
            const response = await fetch('/admin/preventive-maintenance/technicians/list');
            const technicians = await response.json();
            
            const select = document.getElementById('schedule-technician');
            select.innerHTML = '<option value="">Unassigned</option>';
            
            technicians.forEach(function(tech) {
                const option = document.createElement('option');
                option.value = tech.value;
                option.textContent = tech.text;
                select.appendChild(option);
            });
        } catch (error) {
            console.error('Failed to load technicians:', error);
        }
    }

    // ── Open Create Modal ───────────────────────────────────────
    function openCreateModal() {
        console.log('openCreateModal called');
        isEditMode = false;
        currentScheduleId = null;
        
        document.getElementById('modal-title').textContent = 'Create Preventive Maintenance Schedule';
        document.getElementById('submit-btn-text').textContent = 'Create Schedule';
        document.getElementById('scheduleForm').reset();
        document.getElementById('schedule-id').value = '';
        document.getElementById('schedule-priority').value = 'Medium'; // Reset to default
        
        // Set default next due date to tomorrow
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        document.getElementById('schedule-next-due').value = tomorrow.toISOString().split('T')[0];
        
        clearErrors();
        const modal = document.getElementById('scheduleModal');
        console.log('Modal element:', modal);
        modal.classList.add('open');
        console.log('Modal classes after add:', modal.classList);
    }

    // ── Open Edit Modal ─────────────────────────────────────────
    async function openEditModal(scheduleId) {
        isEditMode = true;
        currentScheduleId = scheduleId;
        
        try {
            const response = await fetch(`/admin/preventive-maintenance/${scheduleId}`);
            const data = await response.json();
            
            if (data) {
                document.getElementById('modal-title').textContent = 'Edit Preventive Maintenance Schedule';
                document.getElementById('submit-btn-text').textContent = 'Save Changes';
                
                document.getElementById('schedule-id').value = data.scheduleId;
                document.getElementById('schedule-asset').value = data.assetId;
                document.getElementById('schedule-title').value = data.title;
                document.getElementById('schedule-description').value = data.description || '';
                document.getElementById('schedule-frequency').value = data.frequencyDays;
                document.getElementById('schedule-next-due').value = data.nextDueDate.split('T')[0];
                document.getElementById('schedule-technician').value = data.defaultTechnicianId || '';
                document.getElementById('schedule-priority').value = data.priority || 'Medium';
                
                clearErrors();
                document.getElementById('scheduleModal').classList.add('open');
            }
        } catch (error) {
            showToast('Failed to load schedule details', false);
            console.error(error);
        }
    }

    // ── Close Modal ─────────────────────────────────────────────
    function closeModal() {
        document.getElementById('scheduleModal').classList.remove('open');
        document.getElementById('scheduleForm').reset();
        clearErrors();
    }

    // ── Handle Submit ───────────────────────────────────────────
    async function handleSubmit() {
        clearErrors();
        
        // Validate
        const assetId = document.getElementById('schedule-asset').value;
        const title = document.getElementById('schedule-title').value.trim();
        const frequency = parseInt(document.getElementById('schedule-frequency').value);
        const nextDue = document.getElementById('schedule-next-due').value;
        
        let hasError = false;
        
        if (!assetId) {
            showError('err-asset');
            hasError = true;
        }
        
        if (!title) {
            showError('err-title');
            hasError = true;
        }
        
        if (!frequency || frequency <= 0) {
            showError('err-frequency');
            hasError = true;
        }
        
        if (!nextDue) {
            showError('err-next-due');
            hasError = true;
        } else {
            // Validate next due date is not in the past
            const selectedDate = new Date(nextDue);
            const today = new Date();
            today.setHours(0, 0, 0, 0); // Reset time to compare dates only
            
            if (selectedDate < today) {
                showError('err-next-due', 'Next due date cannot be in the past.');
                hasError = true;
            }
        }
        
        if (hasError) return;
        
        // Prepare data
        const data = {
            assetId: parseInt(assetId),
            title: title,
            description: document.getElementById('schedule-description').value.trim() || null,
            frequencyDays: frequency,
            nextDueDate: nextDue,
            defaultTechnicianId: document.getElementById('schedule-technician').value ? parseInt(document.getElementById('schedule-technician').value) : null,
            priority: document.getElementById('schedule-priority').value || 'Medium'
        };
        
        try {
            let response;
            
            if (isEditMode) {
                response = await fetch(`/admin/preventive-maintenance/${currentScheduleId}/edit`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
            } else {
                response = await fetch('/admin/preventive-maintenance/create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
            }
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, true);
                closeModal();
                setTimeout(function() { location.reload(); }, 1500);
            } else {
                showToast(result.message || 'Operation failed', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    }

    // ── Generate Work Order ─────────────────────────────────────
    async function generateWorkOrder(scheduleId) {
        // Check governance before showing confirmation
        try {
            const checkResponse = await fetch(`/admin/preventive-maintenance/${scheduleId}/can-generate`);
            const checkData = await checkResponse.json();
            
            if (!checkData.canGenerate) {
                showToast(checkData.tooltipMessage || 'Cannot generate work order at this time', false);
                return;
            }
        } catch (error) {
            console.error('Governance check failed:', error);
        }
        
        if (!confirm('Generate a work order from this schedule?')) {
            return;
        }
        
        try {
            const response = await fetch(`/admin/preventive-maintenance/${scheduleId}/generate`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, true);
                setTimeout(function() { location.reload(); }, 1500);
            } else {
                showToast(result.message || 'Failed to generate work order', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    }

    // ── Toggle Status ───────────────────────────────────────────
    async function toggleStatus(scheduleId, isActive) {
        const action = isActive ? 'deactivate' : 'activate';
        
        if (!confirm(`Are you sure you want to ${action} this schedule?`)) {
            return;
        }
        
        try {
            const response = await fetch(`/admin/preventive-maintenance/${scheduleId}/toggle-status`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' }
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, true);
                setTimeout(function() { location.reload(); }, 1500);
            } else {
                showToast(result.message || 'Operation failed', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    }

    // ── Delete Schedule ─────────────────────────────────────────
    async function deleteSchedule(scheduleId) {
        if (!confirm('Are you sure you want to delete this schedule? This action cannot be undone.')) {
            return;
        }
        
        try {
            const response = await fetch(`/admin/preventive-maintenance/${scheduleId}/delete`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' }
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, true);
                setTimeout(function() { location.reload(); }, 1500);
            } else {
                showToast(result.message || 'Failed to delete schedule', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    }

    // ── Search Filter ───────────────────────────────────────────
    function handleSearch() {
        const searchTerm = this.value.toLowerCase();
        const rows = document.querySelectorAll('#pm-tbody tr');
        let visibleCount = 0;
        
        rows.forEach(function(row) {
            const text = row.textContent.toLowerCase();
            if (text.includes(searchTerm)) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });
        
        document.querySelector('#row-count strong').textContent = visibleCount;
    }

    // ── Reset Filters ───────────────────────────────────────────
    function resetFilters() {
        document.getElementById('pm-search').value = '';
        document.getElementById('filter-status').value = 'active';
        
        const rows = document.querySelectorAll('#pm-tbody tr');
        rows.forEach(function(row) {
            row.style.display = '';
        });
        
        document.querySelector('#row-count strong').textContent = rows.length;
    }

    // ── Toggle Action Menu ──────────────────────────────────────
    function toggleActionMenu(btn) {
        const dropdown = btn.nextElementSibling;
        const isOpen = dropdown.classList.contains('show');
        
        // Close all dropdowns
        document.querySelectorAll('.action-dropdown').forEach(function(d) {
            d.classList.remove('show');
        });
        
        // Toggle current dropdown
        if (!isOpen) {
            dropdown.classList.add('show');
        }
    }

    // ── Show Error ──────────────────────────────────────────────
    function showError(errorId, customMessage) {
        const errorEl = document.getElementById(errorId);
        if (customMessage) {
            errorEl.textContent = customMessage;
        }
        errorEl.style.display = 'block';
    }

    // ── Clear Errors ────────────────────────────────────────────
    function clearErrors() {
        document.querySelectorAll('.input-error').forEach(function(el) {
            el.style.display = 'none';
            // Reset to default error messages if they were customized
            if (el.id === 'err-next-due') {
                el.textContent = 'Please select a due date.';
            }
        });
    }

    // ── Show Toast ──────────────────────────────────────────────
    function showToast(message, success) {
        const toast = document.getElementById('pm-toast');
        const messageEl = document.getElementById('toast-message');
        
        messageEl.textContent = message;
        toast.style.borderLeftColor = success ? '#22C55E' : '#EF4444';
        toast.querySelector('.toast-icon').textContent = success ? '✅' : '❌';
        
        toast.classList.add('show');
        
        setTimeout(function() {
            toast.classList.remove('show');
        }, 3000);
    }

    // ═══════════════════════════════════════════════════════════
    // PM GOVERNANCE UI LOGIC
    // ═══════════════════════════════════════════════════════════

    /**
     * Initialize governance checks for all PM schedules on page load
     * Disables "Generate Work Order" buttons when not allowed
     * Shows tooltips explaining why generation is blocked
     */
    async function initializeGovernanceChecks() {
        const generateButtons = document.querySelectorAll('.action-generate-wo');
        
        for (const button of generateButtons) {
            const scheduleId = button.dataset.scheduleId;
            await updateGenerationButtonState(scheduleId, button);
        }
    }

    /**
     * Update a single generation button based on governance rules
     * @param {number} scheduleId - PM schedule ID
     * @param {HTMLElement} button - Generate button element
     */
    async function updateGenerationButtonState(scheduleId, button) {
        try {
            const response = await fetch(`/admin/preventive-maintenance/${scheduleId}/can-generate`);
            const status = await response.json();
            
            // Update button state
            if (!status.canGenerate) {
                button.classList.add('disabled');
                button.style.opacity = '0.5';
                button.style.cursor = 'not-allowed';
                button.title = status.tooltipMessage;
                
                // Prevent click when disabled
                button.addEventListener('click', function(e) {
                    if (this.classList.contains('disabled')) {
                        e.preventDefault();
                        e.stopPropagation();
                        showToast(status.tooltipMessage, false);
                    }
                }, true);
            } else {
                button.classList.remove('disabled');
                button.style.opacity = '1';
                button.style.cursor = 'pointer';
                button.title = 'Generate work order from this schedule';
            }
            
            // Update status badge in table row (if exists)
            const row = button.closest('tr');
            if (row) {
                updateRowStatusIndicator(row, status);
            }
        } catch (error) {
            console.error(`Failed to check governance for schedule ${scheduleId}:`, error);
        }
    }

    /**
     * Update visual status indicator in table row
     * Shows: Due, Overdue, Active WO, Not Due
     * @param {HTMLElement} row - Table row element
     * @param {Object} status - Governance status object
     */
    function updateRowStatusIndicator(row, status) {
        // Find or create status indicator cell
        let statusCell = row.querySelector('.pm-status-indicator');
        
        if (!statusCell) {
            // Create status indicator if it doesn't exist
            const actionsCell = row.querySelector('.actions-cell');
            if (actionsCell) {
                statusCell = document.createElement('span');
                statusCell.className = 'pm-status-indicator';
                statusCell.style.marginLeft = '8px';
                statusCell.style.fontSize = '0.75rem';
                statusCell.style.padding = '2px 8px';
                statusCell.style.borderRadius = '12px';
                statusCell.style.fontWeight = '500';
                actionsCell.insertBefore(statusCell, actionsCell.firstChild);
            }
        }
        
        if (statusCell) {
            statusCell.textContent = status.statusMessage;
            statusCell.title = status.tooltipMessage;
            
            // Color coding
            if (status.hasActiveWorkOrder) {
                statusCell.style.backgroundColor = '#DBEAFE'; // Blue
                statusCell.style.color = '#1E40AF';
            } else if (status.isOverdue) {
                statusCell.style.backgroundColor = '#FEE2E2'; // Red
                statusCell.style.color = '#991B1B';
            } else if (status.isDue) {
                statusCell.style.backgroundColor = '#FEF3C7'; // Yellow
                statusCell.style.color = '#92400E';
            } else {
                statusCell.style.backgroundColor = '#F3F4F6'; // Gray
                statusCell.style.color = '#4B5563';
            }
        }
    }
})();
