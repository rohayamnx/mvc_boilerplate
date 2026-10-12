// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Global Tooltip Handler (replaces CSS-only implementation to fix overflow clipping)
(function () {
    let tooltipTimeout;

    function createTooltip(target) {
        const tooltipText = target.getAttribute('data-tooltip');
        if (!tooltipText) return;

        removeGlobalTooltip();

        const tooltip = document.createElement('div');
        tooltip.className = 'global-tooltip';
        tooltip.textContent = tooltipText;
        document.body.appendChild(tooltip);

        const rect = target.getBoundingClientRect();

        // Initial Position: Centered above the target
        // CSS transform: translate(-50%, -100%) handles the offset
        tooltip.style.left = (rect.left + rect.width / 2) + 'px';
        tooltip.style.top = rect.top + 'px';

        // Collision Check: If tooltip goes off-screen top, flip to bottom
        const tooltipRect = tooltip.getBoundingClientRect();
        if (tooltipRect.top < 0) {
            tooltip.classList.add('tooltip-bottom');
            tooltip.style.top = rect.bottom + 'px';
        }

        // Horizontal Collision Check (optional cleanup): 
        // If half of tooltip would be offscreen left/right, we could adjust 
        // but centering is usually safe enough for this design.

        // Show with animation
        requestAnimationFrame(() => {
            tooltip.classList.add('show');
        });
    }

    function removeGlobalTooltip() {
        const existing = document.querySelectorAll('.global-tooltip');
        existing.forEach(el => el.remove());
    }

    document.addEventListener('mouseover', function (e) {
        const target = e.target.closest('[data-tooltip]');
        if (target) {
            // Small delay to prevent flickering
            createTooltip(target);
        }
    });

    document.addEventListener('mouseout', function (e) {
        const target = e.target.closest('[data-tooltip]');
        if (target) {
            removeGlobalTooltip();
        }
    });

    // Hide tooltip on scroll to prevent detachment from target
    window.addEventListener('scroll', function () {
        removeGlobalTooltip();
    }, { capture: true, passive: true });

})();
