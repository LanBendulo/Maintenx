(function() {
    let isEditMode = false;
    let currentAssetId = null;

    document.addEventListener('DOMContentLoaded', function() {
        loadCategories();
        setupEventListeners();
    });

    function setupEventListeners() {
        const openBtn = document.getElementById('openAssetModal');
        if (openBtn) {
            openBtn.addEventListener('click', openCreateModal);
        }

        document.getElementById('closeAssetModal').addEventListener('click', closeModal);
        document.getElementById('cancelAssetModal').addEventListener('click', closeModal);
        document.getElementById('submitAssetForm').addEventListener('click', handleSubmit);

        document.getElementById('asset-search').addEventListener('input', handleSearch);
        document.getElementById('filter-status').addEventListener('change', function() {
            window.location.href = '/admin/assets?status=' + this.value;
        });
        document.getElementById('reset-filters').addEventListener('click', resetFilters);

        document.querySelectorAll('.action-trigger').forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.stopPropagation();
                toggleActionMenu(this);
            });
        });

        document.addEventListener('click', function() {
            document.querySelectorAll('.action-dropdown').forEach(d => d.classList.remove('show'));
        });

        document.querySelectorAll('.action-edit').forEach(link => {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                openEditModal(this.dataset.assetId);
            });
        });

        document.querySelectorAll('.action-toggle-status').forEach(link => {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                toggleStatus(this.dataset.assetId, this.dataset.status);
            });
        });

        window.addEventListener('click', function(event) {
            if (event.target.classList.contains('mx-modal-overlay')) {
                closeModal();
            }
        });
    }

    async function loadCategories() {
        try {
            const response = await fetch('/admin/assets/categories');
            const categories = await response.json();
            
            const select = document.getElementById('asset-category');
            select.innerHTML = '<option value="">Uncategorized</option>';
            
            categories.forEach(cat => {
                const option = document.createElement('option');
                option.value = cat.value;
                option.textContent = cat.text;
                select.appendChild(option);
            });
        } catch (error) {
            console.error('Failed to load categories:', error);
        }
    }

    function openCreateModal() {
        isEditMode = false;
        currentAssetId = null;
        
        document.getElementById('modal-title').textContent = 'Add Asset';
        document.getElementById('submit-btn-text').textContent = 'Create Asset';
        document.getElementById('assetForm').reset();
        document.getElementById('asset-id').value = '';
        
        clearErrors();
        document.getElementById('assetModal').classList.add('open');
    }

    async function openEditModal(assetId) {
        isEditMode = true;
        currentAssetId = assetId;
        
        try {
            const response = await fetch(`/admin/assets/${assetId}/edit`);
            const data = await response.json();
            
            document.getElementById('modal-title').textContent = 'Edit Asset';
            document.getElementById('submit-btn-text').textContent = 'Save Changes';
            
            document.getElementById('asset-id').value = data.assetId;
            document.getElementById('asset-name').value = data.assetName;
            document.getElementById('asset-code').value = data.assetCode || '';
            document.getElementById('asset-category').value = data.categoryId || '';
            document.getElementById('asset-location').value = data.location || '';
            document.getElementById('asset-description').value = data.description || '';
            
            clearErrors();
            document.getElementById('assetModal').classList.add('open');
        } catch (error) {
            showToast('Failed to load asset details', false);
            console.error(error);
        }
    }

    function closeModal() {
        document.getElementById('assetModal').classList.remove('open');
        document.getElementById('assetForm').reset();
        clearErrors();
    }

    async function handleSubmit() {
        clearErrors();
        
        const name = document.getElementById('asset-name').value.trim();
        const code = document.getElementById('asset-code').value.trim();
        const categoryId = document.getElementById('asset-category').value;
        const location = document.getElementById('asset-location').value.trim();
        const description = document.getElementById('asset-description').value.trim();
        
        let hasError = false;
        
        if (!name) {
            showError('err-name');
            hasError = true;
        }
        
        if (hasError) return;
        
        const data = {
            assetName: name,
            assetCode: code || null,
            categoryId: categoryId ? parseInt(categoryId) : null,
            location: location || null,
            description: description || null
        };
        
        try {
            let response;
            
            if (isEditMode) {
                response = await fetch(`/admin/assets/${currentAssetId}/edit`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
            } else {
                response = await fetch('/admin/assets/create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
            }
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, true);
                closeModal();
                setTimeout(() => location.reload(), 1500);
            } else {
                if (result.message.includes('code')) {
                    showError('err-code');
                }
                showToast(result.message || 'Operation failed', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    }

    async function toggleStatus(assetId, currentStatus) {
        const action = currentStatus === 'Active' ? 'deactivate' : 'activate';
        
        if (!confirm(`Are you sure you want to ${action} this asset?`)) {
            return;
        }
        
        try {
            const response = await fetch(`/admin/assets/${assetId}/toggle-status`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' }
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, true);
                setTimeout(() => location.reload(), 1500);
            } else {
                showToast(result.message || 'Operation failed', false);
            }
        } catch (error) {
            showToast('An error occurred', false);
            console.error(error);
        }
    }

    function handleSearch() {
        const searchTerm = this.value.toLowerCase();
        const rows = document.querySelectorAll('#assets-tbody tr');
        let visibleCount = 0;
        
        rows.forEach(row => {
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

    function resetFilters() {
        document.getElementById('asset-search').value = '';
        document.getElementById('filter-status').value = 'all';
        
        const rows = document.querySelectorAll('#assets-tbody tr');
        rows.forEach(row => row.style.display = '');
        
        document.querySelector('#row-count strong').textContent = rows.length;
    }

    function toggleActionMenu(btn) {
        const dropdown = btn.nextElementSibling;
        const isOpen = dropdown.classList.contains('show');
        
        document.querySelectorAll('.action-dropdown').forEach(d => d.classList.remove('show'));
        
        if (!isOpen) {
            dropdown.classList.add('show');
        }
    }

    function showError(errorId) {
        document.getElementById(errorId).style.display = 'block';
    }

    function clearErrors() {
        document.querySelectorAll('.input-error').forEach(el => el.style.display = 'none');
    }

    function showToast(message, success) {
        const toast = document.getElementById('asset-toast');
        const messageEl = document.getElementById('toast-message');
        
        messageEl.textContent = message;
        toast.style.borderLeftColor = success ? '#22C55E' : '#EF4444';
        toast.querySelector('.toast-icon').textContent = success ? '✅' : '❌';
        
        toast.classList.add('show');
        
        setTimeout(() => toast.classList.remove('show'), 3000);
    }
})();
