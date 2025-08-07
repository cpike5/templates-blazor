window.blazorToast = {
    initialize: function () {
        // Initialize toast functionality
        console.log('Toast notification system initialized');
    },

    showToast: function (toastId) {
        const toast = document.getElementById(`toast-${toastId}`);
        if (toast) {
            toast.classList.add('show');
        }
    },

    hideToast: function (toastId) {
        const toast = document.getElementById(`toast-${toastId}`);
        if (toast) {
            toast.classList.remove('show');
            toast.classList.add('hide');
        }
    }
};