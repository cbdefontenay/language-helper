window.toggleNavMenu = (isOpen) => {
    const menu = document.getElementById('nav-menu');
    const iconPath = document.getElementById('menu-icon-path');

    if (isOpen) {
        menu.classList.remove('-translate-x-full');
        iconPath.setAttribute('d', 'M6 18L18 6M6 6l12 12');
    } else {
        menu.classList.add('-translate-x-full');
        iconPath.setAttribute('d', 'M4 6h16M4 12h16M4 18h16');
    }
};

document.addEventListener('click', function (event) {
    const menu = document.getElementById('nav-menu');
    const button = document.querySelector('button[aria-label="Toggle menu"]');

    if (!menu.contains(event.target) && !button.contains(event.target)) {
        menu.classList.add('-translate-x-full');
        const iconPath = document.getElementById('menu-icon-path');
        iconPath.setAttribute('d', 'M4 6h16M4 12h16M4 18h16');
    }
});
