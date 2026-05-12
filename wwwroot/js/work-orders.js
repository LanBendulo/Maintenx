// Work Orders Page - Database Integration
// Modal lifecycle is handled by work-order-modal.js

(function () {
    'use strict';

    console.log('=== Work Orders JS Initializing ===');

    // Cache form elements
    const submitBtn = document.getElementById('submitWoForm');
    const form = document.getElementById('woForm');

    if (!submitBtn || !form) {
        console.error('❌ CRITICAL: Form elements missing!');
        return;
    }

    console.log('✓ Form elements found');

    // ========================================
    // LISTEN FOR MODAL EVENTS
    // ========================================
    
    // Handle conversion data prefill (triggered by work-order-modal.js)
    document.addEventListener('prefillConversionData', async function(e) {
        const data = e.detail;
        console.log('=== Prefilling Conversion Data ===');
        console.log('Conversion data:', data);
        
        try {
            // Load assets and technicians first, then pre-fill
            console.log('Loading assets and technicians...');
            await Promise.all([loadAssets(), loadTechnicians()]);
            console.log('✓ Assets and technicians loaded successfully');
            
            // Now prefill - dropdowns are guaranteed to be populated
            prefillConversionForm(data, submitBtn, form);
            
        } catch (error) {
            console.error('Error loading assets/technicians:', error);
            showToast('Failed to load form data: ' + error.message, 'error');
            
            // Re-enable submit button even on error
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Convert to Work Order';
        }
    });
    
    // Handle manual work order data load (triggered by work-order-modal.js)
    document.addEventListener('loadManualWorkOrderData', async function() {
        console.log('=== Loading Manual Work Order Data ===');
        
        try {
            console.log('Loading assets and technicians...');
            await Promise.all([loadAssets(), loadTechnicians()]);
            console.log('✓ Manual work order data loaded successfully');
        } catch (error) {
            console.error('Error loading manual work order data:', error);
            showToast('Failed to load form data: ' + error.message, 'error');
        }
    });

    // Initialize date validation and filters
    initializeDateValidation();
    initializeFilters();

    // ========================================
    // PREFILL CONVERSION FORM
    // ========================================
    function prefillConversionForm(data, submitBtn, form) {
        console.log('=== PREFILL DEBUG ===');
        console.log('Conversion data:', data);
        
        // Asset (read-only)
        const assetSelect = document.getElementById('wo-equipment');
        console.log('Asset select element:', assetSelect);
        console.log('Available asset options:', Array.from(assetSelect.options).map(o => ({value: o.value, text: o.text})));
        console.log('Attempting to set asset to ID:', data.assetId, 'Name:', data.assetName);
        
        // Verify option exists BEFORE assigning
        const assetOptionExists = Array.from(assetSelect.options).some(o => o.value == data.assetId);
        console.log('Asset option exists in dropdown:', assetOptionExists);
        
        if (!assetOptionExists) {
            console.warn('⚠️ Asset ID', data.assetId, 'not found in dropdown options!');
            console.warn('Available asset IDs:', Array.from(assetSelect.options).map(o => o.value));
            console.warn('Requested asset ID type:', typeof data.assetId, 'Value:', data.assetId);
            
            // Add it manually as fallback
            const option = document.createElement('option');
            option.value = data.assetId;
            option.textContent = data.assetName;
            option.selected = true;
            assetSelect.appendChild(option);
            
            console.log('✓ Asset option added manually');
        } else {
            // Option exists - set it
            assetSelect.value = data.assetId;
            console.log('✓ Asset successfully set to:', assetSelect.value);
        }
        
        console.log('After setting - assetSelect.value:', assetSelect.value);
        console.log('Selected option:', assetSelect.options[assetSelect.selectedIndex]);
        
        // CRITICAL: Mark as locked AFTER setting value
        assetSelect.dataset.locked = 'true';
        assetSelect.dataset.originalValue = assetSelect.value;
        assetSelect.style.background = '#F0F4F8';
        assetSelect.style.color = '#495057';
        assetSelect.style.cursor = 'not-allowed';
        assetSelect.style.pointerEvents = 'none';
        
        console.log('Asset field locked with value:', assetSelect.value);
        
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
            radio.style.pointerEvents = 'none';
            radio.parentElement.style.opacity = '0.6';
            radio.parentElement.style.cursor = 'not-allowed';
        });
        
        // Store the request ID for submission
        form.dataset.maintenanceRequestId = data.requestId;
        
        // Update submit button text
        submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Convert to Work Order';
        
        // Re-enable submit button after prefill completes
        submitBtn.disabled = false;
        
        console.log('=== PREFILL COMPLETE ===');
        console.log('Final equipment value:', assetSelect.value);
        console.log('Form maintenanceRequestId:', form.dataset.maintenanceRequestId);
        console.log('Submit button enabled:', !submitBtn.disabled);
    }

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
            console.log('Loading technicians from /admin/technicians/list...');
            
            const response = await fetch('/admin/technicians/list', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });
            
            console.log('Technicians response status:', response.status);
            console.log('Technicians response ok:', response.ok);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Technicians endpoint error:', response.status, errorText);
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }
            
            const technicians = await response.json();
            console.log('Technicians loaded successfully:', technicians.length, 'items');
            console.log('First technician:', technicians[0]);
            
            const select = document.getElementById('wo-tech');
            console.log('Technician select element found:', select !== null);
            
            if (!select) {
                console.error('Technician select element not found!');
                return;
            }
            
            // Clear existing options except the first one
            select.innerHTML = '<option value="">Select technician…</option>';
            
            if (technicians.length === 0) {
                console.warn('No technicians found in database.');
                showToast('No technicians found. Please contact administrator.', 'error');
                return;
            }
            
            technicians.forEach(tech => {
                const option = document.createElement('option');
                option.value = tech.value;
                option.textContent = tech.text;
                select.appendChild(option);
            });
            
            console.log('Technicians dropdown populated successfully. Total options:', select.options.length);
            console.log('All technician options:', Array.from(select.options).map(o => ({value: o.value, text: o.text})));
        } catch (error) {
            console.error('Error loading technicians:', error);
            console.error('Error stack:', error.stack);
            showToast('Failed to load technicians list: ' + error.message, 'error');
        }
    }

    // ========================================
    // DATE VALIDATION & AUTO-CORRECTION
    // ========================================
    
    // Date validation helper functions (accessible to validateForm)
    function validateStartDate() {
        const startDateInput = document.getElementById('wo-start');
        const errorElement = document.getElementById('err-start');
        const today = new Date().toISOString().split('T')[0];
        
        // Clear previous errors
        startDateInput.classList.remove('input-validation-error');
        errorElement.style.display = 'none';
        
        const startDate = startDateInput.value;
        
        if (!startDate) {
            startDateInput.classList.add('input-validation-error');
            errorElement.textContent = 'Start date is required.';
            errorElement.style.display = 'block';
            return false;
        }
        
        const startDateObj = new Date(startDate);
        const todayObj = new Date(today);
        
        if (startDateObj < todayObj) {
            startDateInput.classList.add('input-validation-error');
            errorElement.textContent = 'Start date cannot be in the past.';
            errorElement.style.display = 'block';
            return false;
        }
        
        return true;
    }
    
    function validateEndDate() {
        const startDateInput = document.getElementById('wo-start');
        const endDateInput = document.getElementById('wo-end');
        const errorElement = document.getElementById('err-end');
        
        // Clear previous errors
        endDateInput.classList.remove('input-validation-error');
        errorElement.style.display = 'none';
        
        const startDate = startDateInput.value;
        const endDate = endDateInput.value;
        
        if (!endDate) {
            endDateInput.classList.add('input-validation-error');
            errorElement.textContent = 'Expected completion is required.';
            errorElement.style.display = 'block';
            return false;
        }
        
        if (!startDate) {
            // Can't validate relationship without start date
            return true;
        }
        
        const startDateObj = new Date(startDate);
        const endDateObj = new Date(endDate);
        
        if (endDateObj <= startDateObj) {
            endDateInput.classList.add('input-validation-error');
            errorElement.textContent = 'Expected completion must be after the start date.';
            errorElement.style.display = 'block';
            return false;
        }
        
        // Check duration doesn't exceed 365 days
        const durationDays = Math.ceil((endDateObj - startDateObj) / (1000 * 60 * 60 * 24));
        if (durationDays > 365) {
            endDateInput.classList.add('input-validation-error');
            errorElement.textContent = 'Schedule duration cannot exceed 365 days.';
            errorElement.style.display = 'block';
            return false;
        }
        
        return true;
    }
    
    function initializeDateValidation() {
        const startDateInput = document.getElementById('wo-start');
        const endDateInput = document.getElementById('wo-end');
        
        // Set minimum date to today for start date
        const today = new Date().toISOString().split('T')[0];
        startDateInput.setAttribute('min', today);
        
        // Initialize dates
        startDateInput.value = today;
        endDateInput.value = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
        
        // Real-time validation on start date change
        startDateInput.addEventListener('change', function() {
            validateStartDate();
            
            // Auto-correct: Update end date minimum
            endDateInput.setAttribute('min', this.value);
            
            // Auto-correct: If end date is now before start date, adjust it
            if (endDateInput.value && new Date(endDateInput.value) <= new Date(this.value)) {
                const newEndDate = new Date(this.value);
                newEndDate.setDate(newEndDate.getDate() + 1);
                endDateInput.value = newEndDate.toISOString().split('T')[0];
                
                // Show helper message
                showHelperMessage('Expected completion adjusted automatically.');
            }
            
            // Auto-fill: If end date is empty, set to +7 days
            if (!endDateInput.value) {
                const autoEndDate = new Date(this.value);
                autoEndDate.setDate(autoEndDate.getDate() + 7);
                endDateInput.value = autoEndDate.toISOString().split('T')[0];
            }
            
            // Revalidate end date
            validateEndDate();
        });
        
        // Real-time validation on end date change
        endDateInput.addEventListener('change', function() {
            validateEndDate();
        });
        
        function showHelperMessage(message) {
            // Create or update helper message
            let helperDiv = document.getElementById('date-helper-message');
            if (!helperDiv) {
                helperDiv = document.createElement('div');
                helperDiv.id = 'date-helper-message';
                helperDiv.style.cssText = 'color: var(--mx-blue); font-size: 12px; margin-top: 4px; display: flex; align-items: center; gap: 4px;';
                endDateInput.parentElement.appendChild(helperDiv);
            }
            
            helperDiv.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg> ${message}`;
            helperDiv.style.display = 'flex';
            
            // Hide after 3 seconds
            setTimeout(() => {
                helperDiv.style.display = 'none';
            }, 3000);
        }
    }

    // ========================================
    // FORM VALIDATION
    // ========================================
    function validateForm() {
        let isValid = true;
        clearErrors();

        // Equipment - Skip validation if locked (conversion mode)
        const equipment = document.getElementById('wo-equipment');
        const isLocked = equipment.dataset.locked === 'true';
        
        console.log('=== VALIDATION DEBUG ===');
        console.log('Equipment value:', equipment.value);
        console.log('Equipment locked:', isLocked);
        console.log('Equipment options:', Array.from(equipment.options).map(o => ({value: o.value, text: o.text, selected: o.selected})));
        
        if (!isLocked && !equipment.value) {
            showFieldError('wo-equipment', 'err-equip', 'Please select equipment.');
            isValid = false;
        } else if (isLocked && !equipment.value) {
            // Locked but no value - this is the bug!
            console.error('CRITICAL: Equipment is locked but has no value!');
            showFieldError('wo-equipment', 'err-equip', 'Equipment not loaded. Please close and try again.');
            isValid = false;
        }

        // Issue Description
        const issue = document.getElementById('wo-issue');
        if (!issue.value.trim()) {
            showFieldError('wo-issue', 'err-issue', 'Please enter an issue description.');
            isValid = false;
        }

        // Technician
        const tech = document.getElementById('wo-tech');
        console.log('Technician value:', tech.value);
        if (!tech.value) {
            showFieldError('wo-tech', 'err-tech', 'Please assign a technician.');
            isValid = false;
        }

        // Date validation
        const startValid = validateStartDate();
        const endValid = validateEndDate();
        
        if (!startValid || !endValid) {
            isValid = false;
        }

        console.log('=== VALIDATION RESULT:', isValid ? 'PASS' : 'FAIL', '===');

        // Disable/enable submit button based on validation
        submitBtn.disabled = !isValid;
        
        // Scroll to first error if invalid
        if (!isValid) {
            const firstError = document.querySelector('.input-validation-error');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                // Add shake animation
                firstError.classList.add('shake-animation');
                setTimeout(() => firstError.classList.remove('shake-animation'), 500);
            }
        }

        return isValid;
    }

    function showFieldError(fieldId, errorId, message) {
        const field = document.getElementById(fieldId);
        const errorElement = document.getElementById(errorId);
        
        if (field) {
            field.classList.add('input-validation-error');
        }
        
        if (errorElement) {
            errorElement.textContent = message;
            errorElement.style.display = 'block';
        }
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
        document.querySelectorAll('.input-validation-error').forEach(el => {
            el.classList.remove('input-validation-error');
        });
    }
    
    function displayBackendErrors(errors) {
        // Handle structured field errors from backend
        if (typeof errors === 'object' && !Array.isArray(errors)) {
            // Field-specific errors
            for (const [field, message] of Object.entries(errors)) {
                const fieldMap = {
                    'DateCreated': { fieldId: 'wo-start', errorId: 'err-start' },
                    'DueDate': { fieldId: 'wo-end', errorId: 'err-end' },
                    'AssetId': { fieldId: 'wo-equipment', errorId: 'err-equip' },
                    'Description': { fieldId: 'wo-issue', errorId: 'err-issue' },
                    'AssignedTo': { fieldId: 'wo-tech', errorId: 'err-tech' }
                };
                
                const mapping = fieldMap[field];
                if (mapping) {
                    showFieldError(mapping.fieldId, mapping.errorId, message);
                }
            }
            
            // Scroll to first error
            const firstError = document.querySelector('.input-validation-error');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    }

    // ========================================
    // SUBMIT WORK ORDER TO DATABASE
    // ========================================
    submitBtn.addEventListener('click', async function (e) {
        e.preventDefault();

        console.log('=== SUBMIT CLICKED ===');
        
        // Debug: Check equipment field state before validation
        const equipmentField = document.getElementById('wo-equipment');
        console.log('Equipment field state:');
        console.log('  - value:', equipmentField.value);
        console.log('  - locked:', equipmentField.dataset.locked);
        console.log('  - originalValue:', equipmentField.dataset.originalValue);
        console.log('  - selectedIndex:', equipmentField.selectedIndex);
        console.log('  - selected option:', equipmentField.options[equipmentField.selectedIndex]);

        if (!validateForm()) {
            console.log('Validation failed - aborting submit');
            return;
        }

        // Get form values
        const priority = document.querySelector('input[name="wo-priority"]:checked');
        const startDate = document.getElementById('wo-start').value;
        const dueDate = document.getElementById('wo-end').value;
        const equipmentValue = document.getElementById('wo-equipment').value;
        const techValue = document.getElementById('wo-tech').value;
        
        console.log('=== FORM VALUES ===');
        console.log('Equipment raw value:', equipmentValue);
        console.log('Technician raw value:', techValue);
        console.log('Priority:', priority ? priority.value : 'None');
        console.log('Start date:', startDate);
        console.log('Due date:', dueDate);
        
        // Build payload matching backend DTO exactly
        const workOrderData = {
            AssetId: parseInt(equipmentValue),
            Description: document.getElementById('wo-issue').value.trim(),
            AssignedTo: parseInt(techValue),
            Priority: priority ? priority.value : 'Medium',
            DateCreated: startDate ? new Date(startDate).toISOString() : new Date().toISOString(),
            DueDate: dueDate ? new Date(dueDate).toISOString() : new Date().toISOString(),
            Notes: document.getElementById('wo-notes').value.trim(),
            MaintenanceRequestId: form.dataset.maintenanceRequestId ? parseInt(form.dataset.maintenanceRequestId) : null
        };

        console.log('=== PAYLOAD DEBUG ===');
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
            console.error('❌ CRITICAL: Invalid AssetId:', workOrderData.AssetId);
            console.error('Equipment field value was:', equipmentValue);
            console.error('Equipment field state:', {
                value: equipmentField.value,
                locked: equipmentField.dataset.locked,
                options: Array.from(equipmentField.options).map(o => ({value: o.value, text: o.text, selected: o.selected}))
            });
            showToast('Please select equipment', 'error');
            showFieldError('wo-equipment', 'err-equip', 'Invalid equipment selection. Please try again.');
            return;
        }
        
        if (isNaN(workOrderData.AssignedTo) || workOrderData.AssignedTo <= 0) {
            console.error('❌ Invalid AssignedTo:', workOrderData.AssignedTo);
            showToast('Please assign a technician', 'error');
            return;
        }
        
        if (!workOrderData.Description || workOrderData.Description.length === 0) {
            console.error('❌ Description is empty');
            showToast('Please enter an issue description', 'error');
            return;
        }

        console.log('✓ Pre-submit validation passed');

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
                WorkOrderModal.close();
                showToast(result.message || 'Work order created successfully!', 'success');
                
                // Reload the page after 1.5 seconds to show the new work order
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                // Error - display inline validation errors
                console.error('=== Server Error ===');
                console.error('Response status:', response.status);
                console.error('Server response:', result);
                
                // Display field-specific errors if available
                if (result.errors) {
                    displayBackendErrors(result.errors);
                } else {
                    // Generic error message
                    let errorMessage = result.message || 'Failed to create work order';
                    showToast(errorMessage, 'error');
                }
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
    function initializeFilters() {
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
                
                // Populate cost data
                loadCostData(wo);
                
                // Load parts used
                loadPartsUsed(woId, wo.status);
                
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
    // COST TRACKING FUNCTIONALITY
    // ========================================
    
    // Global variable to store current parts cost
    let currentPartsCost = 0;
    
    /**
     * Load and populate cost data in the details modal
     */
    function loadCostData(wo) {
        console.log('Loading cost data:', wo);
        
        // Store work order ID and status in hidden fields
        document.getElementById('cost-work-order-id').value = wo.workOrderId;
        document.getElementById('cost-status').value = wo.status;
        
        // Store parts cost in global variable
        currentPartsCost = wo.partsCost || 0;
        
        // Populate cost fields with currency formatting
        document.getElementById('cost-parts').textContent = formatCurrency(currentPartsCost);
        document.getElementById('cost-labor').value = wo.laborCost || 0;
        document.getElementById('cost-other').value = wo.otherCost || 0;
        document.getElementById('cost-total').textContent = formatCurrency(wo.totalCost || 0);
        
        // Check if work order is completed - lock inputs
        const isCompleted = wo.status === WorkOrderStatuses.COMPLETED || wo.status === WorkOrderStatuses.CANCELLED;
        const laborInput = document.getElementById('cost-labor');
        const otherInput = document.getElementById('cost-other');
        const saveBtn = document.getElementById('saveCostBtn');
        const lockedMessage = document.getElementById('cost-locked-message');
        
        if (isCompleted) {
            // Disable inputs
            laborInput.disabled = true;
            otherInput.disabled = true;
            laborInput.style.background = '#F0F4F8';
            laborInput.style.cursor = 'not-allowed';
            otherInput.style.background = '#F0F4F8';
            otherInput.style.cursor = 'not-allowed';
            
            // Hide save button and show locked message
            saveBtn.style.display = 'none';
            lockedMessage.style.display = 'block';
        } else {
            // Enable inputs
            laborInput.disabled = false;
            otherInput.disabled = false;
            laborInput.style.background = '';
            laborInput.style.cursor = '';
            otherInput.style.background = '';
            otherInput.style.cursor = '';
            
            // Show save button and hide locked message
            saveBtn.style.display = 'block';
            lockedMessage.style.display = 'none';
        }
    }
    
    /**
     * Format number as currency (Philippine Peso)
     */
    function formatCurrency(amount) {
        return '₱ ' + parseFloat(amount).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }
    
    /**
     * Calculate total cost in real-time
     */
    function calculateTotalCost() {
        const laborCost = parseFloat(document.getElementById('cost-labor').value) || 0;
        const otherCost = parseFloat(document.getElementById('cost-other').value) || 0;
        
        // Use global currentPartsCost variable
        const totalCost = currentPartsCost + laborCost + otherCost;
        document.getElementById('cost-total').textContent = formatCurrency(totalCost);
    }
    
    // Add event listeners for real-time calculation
    document.getElementById('cost-labor')?.addEventListener('input', calculateTotalCost);
    document.getElementById('cost-other')?.addEventListener('input', calculateTotalCost);
    
    /**
     * Save cost updates
     */
    document.getElementById('saveCostBtn')?.addEventListener('click', async function(e) {
        e.preventDefault();
        
        const woId = document.getElementById('cost-work-order-id').value;
        const laborCost = parseFloat(document.getElementById('cost-labor').value) || 0;
        const otherCost = parseFloat(document.getElementById('cost-other').value) || 0;
        
        // Validation
        if (laborCost < 0 || otherCost < 0) {
            showToast('Costs cannot be negative', 'error');
            return;
        }
        
        const costData = {
            LaborCost: laborCost,
            OtherCost: otherCost
        };
        
        console.log('Saving cost data:', costData);
        
        // Disable button
        const saveBtn = document.getElementById('saveCostBtn');
        const originalText = saveBtn.innerHTML;
        saveBtn.disabled = true;
        saveBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> Saving...';
        
        try {
            const response = await fetch(`/admin/work-orders/${woId}/update-cost`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(costData)
            });
            
            const result = await response.json();
            
            if (response.ok && result.success) {
                // Update global parts cost variable
                currentPartsCost = result.partsCost || 0;
                
                // Update displayed values with server response
                document.getElementById('cost-parts').textContent = formatCurrency(result.partsCost || 0);
                document.getElementById('cost-total').textContent = formatCurrency(result.totalCost || 0);
                
                showToast(result.message || 'Cost updated successfully!', 'success');
            } else {
                console.error('Server error:', result);
                showToast(result.message || 'Failed to update cost', 'error');
            }
        } catch (error) {
            console.error('Error updating cost:', error);
            showToast('An error occurred while updating cost', 'error');
        } finally {
            saveBtn.disabled = false;
            saveBtn.innerHTML = originalText;
        }
    });

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
                if (wo.status === WorkOrderStatuses.COMPLETED || wo.status === WorkOrderStatuses.CANCELLED) {
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
    
    // ============================================================================
    // WORK ORDER STATUS CONSTANTS
    // ============================================================================
    // NOTE: These constants are for FRONTEND DISPLAY/WORKFLOW ONLY
    // Backend (WorkOrderStatuses.cs) is the authoritative source of truth
    // Any changes to statuses MUST be made in backend first
    // ============================================================================
    const WorkOrderStatuses = {
        PENDING: 'Pending',
        IN_PROGRESS: 'In Progress',
        COMPLETED: 'Completed',
        CANCELLED: 'Cancelled'
    };

    // Status transition rules (must match backend WorkOrderStatuses.GetValidTransitions)
    const statusTransitions = {
        'Pending': [WorkOrderStatuses.IN_PROGRESS, WorkOrderStatuses.CANCELLED],
        'In Progress': [WorkOrderStatuses.COMPLETED, WorkOrderStatuses.CANCELLED],
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
                statusForm.dataset.currentStatus = wo.status || 'Pending';
                
                // Update modal title
                document.getElementById('status-modal-subtitle').textContent = `#WO-${String(wo.workOrderId).padStart(4, '0')}`;
                
                // Show current status
                const currentBadge = document.getElementById('status-current-badge');
                currentBadge.textContent = wo.status || 'Pending';
                currentBadge.className = 'badge badge-' + getStatusClass(wo.status);
                
                // Normalize status for lookup (handle case variations)
                const normalizedStatus = wo.status || 'Pending';
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
        if (statusLower === 'pending') return 'pending';
        return 'pending'; // Default to pending
    }

    // Listen for status selection changes
    document.getElementById('status-new')?.addEventListener('change', function() {
        const selectedStatus = this.value;
        const actualCompletionContainer = document.getElementById('status-actual-completion-container');
        
        if (selectedStatus === WorkOrderStatuses.COMPLETED) {
            actualCompletionContainer.style.display = 'block';
            // Set default to today
            const today = new Date().toISOString().split('T')[0];
            document.getElementById('status-actual-completion').value = today;
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
        
        if (newStatus === WorkOrderStatuses.COMPLETED && !actualCompletion) {
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


    // ========================================
    // ARCHIVE WORK ORDER FUNCTIONALITY
    // ========================================

    // Archive action click handler
    document.addEventListener('click', function(e) {
        if (e.target.closest('.action-archive')) {
            e.preventDefault();
            const link = e.target.closest('.action-archive');
            const woId = link.getAttribute('data-wo-id');
            console.log('Archive button clicked for WO ID:', woId);
            showArchiveModal(woId);
        }
    });

    // Close archive modal handlers
    document.getElementById('closeArchiveModal')?.addEventListener('click', closeArchiveModal);
    document.getElementById('cancelArchiveModal')?.addEventListener('click', closeArchiveModal);

    // Archive work order button
    document.getElementById('saveArchiveForm')?.addEventListener('click', function(e) {
        e.preventDefault();
        archiveWorkOrder();
    });


    // ========================================
    // PARTS USED FUNCTIONALITY
    // ========================================

    /**
     * Load parts used in a work order
     */
    async function loadPartsUsed(workOrderId, status) {
        try {
            const response = await fetch(`/admin/work-orders/${workOrderId}/parts`);
            if (!response.ok) throw new Error('Failed to load parts');
            
            const result = await response.json();
            
            if (result.success) {
                displayPartsUsed(result.parts, result.totalPartsCost, status);
            }
        } catch (error) {
            console.error('Error loading parts:', error);
            showToast('Failed to load parts used', 'error');
        }
    }

    /**
     * Display parts used in the table
     */
    function displayPartsUsed(parts, totalPartsCost, status) {
        const tbody = document.getElementById('parts-used-tbody');
        const noPartsRow = document.getElementById('no-parts-row');
        const addPartBtn = document.getElementById('addPartBtn');
        
        // Clear existing rows except the "no parts" row
        const existingRows = tbody.querySelectorAll('tr:not(#no-parts-row)');
        existingRows.forEach(row => row.remove());
        
        if (parts.length === 0) {
            noPartsRow.style.display = '';
        } else {
            noPartsRow.style.display = 'none';
            
            parts.forEach(part => {
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td>${part.partName}</td>
                    <td style="color:var(--mx-muted);font-size:13px;">${part.partNumber || '-'}</td>
                    <td style="text-align:right;font-weight:600;">${part.quantityUsed}</td>
                    <td style="text-align:right;color:var(--mx-primary);">${formatCurrency(part.unitCostSnapshot)}</td>
                    <td style="text-align:right;font-weight:700;color:var(--mx-green);">${formatCurrency(part.totalCost)}</td>
                    <td style="text-align:center;">
                        <button class="btn-remove-part" data-part-id="${part.id}" style="padding:4px 8px;font-size:12px;background:#DC3545;color:white;border:none;border-radius:4px;cursor:pointer;">
                            Remove
                        </button>
                    </td>
                `;
                tbody.appendChild(row);
            });
        }
        
        // Update Parts Used section total
        document.getElementById('parts-used-total').textContent = formatCurrency(totalPartsCost);
        
        // Update global parts cost variable
        currentPartsCost = totalPartsCost;
        
        // Update Cost Breakdown section - Parts Cost field
        document.getElementById('cost-parts').textContent = formatCurrency(totalPartsCost);
        
        // Recalculate total cost
        calculateTotalCost();
        
        // Disable add/remove if completed or cancelled
        const isCompleted = status === WorkOrderStatuses.COMPLETED || status === WorkOrderStatuses.CANCELLED;
        if (isCompleted) {
            addPartBtn.style.display = 'none';
            document.querySelectorAll('.btn-remove-part').forEach(btn => {
                btn.style.display = 'none';
            });
        } else {
            addPartBtn.style.display = 'inline-flex';
        }
    }

    /**
     * Open Add Part modal
     */
    document.getElementById('addPartBtn')?.addEventListener('click', async function() {
        const woId = document.getElementById('cost-work-order-id').value;
        
        if (!woId) {
            showToast('Work order ID not found', 'error');
            return;
        }
        
        // Store work order ID
        document.getElementById('add-part-work-order-id').value = woId;
        
        // Load available parts
        await loadAvailableParts();
        
        // Reset form
        document.getElementById('addPartForm').reset();
        document.getElementById('part-info-display').style.display = 'none';
        document.getElementById('add-part-hint-quantity').style.display = 'none';
        document.getElementById('add-part-estimated-cost').textContent = '₱ 0.00';
        
        // Open modal
        document.getElementById('addPartModal').classList.add('open');
        document.body.style.overflow = 'hidden';
    });

    /**
     * Load available parts from inventory
     */
    async function loadAvailableParts() {
        try {
            const response = await fetch('/admin/parts/available');
            if (!response.ok) throw new Error('Failed to load parts');
            
            const parts = await response.json();
            const select = document.getElementById('add-part-select');
            
            // Store parts data for later use
            select.dataset.partsData = JSON.stringify(parts);
            
            // Clear and populate dropdown
            select.innerHTML = '<option value="">Select part…</option>';
            
            if (parts.length === 0) {
                const option = document.createElement('option');
                option.value = '';
                option.textContent = 'No parts available in inventory';
                option.disabled = true;
                select.appendChild(option);
            } else {
                parts.forEach(part => {
                    const option = document.createElement('option');
                    option.value = part.value;
                    option.textContent = part.text;
                    select.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading parts:', error);
            showToast('Failed to load parts list', 'error');
        }
    }

    /**
     * Handle part selection change
     */
    document.getElementById('add-part-select')?.addEventListener('change', function() {
        const partId = this.value;
        const partsData = JSON.parse(this.dataset.partsData || '[]');
        const selectedPart = partsData.find(p => p.value == partId);
        
        if (selectedPart) {
            // Show part info
            document.getElementById('part-info-display').style.display = 'block';
            document.getElementById('part-available-qty').textContent = selectedPart.availableQuantity;
            document.getElementById('part-unit-cost').textContent = formatCurrency(selectedPart.unitCost);
            document.getElementById('part-location').textContent = selectedPart.location || '-';
            
            // Show quantity hint
            document.getElementById('add-part-hint-quantity').style.display = 'block';
            document.getElementById('add-part-max-qty').textContent = selectedPart.availableQuantity;
            
            // Set max quantity
            document.getElementById('add-part-quantity').max = selectedPart.availableQuantity;
            
            // Store unit cost for calculation
            document.getElementById('add-part-quantity').dataset.unitCost = selectedPart.unitCost;
            
            // Reset quantity
            document.getElementById('add-part-quantity').value = '';
            document.getElementById('add-part-estimated-cost').textContent = '₱ 0.00';
        } else {
            document.getElementById('part-info-display').style.display = 'none';
            document.getElementById('add-part-hint-quantity').style.display = 'none';
        }
    });

    /**
     * Calculate estimated cost as user types quantity
     */
    document.getElementById('add-part-quantity')?.addEventListener('input', function() {
        const quantity = parseFloat(this.value) || 0;
        const unitCost = parseFloat(this.dataset.unitCost) || 0;
        const estimatedCost = quantity * unitCost;
        
        document.getElementById('add-part-estimated-cost').textContent = formatCurrency(estimatedCost);
    });

    /**
     * Close Add Part modal
     */
    const closeAddPartModal = () => {
        document.getElementById('addPartModal').classList.remove('open');
        document.body.style.overflow = '';
        document.getElementById('addPartForm').reset();
    };
    
    document.getElementById('closeAddPartModal')?.addEventListener('click', closeAddPartModal);
    document.getElementById('cancelAddPartModal')?.addEventListener('click', closeAddPartModal);

    /**
     * Save Add Part form
     */
    document.getElementById('saveAddPartForm')?.addEventListener('click', async function(e) {
        e.preventDefault();
        
        const woId = document.getElementById('add-part-work-order-id').value;
        const partId = document.getElementById('add-part-select').value;
        const quantity = parseInt(document.getElementById('add-part-quantity').value);
        const maxQty = parseInt(document.getElementById('add-part-quantity').max);
        
        // Validation
        if (!partId) {
            showToast('Please select a part', 'error');
            return;
        }
        
        if (!quantity || quantity <= 0) {
            showToast('Please enter a valid quantity', 'error');
            return;
        }
        
        if (quantity > maxQty) {
            showToast(`Quantity exceeds available stock (${maxQty})`, 'error');
            return;
        }
        
        const partData = {
            PartId: parseInt(partId),
            QuantityUsed: quantity
        };
        
        console.log('Adding part:', partData);
        
        // Disable button
        const saveBtn = document.getElementById('saveAddPartForm');
        const originalText = saveBtn.innerHTML;
        saveBtn.disabled = true;
        saveBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> Adding...';
        
        try {
            const response = await fetch(`/admin/work-orders/${woId}/add-part`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(partData)
            });
            
            const result = await response.json();
            
            if (response.ok && result.success) {
                closeAddPartModal();
                showToast(result.message || 'Part added successfully!', 'success');
                
                // Reload parts used (this will update global variable and recalculate)
                const status = document.getElementById('cost-status').value;
                await loadPartsUsed(woId, status);
            } else {
                console.error('Server error:', result);
                showToast(result.message || 'Failed to add part', 'error');
            }
        } catch (error) {
            console.error('Error adding part:', error);
            showToast('An error occurred while adding part', 'error');
        } finally {
            saveBtn.disabled = false;
            saveBtn.innerHTML = originalText;
        }
    });

    /**
     * Handle remove part button clicks
     */
    document.addEventListener('click', async function(e) {
        if (e.target.closest('.btn-remove-part')) {
            const btn = e.target.closest('.btn-remove-part');
            const workOrderPartId = btn.getAttribute('data-part-id');
            const woId = document.getElementById('cost-work-order-id').value;
            
            if (!confirm('Are you sure you want to remove this part? The quantity will be restored to inventory.')) {
                return;
            }
            
            try {
                const response = await fetch(`/admin/work-orders/${woId}/remove-part`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ WorkOrderPartId: parseInt(workOrderPartId) })
                });
                
                const result = await response.json();
                
                if (response.ok && result.success) {
                    showToast(result.message || 'Part removed successfully!', 'success');
                    
                    // Reload parts used (this will update global variable and recalculate)
                    const status = document.getElementById('cost-status').value;
                    await loadPartsUsed(woId, status);
                } else {
                    console.error('Server error:', result);
                    showToast(result.message || 'Failed to remove part', 'error');
                }
            } catch (error) {
                console.error('Error removing part:', error);
                showToast('An error occurred while removing part', 'error');
            }
        }
    });

})();


// ============================================================
// WORK ORDER ARCHIVE OPERATIONS
// ============================================================

/**
 * Shows archive confirmation modal
 */
function showArchiveModal(workOrderId) {
    console.log('showArchiveModal called with ID:', workOrderId);
    
    const modal = document.getElementById('woArchiveModal');
    if (!modal) {
        console.error('Archive modal not found');
        return;
    }
    
    console.log('Modal found:', modal);

    // Set work order ID in modal
    document.getElementById('archive-work-order-id').value = workOrderId;
    document.getElementById('archive-reason').value = '';
    
    // Clear error state
    const errorDiv = document.getElementById('archive-err-reason');
    if (errorDiv) errorDiv.style.display = 'none';

    console.log('Fetching can-archive status for WO:', workOrderId);
    
    // Check if work order can be archived and fetch details
    fetch(`/admin/work-orders/${workOrderId}/can-archive`)
        .then(response => {
            console.log('Can-archive response status:', response.status);
            return response.json();
        })
        .then(data => {
            console.log('Can-archive data:', data);
            
            if (!data.canArchive) {
                console.warn('Cannot archive:', data.message);
                showToast(data.message, 'error');
                return;
            }

            console.log('✅ Can archive - proceeding to open modal');

            // Fetch work order details to populate modal
            const workOrderRow = document.querySelector(`tr[data-status] a[data-wo-id="${workOrderId}"]`);
            console.log('Work order row found:', workOrderRow);
            
            if (workOrderRow) {
                const row = workOrderRow.closest('tr');
                const equipment = row.querySelector('.td-equip')?.textContent || '-';
                const status = row.querySelector('td:nth-child(6)')?.textContent.trim() || '-';
                const technician = row.querySelector('.td-staff')?.textContent.trim() || '-';
                
                console.log('Populating modal with:', { equipment, status, technician });
                
                document.getElementById('archive-equipment').textContent = equipment;
                document.getElementById('archive-status').textContent = status;
                document.getElementById('archive-technician').textContent = technician;
            }
            
            // Update modal subtitle
            document.getElementById('archive-modal-subtitle').textContent = `#WO-${String(workOrderId).padStart(4, '0')}`;

            console.log('About to show modal...');
            console.log('Modal display before:', modal.style.display);
            console.log('Modal classes before:', modal.className);
            
            // Show modal - add 'open' class for CSS transition
            modal.classList.add('open');
            
            console.log('Modal display after:', modal.style.display);
            console.log('Modal classes after:', modal.className);
            console.log('✅ Modal should now be visible');
        })
        .catch(error => {
            console.error('Error checking archive eligibility:', error);
            showToast('Error checking if work order can be archived', 'error');
        });
}

/**
 * Closes archive modal
 */
function closeArchiveModal() {
    const modal = document.getElementById('woArchiveModal');
    if (modal) {
        modal.classList.remove('open');
    }
}

/**
 * Archives a work order
 */
function archiveWorkOrder() {
    console.log('🚀 archiveWorkOrder() called');
    
    const workOrderId = document.getElementById('archive-work-order-id').value;
    const archiveReason = document.getElementById('archive-reason').value.trim();
    const errorDiv = document.getElementById('archive-err-reason');

    console.log('Work Order ID:', workOrderId);
    console.log('Archive Reason:', archiveReason);

    if (!archiveReason) {
        console.warn('❌ Archive reason is empty');
        if (errorDiv) errorDiv.style.display = 'block';
        return;
    }
    
    if (errorDiv) errorDiv.style.display = 'none';

    console.log('✅ Validation passed, sending archive request...');

    const formData = new FormData();
    formData.append('archiveReason', archiveReason);
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    console.log('Sending POST to:', `/admin/work-orders/${workOrderId}/archive`);

    fetch(`/admin/work-orders/${workOrderId}/archive`, {
        method: 'POST',
        body: formData
    })
    .then(response => {
        console.log('Archive response status:', response.status);
        return response.json();
    })
    .then(data => {
        console.log('Archive response data:', data);
        
        if (data.success) {
            console.log('✅ Archive successful!');
            
            // Show success toast
            const toast = document.getElementById('wo-archive-toast');
            if (toast) {
                toast.classList.add('show');
                setTimeout(() => toast.classList.remove('show'), 3000);
            }
            
            closeArchiveModal();
            
            // Reload page to update work order list
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            console.error('❌ Archive failed:', data.message);
            showToast(data.message, 'error');
        }
    })
    .catch(error => {
        console.error('❌ Error archiving work order:', error);
        showToast('Error archiving work order', 'error');
    });
}

/**
 * Restores an archived work order
 */
function restoreWorkOrder(workOrderId) {
    if (!confirm('Are you sure you want to restore this work order?')) {
        return;
    }

    const formData = new FormData();
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    fetch(`/admin/work-orders/${workOrderId}/restore`, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showToast(data.message, 'success');
            
            // Reload page
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showToast(data.message, 'error');
        }
    })
    .catch(error => {
        console.error('Error restoring work order:', error);
        showToast('Error restoring work order', 'error');
    });
}

// ============================================================
// ARCHIVE EVENT LISTENERS (Must be outside IIFE)
// ============================================================

// Archive action click handler
document.addEventListener('click', function(e) {
    if (e.target.closest('.action-archive')) {
        e.preventDefault();
        const link = e.target.closest('.action-archive');
        const woId = link.getAttribute('data-wo-id');
        console.log('Archive button clicked for WO ID:', woId);
        showArchiveModal(woId);
    }
});

// Close archive modal handlers
document.getElementById('closeArchiveModal')?.addEventListener('click', closeArchiveModal);
document.getElementById('cancelArchiveModal')?.addEventListener('click', closeArchiveModal);

// Archive work order button
document.getElementById('saveArchiveForm')?.addEventListener('click', function(e) {
    e.preventDefault();
    console.log('🔥 Archive confirm button clicked!');
    archiveWorkOrder();
});

// Close modal when clicking outside
window.onclick = function(event) {
    const archiveModal = document.getElementById('woArchiveModal');
    if (event.target == archiveModal) {
        closeArchiveModal();
    }
}
