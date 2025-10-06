// Modern UI interactions for ABC Retailers

document.addEventListener('DOMContentLoaded', function () {
    // Add active class to current nav item
    highlightCurrentNavItem();
    
    // Auto-dismiss alerts after 5 seconds
    setupAutoDismissAlerts();
    
    // Add scroll behavior
    setupScrollBehavior();
    
    // Initialize tooltips and popovers if Bootstrap is available
    initializeBootstrapComponents();

    // Defensive: clear any stuck modal backdrops that could block clicks
    clearStuckModalBackdrop();
});

// Highlight the current navigation item based on URL
function highlightCurrentNavItem() {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href === currentPath || 
            (currentPath.includes(href) && href !== '/' && href !== '#')) {
            link.classList.add('active');
            link.setAttribute('aria-current', 'page');
        }
    });
}

// Auto-dismiss alerts after a delay
function setupAutoDismissAlerts() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
}

// Add scroll behavior for better UX
function setupScrollBehavior() {
    // Add shadow to navbar on scroll
    window.addEventListener('scroll', function() {
        const navbar = document.querySelector('.navbar');
        if (window.scrollY > 10) {
            navbar.classList.add('shadow-sm');
        } else {
            navbar.classList.remove('shadow-sm');
        }
    });
    
    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;
            
            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth'
                });
            }
        });
    });
}

// Initialize Bootstrap components if available
function initializeBootstrapComponents() {
    if (typeof bootstrap !== 'undefined') {
        // Initialize tooltips
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
        
        // Initialize popovers
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    }
}

// Add animation to cards when they enter viewport
document.addEventListener('DOMContentLoaded', function() {
    // Check if Intersection Observer is supported
    if ('IntersectionObserver' in window) {
        const cards = document.querySelectorAll('.card');
        
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('fade-in');
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.1
        });
        
        cards.forEach(card => {
            observer.observe(card);
        });
    }
});

// Fix: sometimes a stuck Bootstrap backdrop can block clicks after a modal
function clearStuckModalBackdrop() {
    setTimeout(() => {
        // Remove all backdrops
        document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
        // Close any open modals defensively
        document.querySelectorAll('.modal.show').forEach(m => {
            try { bootstrap.Modal.getOrCreateInstance(m).hide(); } catch (_) {}
        });
        // Reset body state
        const body = document.body;
        body.classList.remove('modal-open');
        body.style.removeProperty('overflow');
        body.style.removeProperty('padding-right');
    }, 0);
}