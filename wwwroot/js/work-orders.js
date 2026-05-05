// Work Orders Page - Database Integration

(function () {
    'use strict';

    // ========================================
    // MODAL MANAGEMENT
    // ========================================
    const overlay = document.getElementById('woModal');
    const openBtn = document.getElementById('openWoModal');
    const closeBtn = document.getElementById('closeWoModal');
    const cancelBtn = document.getElementById('cancelWoModal');
    const submitBtn = document.getElementById('submitWoForm');
    const toast = document.getElementById('wo-toast');
    const form = document.getElementById('woForm');

    function openModal() {
        console.log('=== Opening Work Order Modal ===');
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
        
        // Check if we're converting from a maintenance request
        const convertData = sessionStorage.getItem('convertFromRequest');
        console.log('Convert data from sessionStorage:', convertData);
        
        if (convertData) {
            const data = JSON.parse(convertData);
            console.log('Parsed conversion data:', data);
            sessionStorage.removeItem('convertFromRequest');
            
            // Update modal title
            document.getElementById('modal-title').textContent = 'Convert Request to Work Order';
            document.querySelector('.modal-subtitle').textContent = `Converting ${data.requestNumber} to a work order`;
            
            // Load assets and technicians first, then pre-fill
            console.log('Loading assets and technicians for conversion...');
            Promise.all([loadAssets(), loadTechnicians()]).then(() => {
                console.log('Assets and technicians loaded. Pre-filling form...');
                // Pre-fill and lock fields after assets are loaded
                setTimeout(() => {
                    // Asset (read-only)
                    const assetSelect = document.getElementById('wo-equipment');
                    console.log('Asset select element:', assetSelect);
                    console.log('Available options:', Array.from(assetSelect.options).map(o => ({value: o.value, text: o.text})));
                    console.log('Setting asset to:', data.assetId, 'Asset name:', data.assetName);
                    assetSelect.value = data.assetId;
                    
                    // Verify the asset was set
                    if (!assetSelect.value || assetSelect.value === '') {
                        console.error('Asset not found in dropdown. Available options:', 
                            Array.from(assetSelect.options).map(o => ({value: o.value, text: o.text})));
                        // If asset not found, add it manually
                        console.log('Manually adding asset option...');
                        const option = document.createElement('option');
                        option.value = data.assetId;
                        option.textContent = data.assetName;
                        option.selected = true;
                        assetSelect.appendChild(option);
                        console.log('Asset option added manually');
                    } else {
                        console.log('Asset successfully set to:', assetSelect.value);
                    }
                    
                    // Make it look disabled but keep it enabled so value is submitted
                    // Store original event handlers
                    assetSelect.dataset.locked = 'true';
                    assetSelect.style.background = '#F0F4F8';
                    assetSelect.style.color = '#495057';
                    assetSelect.style.cursor = 'not-allowed';
                    assetSelect.style.pointerEvents = 'none'; // Prevent interaction
                    
                    // Description (read-only)
                    const descTextarea = document.getElementById('wo-issue');
                    descTextarea.value = data.description;
                    descTextarea.readOnly = true;
                    descTextarea.style.background = '#F0F4F8';
                    descTextarea.style.color = '#495057';
                    descTextarea.style.cursor = 'not-allowed';
                    
                    // Priority (read-only)
                    const priorityRadios = document.querySelectorAll('input[name="wo-priority"]');
                    priorityRadios.forEach(radio => {
                        if (radio.value === data.priority) {
                            radio.checked = true;
                        }
                        // Make it look disabled but keep enabled
                        radio.style.pointerEvents = 'none';
                        radio.parentElement.style.opacity = '0.6';
                        radio.parentElement.style.cursor = 'not-allowed';
                    });
                    
                    // Store the request ID for submission
                    form.dataset.maintenanceRequestId = data.maintenanceRequestId;
                    
                    // Update submit button text
                    submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Convert to Work Order';
                    
                    console.log('Form pre-filled successfully');
                }, 200);
            }).catch(error => {
                console.error('Error loading assets/technicians:', error);
                showToast('Failed to load form data: ' + error.message, 'error');
            });
        } else {
            console.log('No conversion data - opening as manual work order');
            // Reset modal for manual work order
            document.getElementById('modal-title').textContent = 'Create Manual Work Order';
            document.querySelector('.modal-subtitle').textContent = 'Create a work order without a maintenance request';
            delete form.dataset.maintenanceRequestId;
            
            // Load assets and technicians
            console.log('Loading assets and technicians for manual work order...');
            loadAssets();
            loadTechnicians();
            
            // Reset submit button text
            submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Create Manual Work Order';
        }
    }

    function closeModal() {
        overlay.classList.remove('open');
        document.body.style.overflow = '';
        form.reset();
        clearErrors();
        
        // Re-enable all fields
        const assetSelect = document.getElementById('wo-equipment');
        assetSelect.style.background = '';
        assetSelect.style.color = '';
        assetSelect.style.cursor = '';
        assetSelect.style.pointerEvents = '';
        delete assetSelect.dataset.locked;
        
        const descTextarea = document.getElementById('wo-issue');
        descTextarea.readOnly = false;
        descTextarea.style.background = '';
        descTextarea.style.color = '';
        descTextarea.style.cursor = '';
        
        document.querySelectorAll('input[name="wo-priority"]').forEach(radio => {
            radio.style.pointerEvents = '';
            radio.parentElement.style.opacity = '';
            radio.parentElement.style.cursor = '';
        });
        
        delete form.dataset.maintenanceRequestId;
    }

    openBtn.addEventListener('click', openModal);
    closeBtn.addEventListener('click', closeModal);
    cancelBtn.addEventListener('click', closeModal);
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeModal();
    });

    // ========================================
    // LOAD ASSETS FROM DATABASE
    // ========================================
    async function loadAssets() {
        try {
            console.log('Loading assets from /admin/assets/list...');
            console.log('Current URL:', window.location.href);
            
            const response = await fetch('/admin/assets/list', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });
            
            console.log('Response status:', response.status);
            console.log('Response ok:', response.ok);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Assets endpoint error:', response.status, errorText);
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }
            
            const assets = await response.json();
            console.log('Assets loaded successfully:', assets.length, 'items');
            console.log('First asset:', assets[0]);
            
            const select = document.getElementById('wo-equipment');
            console.log('Select element found:', select !== null);
            
            if (!select) {
                console.error('Equipment select element not found!');
                return;
            }
            
            // Clear existing options except the first one
            select.innerHTML = '<option value="">Select equipment…</option>';
            
            if (assets.length === 0) {
                console.warn('No assets found in database. Please seed the database first.');
                showToast('No equipment found. Please contact administrator.', 'error');
                return;
            }
            
            assets.forEach(asset => {
                const option = document.createElement('option');
                option.value = asset.value;
                option.textContent = asset.text;
                select.appendChild(option);
            });
            
            console.log('Assets dropdown populated successfully. Total options:', select.options.length);
            console.log('All options:', Array.from(select.options).map(o => ({value: o.value, text: o.text})));
        } catch (error) {
            console.error('Error loading assets:', error);
            console.error('Error stack:', error.stack);
            showToast('Failed to load equipment list: ' + error.message, 'error');
        }
    }

    // ========================================
    // LOAD TECHNICIANS FROM DATABASE
    // ========================================
    async function loadTechnicians() {
        try {
            const response = await fetch('/admin/technicians/list');
            if (!response.ok) throw new Error('Failed to load technicians');
            
            const technicians = await response.json();
            const select = document.getElementById('wo-tech');
            
            // Clear existing options except the first one
            select.innerHTML = '<option value="">Select technician…</option>';
            
            technicians.forEach(tech => {
                const option = document.createElement('option');
                option.value = tech.value;
                option.textContent = tech.text;
                select.appendChild(option);
            });
        } catch (error) {
            console.error('Error loading technicians:', error);
            showToast('Failed to load technicians list', 'error');
        }
    }

    // ========================================
    // FORM VALIDATION
    // ========================================
    function validateForm() {
        let isValid = true;
        clearErrors();

        // Equipment
        const equipment = document.getElementById('wo-equipment');
        if (!equipment.value) {
            showError('err-equip');
            isValid = false;
        }

        // Issue Description
        const issue = document.getElementById('wo-issue');
        if (!issue.value.trim()) {
            showError('err-issue');
            isValid = false;
        }

        // Technician
        const tech = document.getElementById('wo-tech');
        if (!tech.value) {
            showError('err-tech');
            isValid = false;
        }

        // Start Date
        const startDate = document.getElementById('wo-start');
        if (!startDate.value) {
            showError('err-start');
            isValid = false;
        }

        // End Date
        const endDate = document.getElementById('wo-end');
        if (!endDate.value) {
            showError('err-end');
            isValid = false;
        }

        // Validate end date is after start date
        if (startDate.value && endDate.value) {
            if (new Date(endDate.value) < new Date(startDate.value)) {
                showError('err-end');
                document.getElementById('err-end').textContent = 'Completion date must be after start date.';
                isValid = false;
            }
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
    // SUBMIT WORK ORDER TO DATABASE
    // ========================================
    submitBtn.addEventListener('click', async function (e) {
        e.preventDefault();

        if (!validateForm()) {
            return;
        }

        // Get form values
        const priority = document.querySelector('input[name="wo-priority"]:checked');
        const startDate = document.getElementById('wo-start').value;
        const dueDate = document.getElementById('wo-end').value;
        
        // Build payload matching backend DTO exactly
        const workOrderData = {
            AssetId: parseInt(document.getElementById('wo-equipment').value),
            Description: document.getElementById('wo-issue').value.trim(),
            AssignedTo: parseInt(document.getElementById('wo-tech').value),
            Priority: priority ? priority.value : 'Medium',
            DateCreated: startDate ? new Date(startDate).toISOString() : new Date().toISOString(),
            DueDate: dueDate ? new Date(dueDate).toISOString() : new Date().toISOString(),
            Notes: document.getElementById('wo-notes').value.trim(),
            MaintenanceRequestId: form.dataset.maintenanceRequestId ? parseInt(form.dataset.maintenanceRequestId) : null
        };

        console.log('=== Submitting Work Order ===');
        console.log('Work Order Data:', JSON.stringify(workOrderData, null, 2));
        console.log('AssetId:', workOrderData.AssetId, 'Type:', typeof workOrderData.AssetId, 'IsNaN:', isNaN(workOrderData.AssetId));
        console.log('AssignedTo:', workOrderData.AssignedTo, 'Type:', typeof workOrderData.AssignedTo, 'IsNaN:', isNaN(workOrderData.AssignedTo));
        console.log('MaintenanceRequestId:', workOrderData.MaintenanceRequestId);
        console.log('Description length:', workOrderData.Description.length);
        console.log('Priority:', workOrderData.Priority);
        console.log('DateCreated:', workOrderData.DateCreated);
        console.log('DueDate:', workOrderData.DueDate);

        // Validate data before sending
        if (isNaN(workOrderData.AssetId) || workOrderData.AssetId <= 0) {
            console.error('Invalid AssetId:', workOrderData.AssetId);
            showToast('Please select equipment', 'error');
            return;
        }
        
        if (isNaN(workOrderData.AssignedTo) || workOrderData.AssignedTo <= 0) {
            console.error('Invalid AssignedTo:', workOrderData.AssignedTo);
            showToast('Please assign a technician', 'error');
            return;
        }
        
        if (!workOrderData.Description || workOrderData.Description.length === 0) {
            console.error('Description is empty');
            showToast('Please enter an issue description', 'error');
            return;
        }

        // Disable submit button
        submitBtn.disabled = true;
        const isConversion = form.dataset.maintenanceRequestId;
        submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> ' + 
            (isConversion ? 'Converting...' : 'Creating...');

        try {
            console.log('Sending POST request to /admin/work-orders/create...');
            const response = await fetch('/admin/work-orders/create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(workOrderData)
            });

            console.log('Response status:', response.status);
            console.log('Response ok:', response.ok);

            const result = await response.json();
            console.log('Response data:', result);
            console.log('Response success:', result.success);
            console.log('Response message:', result.message);
            console.log('Response errors:', result.errors);

            if (response.ok && result.success) {
                // Success
                console.log('Work order created successfully!');
                closeModal();
                showToast(result.message || 'Work order created successfully!', 'success');
                
                // Reload the page after 1.5 seconds to show the new work order
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                // Error - log for debugging
                console.error('=== Server Error ===');
                console.error('Response status:', response.status);
                console.error('Response ok:', response.ok);
                console.error('Server response:', result);
                console.error('Success:', result.success);
                console.error('Message:', result.message);
                console.error('Errors:', result.errors);
                
                let errorMessage = result.message || 'Failed to create work order';
                if (result.errors && Array.isArray(result.errors)) {
                    errorMessage = result.errors.join(', ');
                }
                
                showToast(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Error creating work order:', error);
            console.error('Error stack:', error.stack);
            showToast('An error occurred while creating the work order. Please try again.', 'error');
        } finally {
            // Re-enable submit button
            submitBtn.disabled = false;
            const buttonText = form.dataset.maintenanceRequestId ? 'Convert to Work Order' : 'Create Manual Work Order';
            submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> ' + buttonText;
        }
    });

    // ========================================
    // TOAST NOTIFICATIONS
    // ========================================
    function showToast(message, type = 'success') {
        const toast = document.getElementById('wo-toast');
        if (!toast) return;

        toast.textContent = '';
        
        // Add icon
        const icon = document.createElement('span');
        icon.className = 'toast-icon';
        icon.textContent = type === 'success' ? '✅' : '❌';
        toast.appendChild(icon);
        
        // Add message
        const messageText = document.createTextNode(message);
        toast.appendChild(messageText);
        
        // Update styling
        toast.className = type === 'success' ? 'mx-toast-success' : 'mx-toast-error';
        toast.classList.add('show');

        // Hide after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
        }, 3000);
    }

    // ========================================
    // FILTER FUNCTIONALITY
    // ========================================
    const searchInput = document.getElementById('wo-search');
    const statusFilter = document.getElementById('filter-status');
    const priorityFilter = document.getElementById('filter-priority');
    const techFilter = document.getElementById('filter-tech');
    const sourceFilter = document.getElementById('filter-source');
    const resetBtn = document.getElementById('reset-filters');
    const tableBody = document.getElementById('wo-tbody');

    function filterTable() {
        const searchTerm = searchInput.value.toLowerCase();
        const statusValue = statusFilter.value;
        const priorityValue = priorityFilter.value;
        const techValue = techFilter.value;
        const sourceValue = sourceFilter.value;

        const rows = tableBody.querySelectorAll('tr');
        let visibleCount = 0;

        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const rowStatus = row.getAttribute('data-status');
            const rowPriority = row.getAttribute('data-priority');
            const rowTech = row.getAttribute('data-tech');
            const rowSource = row.getAttribute('data-source');

            const matchesSearch = text.includes(searchTerm);
            const matchesStatus = !statusValue || rowStatus === statusValue;
            const matchesPriority = !priorityValue || rowPriority === priorityValue;
            const matchesTech = !techValue || rowTech === techValue;
            const matchesSource = !sourceValue || rowSource === sourceValue;

            if (matchesSearch && matchesStatus && matchesPriority && matchesTech && matchesSource) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        // Update count
        const countElement = document.getElementById('row-count');
        if (countElement) {
            countElement.innerHTML = `Showing <strong>${visibleCount}</strong> result${visibleCount !== 1 ? 's' : ''}`;
        }
    }

    if (searchInput) searchInput.addEventListener('input', filterTable);
    if (statusFilter) statusFilter.addEventListener('change', filterTable);
    if (priorityFilter) priorityFilter.addEventListener('change', filterTable);
    if (techFilter) techFilter.addEventListener('change', filterTable);
    if (sourceFilter) sourceFilter.addEventListener('change', filterTable);

    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            searchInput.value = '';
            statusFilter.value = '';
            priorityFilter.value = '';
            techFilter.value = '';
            sourceFilter.value = '';
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
            
            // Close other dropdowns
            document.querySelectorAll('.action-dropdown').forEach(dd => {
                if (dd !== dropdown) dd.classList.remove('show');
            });
            
            dropdown.classList.toggle('show');
        });
    });

    // Close dropdowns when clicking outside
    document.addEventListener('click', () => {
        document.querySelectorAll('.action-dropdown').forEach(dd => {
            dd.classList.remove('show');
        });
    });

    // ========================================
    // AUTO-OPEN MODAL FOR CONVERSION
    // ========================================
    // Check if we're converting from a maintenance request on page load
    window.addEventListener('DOMContentLoaded', function() {
        const convertData = sessionStorage.getItem('convertFromRequest');
        if (convertData) {
            // Automatically open the modal
            setTimeout(() => {
                openModal();
            }, 500);
        }
    });

})();


    // ========================================
    // VIEW DETAILS ACTION
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-view-details')) {
            e.preventDefault();
            const link = e.target.closest('.action-view-details');
            const woId = link.getAttribute('data-wo-id');
            
            try {
                const response = await fetch(`/admin/work-orders/${woId}`);
                if (!response.ok) throw new Error('Failed to load work order');
                
                const wo = await response.json();
                
                // Populate modal
                document.getElementById('details-modal-subtitle').textContent = `#WO-${String(wo.workOrderId).padStart(4, '0')}`;
                document.getElementById('details-source').textContent = wo.source || 'Manual';
                document.getElementById('details-asset').textContent = wo.assetName || 'N/A';
                document.getElementById('details-description').textContent = wo.description || 'No description';
                document.getElementById('details-technician').textContent = wo.assignedToName || 'Unassigned';
                document.getElementById('details-status').textContent = wo.status || 'N/A';
                document.getElementById('details-priority').textContent = wo.priority || 'N/A';
                document.getElementById('details-created-by').textContent = wo.createdBy || 'N/A';
                document.getElementById('details-start-date').textContent = wo.dateCreated ? new Date(wo.dateCreated).toLocaleDateString() : 'N/A';
                document.getElementById('details-due-date').textContent = wo.dueDate ? new Date(wo.dueDate).toLocaleDateString() : 'N/A';
                
                // Show modal
                document.getElementById('woDetailsModal').classList.add('open');
                document.body.style.overflow = 'hidden';
            } catch (error) {
                console.error('Error loading work order:', error);
                showToast('Failed to load work order details', 'error');
            }
        }
    });

    // Close details modal
    const closeDetailsModal = () => {
        document.getElementById('woDetailsModal').classList.remove('open');
        document.body.style.overflow = '';
    };
    
    document.getElementById('closeDetailsModal')?.addEventListener('click', closeDetailsModal);
    document.getElementById('closeDetailsBtn')?.addEventListener('click', closeDetailsModal);

    // ========================================
    // EDIT ACTION
    // ========================================
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-edit')) {
            e.preventDefault();
            const link = e.target.closest('.action-edit');
            const woId = link.getAttribute('data-wo-id');
            
            try {
                // Load work order details
                const response = await fetch(`/admin/work-orders/${woId}`);
                if (!response.ok) throw new Error('Failed to load work order');
                
                const wo = await response.json();
                
                // Check if can be edited
                if (wo.status === 'Completed' || wo.status === 'Cancelled') {
                    showToast('Cannot edit completed or cancelled work orders', 'error');
                    return;
                }
                
                // Store work order ID and data
                const updForm = document.getElementById('updForm');
                updForm.dataset.workOrderId = woId;
                updForm.dataset.maintenanceRequestId = wo.maintenanceRequestId || '';
                updForm.dataset.originalAssetId = wo.assetId || '';
                updForm.dataset.originalDescription = wo.description || '';
                updForm.dataset.originalPriority = wo.priority || '';
                
                // Update modal title
                document.getElementById('upd-modal-subtitle').textContent = `Editing — #WO-${String(wo.workOrderId).padStart(4, '0')}`;
                
                // Load assets and technicians
                await loadAssetsForEdit();
                await loadTechniciansForEdit();
                
                // Pre-fill form
                setTimeout(() => {
                    // Check if linked to maintenance request
                    const isLinked = wo.maintenanceRequestId != null;
                    
                    if (isLinked) {
                        // Show warning banner
                        document.getElementById('upd-warning-banner').style.display = 'block';
                        document.getElementById('upd-request-link').textContent = wo.source || 'Maintenance Request';
                        
                        // Lock Asset, Description, Priority
                        lockField('upd-equipment', true);
                        lockField('upd-issue', true);
                        lockPriorityRadios(true);
                        
                        // Show hints
                        document.getElementById('upd-asset-hint').style.display = 'block';
                        document.getElementById('upd-desc-hint').style.display = 'block';
                        document.getElementById('upd-priority-hint').style.display = 'block';
                    } else {
                        // Hide warning banner
                        document.getElementById('upd-warning-banner').style.display = 'none';
                        
                        // Unlock fields
                        lockField('upd-equipment', false);
                        lockField('upd-issue', false);
                        lockPriorityRadios(false);
                        
                        // Hide hints
                        document.getElementById('upd-asset-hint').style.display = 'none';
                        document.getElementById('upd-desc-hint').style.display = 'none';
                        document.getElementById('upd-priority-hint').style.display = 'none';
                    }
                    
                    // Fill in values
                    document.getElementById('upd-equipment').value = wo.assetId || '';
                    document.getElementById('upd-issue').value = wo.description || '';
                    document.getElementById('upd-tech').value = wo.assignedTo || '';
                    document.getElementById('upd-start').value = wo.dateCreated ? wo.dateCreated.split('T')[0] : '';
                    document.getElementById('upd-end').value = wo.dueDate ? wo.dueDate.split('T')[0] : '';
                    
                    // Set priority
                    const priorityRadios = document.querySelectorAll('input[name="upd-priority"]');
                    priorityRadios.forEach(radio => {
                        if (radio.value === wo.priority) {
                            radio.checked = true;
                        }
                    });
                }, 500);
                
                // Open modal
                document.getElementById('woUpdateModal').classList.add('open');
                document.body.style.overflow = 'hidden';
            } catch (error) {
                console.error('Error loading work order:', error);
                showToast('Failed to load work order details', 'error');
            }
        }
    });

    // Helper functions for locking fields
    function lockField(fieldId, lock) {
        const field = document.getElementById(fieldId);
        if (field) {
            field.disabled = lock;
            if (lock) {
                field.style.background = '#F0F4F8';
                field.style.color = 'var(--mx-muted)';
                field.style.cursor = 'not-allowed';
            } else {
                field.style.background = '';
                field.style.color = '';
                field.style.cursor = '';
            }
        }
    }

    function lockPriorityRadios(lock) {
        const radios = document.querySelectorAll('input[name="upd-priority"]');
        radios.forEach(radio => {
            radio.disabled = lock;
            if (lock) {
                radio.parentElement.style.opacity = '0.6';
                radio.parentElement.style.cursor = 'not-allowed';
            } else {
                radio.parentElement.style.opacity = '';
                radio.parentElement.style.cursor = '';
            }
        });
    }

    // Load assets for edit modal
    async function loadAssetsForEdit() {
        try {
            console.log('Loading assets for edit modal from /admin/assets/list...');
            
            const response = await fetch('/admin/assets/list', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });
            
            console.log('Edit modal - Response status:', response.status);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Assets endpoint error:', response.status, errorText);
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }
            
            const assets = await response.json();
            console.log('Assets loaded for edit:', assets.length, 'items');
            
            const select = document.getElementById('upd-equipment');
            
            if (!select) {
                console.error('Edit equipment select element not found!');
                return;
            }
            
            select.innerHTML = '<option value="">Select equipment…</option>';
            
            if (assets.length === 0) {
                console.warn('No assets found in database for edit modal.');
                return;
            }
            
            assets.forEach(asset => {
                const option = document.createElement('option');
                option.value = asset.value;
                option.textContent = asset.text;
                select.appendChild(option);
            });
            
            console.log('Edit modal assets dropdown populated successfully. Total options:', select.options.length);
        } catch (error) {
            console.error('Error loading assets for edit:', error);
            console.error('Error stack:', error.stack);
            showToast('Failed to load equipment list: ' + error.message, 'error');
        }
    }

    // Load technicians for edit modal
    async function loadTechniciansForEdit() {
        try {
            const response = await fetch('/admin/technicians/list');
            if (!response.ok) throw new Error('Failed to load technicians');
            
            const technicians = await response.json();
            const select = document.getElementById('upd-tech');
            
            select.innerHTML = '<option value="">Select technician…</option>';
            
            technicians.forEach(tech => {
                const option = document.createElement('option');
                option.value = tech.value;
                option.textContent = tech.text;
                select.appendChild(option);
            });
        } catch (error) {
            console.error('Error loading technicians:', error);
        }
    }

    // Close edit modal
    const closeEditModal = () => {
        document.getElementById('woUpdateModal').classList.remove('open');
        document.body.style.overflow = '';
        document.getElementById('updForm').reset();
    };
    
    document.getElementById('closeUpdModal')?.addEventListener('click', closeEditModal);
    document.getElementById('cancelUpdModal')?.addEventListener('click', closeEditModal);

    // Save edit form
    document.getElementById('saveUpdForm')?.addEventListener('click', async function(e) {
        e.preventDefault();
        
        const updForm = document.getElementById('updForm');
        const woId = updForm.dataset.workOrderId;
        const isLinked = updForm.dataset.maintenanceRequestId !== '';
        
        // Get form values
        const priority = document.querySelector('input[name="upd-priority"]:checked');
        const startDate = document.getElementById('upd-start').value;
        const dueDate = document.getElementById('upd-end').value;
        
        // For linked work orders, use original values for locked fields
        const assetId = isLinked ? parseInt(updForm.dataset.originalAssetId) : parseInt(document.getElementById('upd-equipment').value);
        const description = isLinked ? updForm.dataset.originalDescription : document.getElementById('upd-issue').value.trim();
        const priorityValue = isLinked ? updForm.dataset.originalPriority : (priority ? priority.value : 'Medium');
        
        const assignedTo = document.getElementById('upd-tech').value;
        const notes = document.getElementById('upd-notes').value.trim();
        
        // Validation
        if (!assetId || !assignedTo || !description || !priorityValue) {
            showToast('Please fill in all required fields', 'error');
            return;
        }
        
        if (!startDate || !dueDate) {
            showToast('Please fill in all date fields', 'error');
            return;
        }
        
        if (new Date(dueDate) < new Date(startDate)) {
            showToast('Expected completion must be after start date', 'error');
            return;
        }
        
        const updateData = {
            AssetId: assetId,
            Description: description,
            Priority: priorityValue,
            PersonnelId: parseInt(assignedTo),
            StartDate: new Date(startDate).toISOString(),
            ExpectedCompletion: new Date(dueDate).toISOString(),
            Notes: notes
        };
        
        console.log('Sending edit data:', updateData);
        console.log('Is linked:', isLinked);
        
        // Disable button
        const saveBtn = document.getElementById('saveUpdForm');
        saveBtn.disabled = true;
        saveBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> Saving...';
        
        try {
            const response = await fetch(`/admin/work-orders/${woId}/edit`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updateData)
            });
            
            const result = await response.json();
            
            if (response.ok && result.success) {
                closeEditModal();
                showToast(result.message || 'Work order updated successfully!', 'success');
                setTimeout(() => window.location.reload(), 1500);
            } else {
                console.error('Server error:', result);
                showToast(result.message || 'Failed to update work order', 'error');
            }
        } catch (error) {
            console.error('Error updating work order:', error);
            showToast('An error occurred while updating', 'error');
        } finally {
            saveBtn.disabled = false;
            saveBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" width="15" height="15"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg> Save Changes';
        }
    });

    // ========================================
    // UPDATE STATUS ACTION (Modal-based)
    // ========================================
    
    // Status transition rules
    const statusTransitions = {
        'Open': ['In Progress', 'Cancelled'],
        'Pending': ['In Progress', 'Cancelled'], // Added Pending as alias for Open
        'In Progress': ['Completed', 'Cancelled'],
        'Completed': [],
        'Cancelled': []
    };

    document.addEventListener('click', async function(e) {
        if (e.target.closest('.action-update-status')) {
            e.preventDefault();
            const link = e.target.closest('.action-update-status');
            const woId = link.getAttribute('data-wo-id');
            
            try {
                // Load work order details
                const response = await fetch(`/admin/work-orders/${woId}`);
                if (!response.ok) throw new Error('Failed to load work order');
                
                const wo = await response.json();
                
                console.log('Work Order Data:', wo);
                console.log('Current Status:', wo.status);
                
                // Store work order ID and current status
                const statusForm = document.getElementById('statusForm');
                statusForm.dataset.workOrderId = woId;
                statusForm.dataset.currentStatus = wo.status || 'Open';
                
                // Update modal title
                document.getElementById('status-modal-subtitle').textContent = `#WO-${String(wo.workOrderId).padStart(4, '0')}`;
                
                // Show current status
                const currentBadge = document.getElementById('status-current-badge');
                currentBadge.textContent = wo.status || 'Open';
                currentBadge.className = 'badge badge-' + getStatusClass(wo.status);
                
                // Normalize status for lookup (handle case variations)
                const normalizedStatus = wo.status || 'Open';
                console.log('Normalized Status:', normalizedStatus);
                console.log('Available transitions:', statusTransitions[normalizedStatus]);
                
                // Show allowed transitions
                const allowedTransitions = statusTransitions[normalizedStatus] || [];
                console.log('Allowed Transitions:', allowedTransitions);
                
                if (allowedTransitions.length > 0) {
                    document.getElementById('status-hint').style.display = 'block';
                    document.getElementById('status-allowed-transitions').textContent = allowedTransitions.join(', ');
                } else {
                    document.getElementById('status-hint').style.display = 'none';
                    console.warn('No allowed transitions found for status:', normalizedStatus);
                }
                
                // Populate status dropdown with only allowed transitions
                const statusSelect = document.getElementById('status-new');
                statusSelect.innerHTML = '<option value="">Select new status…</option>';
                
                if (allowedTransitions.length === 0) {
                    // If no transitions available, show a message
                    const option = document.createElement('option');
                    option.value = '';
                    option.textContent = 'No status changes available';
                    option.disabled = true;
                    statusSelect.appendChild(option);
                } else {
                    allowedTransitions.forEach(status => {
                        const option = document.createElement('option');
                        option.value = status;
                        option.textContent = status;
                        statusSelect.appendChild(option);
                    });
                }
                
                console.log('Dropdown populated with', statusSelect.options.length - 1, 'options');
                
                // Reset form
                document.getElementById('status-actual-completion-container').style.display = 'none';
                document.getElementById('status-actual-completion').value = '';
                
                // Open modal
                document.getElementById('woStatusModal').classList.add('open');
                document.body.style.overflow = 'hidden';
            } catch (error) {
                console.error('Error loading work order:', error);
                showToast('Failed to load work order details', 'error');
            }
        }
    });

    // Helper function to get status class
    function getStatusClass(status) {
        const statusLower = (status || '').toLowerCase();
        if (statusLower === 'in progress' || statusLower === 'inprogress') return 'inprog';
        if (statusLower === 'completed') return 'done';
        if (statusLower === 'cancelled') return 'cancelled';
        if (statusLower === 'open' || statusLower === 'pending') return 'pending';
        return 'pending';
    }

    // Listen for status selection changes
    document.getElementById('status-new')?.addEventListener('change', function() {
        const selectedStatus = this.value;
        const actualCompletionContainer = document.getElementById('status-actual-completion-container');
        
        if (selectedStatus === 'Completed') {
            actualCompletionContainer.style.display = 'block';
        } else {
            actualCompletionContainer.style.display = 'none';
        }
    });

    // Close status modal
    const closeStatusModal = () => {
        document.getElementById('woStatusModal').classList.remove('open');
        document.body.style.overflow = '';
        document.getElementById('statusForm').reset();
    };
    
    document.getElementById('closeStatusModal')?.addEventListener('click', closeStatusModal);
    document.getElementById('cancelStatusModal')?.addEventListener('click', closeStatusModal);

    // Save status form
    document.getElementById('saveStatusForm')?.addEventListener('click', async function(e) {
        e.preventDefault();
        
        const statusForm = document.getElementById('statusForm');
        const woId = statusForm.dataset.workOrderId;
        const newStatus = document.getElementById('status-new').value;
        const actualCompletion = document.getElementById('status-actual-completion').value;
        
        // Validation
        if (!newStatus) {
            showToast('Please select a new status', 'error');
            return;
        }
        
        if (newStatus === 'Completed' && !actualCompletion) {
            showToast('Actual completion date is required when marking as completed', 'error');
            return;
        }
        
        const statusData = {
            Status: newStatus,
            ActualCompletion: actualCompletion ? new Date(actualCompletion).toISOString() : null
        };
        
        console.log('Sending status update:', statusData);
        
        // Disable button
        const saveBtn = document.getElementById('saveStatusForm');
        saveBtn.disabled = true;
        saveBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> Updating...';
        
        try {
            const response = await fetch(`/admin/work-orders/${woId}/status`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(statusData)
            });
            
            const result = await response.json();
            
            if (response.ok && result.success) {
                closeStatusModal();
                showToast(result.message || 'Status updated successfully!', 'success');
                setTimeout(() => window.location.reload(), 1500);
            } else {
                console.error('Server error:', result);
                showToast(result.message || 'Failed to update status', 'error');
            }
        } catch (error) {
            console.error('Error updating status:', error);
            showToast('An error occurred while updating status', 'error');
        } finally {
            saveBtn.disabled = false;
            saveBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" width="15" height="15"><polyline points="20 6 9 17 4 12"/></svg> Update Status';
        }
    });

