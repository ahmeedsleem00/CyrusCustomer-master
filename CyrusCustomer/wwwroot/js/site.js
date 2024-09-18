// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function () {
    const darkModeToggle = document.getElementById('darkModeToggle');
    const darkModeIcon = document.getElementById('darkModeIcon');
    const themeStylesheet = document.getElementById('theme-stylesheet');

    if (!themeStylesheet) {
        console.error('Theme stylesheet element not found');
        return;
    }

    // Check local storage for dark mode preference
    if (localStorage.getItem('darkMode') === 'true') {
        document.body.classList.add('dark-mode');
        darkModeIcon.classList.remove('fa-moon');
        darkModeIcon.classList.add('fa-sun');
        themeStylesheet.setAttribute('href', '/css/dark-theme.css');
    }

    darkModeToggle.addEventListener('click', function () {
        document.body.classList.toggle('dark-mode');
        if (document.body.classList.contains('dark-mode')) {
            localStorage.setItem('darkMode', 'true');
            darkModeIcon.classList.remove('fa-moon');
            darkModeIcon.classList.add('fa-sun');
            themeStylesheet.setAttribute('href', '/css/dark-theme.css');
        } else {
            localStorage.setItem('darkMode', 'false');
            darkModeIcon.classList.remove('fa-sun');
            darkModeIcon.classList.add('fa-moon');
            themeStylesheet.setAttribute('href', '/css/light-theme.css');
        }
    });
});
