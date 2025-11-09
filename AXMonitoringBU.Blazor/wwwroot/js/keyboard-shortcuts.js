// Keyboard Shortcuts for AX Monitoring BU
window.setupKeyboardShortcuts = () => {
    document.addEventListener('keydown', (e) => {
        // Ctrl/Cmd + K for search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            const searchBox = document.querySelector('.search-box input');
            if (searchBox) {
                searchBox.focus();
            }
        }

        // Ctrl/Cmd + D for dark mode toggle
        if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
            e.preventDefault();
            const themeToggle = document.querySelector('.theme-toggle');
            if (themeToggle) {
                themeToggle.click();
            }
        }

        // Ctrl/Cmd + R for refresh (prevent default browser refresh)
        if ((e.ctrlKey || e.metaKey) && e.key === 'r' && !e.shiftKey) {
            // Allow normal refresh, but could be intercepted if needed
        }

        // Escape to close modals/dropdowns
        if (e.key === 'Escape') {
            const activeModal = document.querySelector('.modal.show');
            if (activeModal) {
                const closeButton = activeModal.querySelector('[data-bs-dismiss="modal"]');
                if (closeButton) {
                    closeButton.click();
                }
            }
        }

        // Number keys for quick navigation (1-9)
        if (e.key >= '1' && e.key <= '9' && !e.ctrlKey && !e.metaKey && !e.altKey) {
            const navLinks = document.querySelectorAll('.sidebar .nav-link');
            const index = parseInt(e.key) - 1;
            if (navLinks[index]) {
                navLinks[index].click();
            }
        }
    });
};

// Initialize on page load
window.addEventListener('DOMContentLoaded', () => {
    window.setupKeyboardShortcuts();
});

