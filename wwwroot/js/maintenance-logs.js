(function() {
    document.addEventListener('DOMContentLoaded', function() {
        setupEventListeners();
    });

    function setupEventListeners() {
        // View details buttons
        document.querySelectorAll('.btn-view-details').forEach(function(btn) {
            btn.addEventListener('click', function() {
                viewDetails(this.dataset.logId);
            });
        });

        // Close modal
        document.getElementById('closeDetailsModal').addEventListener('click', closeModal);
        document.getElementById('cancelDetailsModal').addEventListener('click', closeModal);

        // Search
        document.getElementById('log-search').addEventListener('input', handleSearch);

        // Reset
        document.getElementById('reset-filters').addEventListener('click', resetFilters);

        // Close modal on outside click
        window.addEventListener('click', function(event) {
            if (event.target.classList.contains('mx-modal-overlay')) {
                closeModal();
            }
        });
    }

    async function viewDetails(logId) {
        try {
            // Detect route based on current URL
            const baseRoute = window.location.pathname.startsWith('/admin') ? '/admin/maintenance-logs' : '/maintenance-logs';
            const response = await fetch(`${baseRoute}/${logId}`);
            const data = await response.json();

            document.getElementById('detail-log-id').textContent = '#' + data.logId;
            document.getElementById('detail-wo-id').textContent = 'WO-' + data.workOrderId;
            document.getElementById('detail-asset').textContent = data.assetName || 'N/A';
            document.getElementById('detail-title').textContent = data.title;
            document.getElementById('detail-description').textContent = data.description || 'No description';
            document.getElementById('detail-completed-by').textContent = data.completedBy || 'Unassigned';
            document.getElementById('detail-completed-date').textContent = new Date(data.completedDate).toLocaleString();
            document.getElementById('detail-notes').textContent = data.notes || 'No notes';

            document.getElementById('detailsModal').classList.add('open');
        } catch (error) {
            console.error('Failed to load log details:', error);
        }
    }

    function closeModal() {
        document.getElementById('detailsModal').classList.remove('open');
    }

    function handleSearch() {
        const searchTerm = this.value.toLowerCase();
        const rows = document.querySelectorAll('#logs-tbody tr');
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

    function resetFilters() {
        document.getElementById('log-search').value = '';
        const rows = document.querySelectorAll('#logs-tbody tr');
        rows.forEach(function(row) {
            row.style.display = '';
        });
        document.querySelector('#row-count strong').textContent = rows.length;
    }
})();
