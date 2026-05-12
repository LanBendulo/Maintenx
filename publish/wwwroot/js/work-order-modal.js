// Work Order Modal Lifecycle Management
// Handles ONLY: modal open/close, conversion auto-open, overlay, escape key
// All operational logic remains in work-orders.js

(function () {
    'use strict';

    console.log('=== Work Order Modal JS Initializing ===');

    // Modal state
    let modalElements = null;

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        console.log('=== Modal Lifecycle Initializing ===');
        
        // Cache modal elements
        cacheElements();
        
        // Bind event listeners
        bindEvents();
        
        // Check for conversion auto-open
        initializeConversionFlow();
        
        console.log('✓ Modal lifecycle initialized');
    }

    /**
     * Cache all modal-related DOM elements
     */
    function cacheElements() {
        modalElements = {
            overlay: document.getElementById('woModal'),
            openBtn: document.getElementById('openWoModal'),
            closeBtn: document.getElementById('closeWoModal'),
            cancelBtn: document.getElementById('cancelWoModal'),
            form: document.getElementById('woForm'),
            submitBtn: document.getElementById('submitWoForm'),
            conversionBanner: document.getElementById('conversion-mode-banner')
        };

        console.log('Modal elements cached:', {
            overlay: !!modalElements.overlay,
            openBtn: !!modalElements.openBtn,
            closeBtn: !!modalElements.closeBtn,
            cancelBtn: !!modalElements.cancelBtn,
            form: !!modalElements.form,
            submitBtn: !!modalElements.submitBtn,
            conversionBanner: !!modalElements.conversionBanner
        });

        // Validate critical elements
        if (!modalElements.overlay) {
            console.error('❌ CRITICAL: Modal overlay #woModal not found!');
            return false;
        }

        if (!modalElements.openBtn) {
            console.error('❌ CRITICAL: Open button #openWoModal not found!');
            return false;
        }

        return true;
    }

    /**
     * Bind event listeners for modal controls
     */
    function bindEvents() {
        if (!modalElements.overlay || !modalElements.openBtn) {
            console.error('Cannot bind events - elements missing');
            return;
        }

        // Open modal button
        modalElements.openBtn.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('=== Manual Work Order Button Clicked ===');
            openManualWorkOrder();
        });

        // Close button
        if (modalElements.closeBtn) {
            modalElements.closeBtn.addEventListener('click', function(e) {
                e.preventDefault();
                console.log('Close button clicked');
                close();
            });
        }

        // Cancel button
        if (modalElements.cancelBtn) {
            modalElements.cancelBtn.addEventListener('click', function(e) {
                e.preventDefault();
                console.log('Cancel button clicked');
                close();
            });
        }

        // Overlay click (close on backdrop)
        modalElements.overlay.addEventListener('click', function(e) {
            if (e.target === modalElements.overlay) {
                console.log('Overlay clicked - closing modal');
                close();
            }
        });

        // Escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && modalElements.overlay.classList.contains('open')) {
                console.log('Escape key pressed - closing modal');
                close();
            }
        });

        console.log('✓ Event listeners bound');
    }

    /**
     * Open modal for manual work order creation
     */
    function openManualWorkOrder() {
        console.log('=== Opening Manual Work Order Modal ===');
        
        if (!modalElements.overlay) {
            console.error('Cannot open modal - overlay element missing');
            return;
        }

        // Reset form
        resetForm();

        // Hide conversion banner
        if (modalElements.conversionBanner) {
            modalElements.conversionBanner.style.display = 'none';
        }

        // Update modal title
        const modalTitle = document.querySelector('#woModal .modal-title');
        const modalSubtitle = document.querySelector('#woModal .modal-subtitle');
        if (modalTitle) modalTitle.textContent = 'Create Manual Work Order';
        if (modalSubtitle) modalSubtitle.textContent = 'Create a work order without a maintenance request';

        // Update submit button text
        if (modalElements.submitBtn) {
            modalElements.submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Create Manual Work Order';
        }

        // Clear maintenanceRequestId from form
        if (modalElements.form) {
            delete modalElements.form.dataset.maintenanceRequestId;
        }

        // Trigger event for work-orders.js to load data
        console.log('Dispatching loadManualWorkOrderData event');
        document.dispatchEvent(new CustomEvent('loadManualWorkOrderData'));

        // Open modal
        modalElements.overlay.classList.add('open');
        document.body.style.overflow = 'hidden';

        console.log('✓ Manual work order modal opened');
    }

    /**
     * Open modal for converting maintenance request to work order
     */
    function openFromRequest(requestData) {
        console.log('=== Opening Conversion Modal ===');
        console.log('Request data:', requestData);

        if (!modalElements.overlay) {
            console.error('Cannot open modal - overlay element missing');
            return;
        }

        // Reset form first
        resetForm();

        // Show conversion banner
        if (modalElements.conversionBanner) {
            modalElements.conversionBanner.style.display = 'block';
        }

        // Update modal title
        const modalTitle = document.querySelector('#woModal .modal-title');
        const modalSubtitle = document.querySelector('#woModal .modal-subtitle');
        if (modalTitle) modalTitle.textContent = 'Convert to Work Order';
        if (modalSubtitle) modalSubtitle.textContent = `Converting Request #${requestData.requestNumber || 'N/A'}`;

        // Disable submit button until prefill completes
        if (modalElements.submitBtn) {
            modalElements.submitBtn.disabled = true;
            modalElements.submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/></svg> Loading...';
        }

        // Open modal
        modalElements.overlay.classList.add('open');
        document.body.style.overflow = 'hidden';

        console.log('✓ Conversion modal opened');

        // Dispatch event for work-orders.js to prefill data
        console.log('Dispatching prefillConversionData event');
        document.dispatchEvent(new CustomEvent('prefillConversionData', {
            detail: requestData
        }));
    }

    /**
     * Close modal
     */
    function close() {
        console.log('=== Closing Modal ===');

        if (!modalElements.overlay) {
            console.error('Cannot close modal - overlay element missing');
            return;
        }

        modalElements.overlay.classList.remove('open');
        document.body.style.overflow = '';

        // Reset form after animation completes
        setTimeout(() => {
            resetForm();
        }, 300);

        console.log('✓ Modal closed');
    }

    /**
     * Reset form to initial state
     */
    function resetForm() {
        console.log('Resetting form');

        if (!modalElements.form) {
            console.warn('Form element not found - cannot reset');
            return;
        }

        // Reset form fields
        modalElements.form.reset();

        // Clear validation errors
        document.querySelectorAll('.input-error').forEach(el => {
            el.style.display = 'none';
        });
        document.querySelectorAll('.input-validation-error').forEach(el => {
            el.classList.remove('input-validation-error');
        });

        // Unlock all fields (in case they were locked for conversion)
        const equipmentField = document.getElementById('wo-equipment');
        if (equipmentField) {
            delete equipmentField.dataset.locked;
            delete equipmentField.dataset.originalValue;
            equipmentField.style.background = '';
            equipmentField.style.color = '';
            equipmentField.style.cursor = '';
            equipmentField.style.pointerEvents = '';
            equipmentField.disabled = false;
        }

        const issueField = document.getElementById('wo-issue');
        if (issueField) {
            issueField.readOnly = false;
            issueField.style.background = '';
            issueField.style.color = '';
            issueField.style.cursor = '';
        }

        const priorityRadios = document.querySelectorAll('input[name="wo-priority"]');
        priorityRadios.forEach(radio => {
            radio.style.pointerEvents = '';
            radio.disabled = false;
            if (radio.parentElement) {
                radio.parentElement.style.opacity = '';
                radio.parentElement.style.cursor = '';
            }
        });

        // Clear dataset
        delete modalElements.form.dataset.maintenanceRequestId;

        // Re-enable submit button
        if (modalElements.submitBtn) {
            modalElements.submitBtn.disabled = false;
            modalElements.submitBtn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg> Create Manual Work Order';
        }

        console.log('✓ Form reset complete');
    }

    /**
     * Check for conversion querystring and auto-open modal
     */
    function initializeConversionFlow() {
        console.log('=== Checking for Conversion Querystring ===');

        const urlParams = new URLSearchParams(window.location.search);
        const convertRequestId = urlParams.get('convertRequestId');

        if (convertRequestId) {
            console.log('✓ Conversion detected - Request ID:', convertRequestId);

            // Clean URL immediately (before async fetch)
            const cleanUrl = window.location.pathname;
            window.history.replaceState({}, document.title, cleanUrl);
            console.log('✓ URL cleaned');

            // Fetch request details and open modal
            fetchRequestDetailsAndOpen(convertRequestId);
        } else {
            console.log('No conversion querystring found');
        }
    }

    /**
     * Fetch request details from backend and open conversion modal
     */
    async function fetchRequestDetailsAndOpen(requestId) {
        console.log('=== Fetching Request Details ===');
        console.log('Request ID:', requestId);

        try {
            const response = await fetch(`/admin/work-orders/request-details/${requestId}`);
            console.log('Response status:', response.status);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Failed to fetch request details:', response.status, errorText);
                showToast('Failed to load maintenance request details', 'error');
                return;
            }

            const data = await response.json();
            console.log('Request details loaded:', data);

            if (!data.success) {
                console.error('Backend returned error:', data.message);
                showToast(data.message || 'Failed to load request details', 'error');
                return;
            }

            // Open modal with request data
            openFromRequest(data);

        } catch (error) {
            console.error('Error fetching request details:', error);
            console.error('Error stack:', error.stack);
            showToast('An error occurred while loading request details', 'error');
        }
    }

    /**
     * Show toast notification
     */
    function showToast(message, type = 'success') {
        const toast = document.getElementById('wo-toast');
        if (!toast) {
            console.warn('Toast element not found');
            return;
        }

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

    // Expose public API
    window.WorkOrderModal = {
        init: init,
        open: openManualWorkOrder,
        openFromRequest: openFromRequest,
        close: close,
        resetForm: resetForm
    };

    console.log('✓ WorkOrderModal namespace exposed');

})();
