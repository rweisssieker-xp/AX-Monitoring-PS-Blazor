// Theme management JavaScript
window.applyTheme = (theme) => {
    const html = document.documentElement;
    if (theme === 'dark') {
        html.setAttribute('data-theme', 'dark');
        html.classList.add('dark-mode');
    } else {
        html.removeAttribute('data-theme');
        html.classList.remove('dark-mode');
    }
};

// Initialize theme on page load
window.addEventListener('DOMContentLoaded', () => {
    const savedTheme = localStorage.getItem('theme') || 'light';
    window.applyTheme(savedTheme);
});

