// Theme Management
const html = document.documentElement;
const themeToggle = document.getElementById('themeToggle');

themeToggle.addEventListener('click', () => {
    const isDark = html.getAttribute('data-theme') === 'dark';
    const newTheme = isDark ? 'light' : 'dark';
    
    html.setAttribute('data-theme', newTheme);
    themeToggle.textContent = `Dark Mode: ${newTheme === 'dark' ? 'On' : 'Off'}`;
});

// Element Visibility Helper
function toggleElement(id, show) {
    const el = document.getElementById(id);
    if (!el) return;
    
    if (show) {
        el.classList.remove('hidden');
    } else {
        el.classList.add('hidden');
    }
}

// Toast Notification Logic
let toastTimeout;
function showToast() {
    const toast = document.getElementById('toastNotification');
    if (!toast) return;

    // Reset if already showing
    if (toastTimeout) {
        clearTimeout(toastTimeout);
        toast.classList.add('hidden');
        // Force reflow for animation if we had one (though we kept it flat)
        void toast.offsetWidth;
    }

    toast.classList.remove('hidden');

    // Auto-hide after 4 seconds
    toastTimeout = setTimeout(() => {
        toast.classList.add('hidden');
        toastTimeout = null;
    }, 4000);
}

// Modal Logic
const modalOverlay = document.getElementById('errorModal');

function toggleModal(show) {
    if (!modalOverlay) return;
    if (show) {
        modalOverlay.classList.add('active');
        // Prevent body scroll when modal is open
        document.body.style.overflow = 'hidden';
    } else {
        modalOverlay.classList.remove('active');
        document.body.style.overflow = 'auto';
    }
}

// Close modal on outside click
modalOverlay.addEventListener('click', (e) => {
    if (e.target === modalOverlay) {
        toggleModal(false);
    }
});

// Global ESC key listener
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        toggleModal(false);
    }
});
