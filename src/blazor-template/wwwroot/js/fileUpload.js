// File Upload JavaScript Helper Functions
window.fileUploadInit = (dotnetRef) => {
    console.log('File upload initialized');
};

window.triggerFileInput = (element) => {
    if (element) {
        element.click();
    }
};

// Enhanced drag and drop functionality
document.addEventListener('DOMContentLoaded', function() {
    // Prevent default drag behaviors on the entire document
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        document.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Add visual feedback for file drag operations
    ['dragenter', 'dragover'].forEach(eventName => {
        document.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        document.addEventListener(eventName, unhighlight, false);
    });

    function highlight(e) {
        const dropZone = document.querySelector('.drop-zone');
        if (dropZone && e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
            dropZone.classList.add('drag-active');
        }
    }

    function unhighlight(e) {
        const dropZone = document.querySelector('.drop-zone');
        if (dropZone) {
            dropZone.classList.remove('drag-active');
        }
    }

    // Handle file drops
    document.addEventListener('drop', handleDrop, false);

    function handleDrop(e) {
        const dropZone = document.querySelector('.drop-zone');
        if (dropZone && dropZone.contains(e.target)) {
            const dt = e.dataTransfer;
            const files = dt.files;
            
            if (files.length > 0) {
                const fileInput = document.querySelector('input[type="file"]');
                if (fileInput) {
                    // Create a new FileList-like object and assign it to the input
                    Object.defineProperty(fileInput, 'files', {
                        value: files,
                        configurable: true,
                    });
                    
                    // Trigger the change event
                    const event = new Event('change', { bubbles: true });
                    fileInput.dispatchEvent(event);
                }
            }
        }
    }
});

// Utility function to format file sizes
window.formatFileSize = (bytes) => {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    let order = 0;
    let size = bytes;
    
    while (size >= 1024 && order < sizes.length - 1) {
        order++;
        size = size / 1024;
    }
    
    return `${size.toFixed(2)} ${sizes[order]}`;
};

// Add tooltip functionality
window.initTooltips = () => {
    // Initialize Bootstrap tooltips if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
};