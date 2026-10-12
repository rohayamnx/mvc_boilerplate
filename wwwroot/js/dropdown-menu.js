/**
 * Dropdown Menu Component JavaScript
 * Reusable dropdown menu functionality with nested submenus
 * Works with Alpine.js for reactive state management
 * 
 * Usage:
 * 1. Include this file in your page
 * 2. Use Alpine.js x-data directive with dropdownMenu() function
 * 3. Call methods via Alpine.js event handlers
 * 
 * Example:
 * <div x-data="dropdownMenu()">
 *   <button @click="toggleDropdown()">Actions</button>
 *   <div x-show="isOpen" class="dropdown-menu">
 *     <!-- Menu items -->
 *   </div>
 * </div>
 */

/**
 * Calculate the optimal position for a submenu (left or right)
 * @param {HTMLElement} buttonElement - The button element that triggers the submenu
 * @returns {string} - 'left' or 'right'
 */
function calculateSubmenuPosition(buttonElement) {
    const parentMenu = buttonElement.closest('.dropdown-menu');
    if (!parentMenu) return 'right';
    
    const parentRect = parentMenu.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const submenuWidth = 224; // w-56 = 14rem = 224px
    const spaceOnRight = viewportWidth - parentRect.right;
    
    return (spaceOnRight < submenuWidth) ? 'left' : 'right';
}

/**
 * Alpine.js component factory for dropdown menu
 * @returns {Object} Alpine.js component data and methods
 */
function dropdownMenu() {
    return {
        // State
        isOpen: false,
        activeSubmenu: null,
        submenuPositions: {},

        // Initialize
        init() {
            // Close dropdown when clicking outside
            document.addEventListener('click', (e) => {
                if (!this.$el.contains(e.target)) {
                    this.closeAll();
                }
            });
        },

        // Toggle main dropdown
        toggleDropdown() {
            this.isOpen = !this.isOpen;
            if (!this.isOpen) {
                this.activeSubmenu = null;
            }
        },

        // Open main dropdown
        openDropdown() {
            this.isOpen = true;
        },

        // Close main dropdown
        closeDropdown() {
            this.isOpen = false;
            this.activeSubmenu = null;
        },

        // Toggle submenu
        toggleSubmenu(submenuId, buttonElement) {
            // Close other submenus
            if (this.activeSubmenu === submenuId) {
                this.activeSubmenu = null;
            } else {
                this.activeSubmenu = submenuId;
                
                // Calculate position for desktop
                this.$nextTick(() => {
                    this.submenuPositions[submenuId] = calculateSubmenuPosition(buttonElement);
                });
            }
        },

        // Check if submenu is open
        isSubmenuOpen(submenuId) {
            return this.activeSubmenu === submenuId;
        },

        // Get submenu position class
        getSubmenuPosition(submenuId) {
            return this.submenuPositions[submenuId] || 'right';
        },

        // Close all menus
        closeAll() {
            this.isOpen = false;
            this.activeSubmenu = null;
        }
    };
}

/**
 * Utility function to create a dropdown menu configuration
 * @param {Object} options - Configuration options
 * @returns {Object} Dropdown menu configuration
 */
function createDropdownConfig(options = {}) {
    const defaults = {
        closeOnItemClick: true,
        closeOnOutsideClick: true,
        position: 'right',
        animation: true
    };

    return { ...defaults, ...options };
}

/**
 * Helper to handle dropdown item click
 * @param {Function} callback - Function to execute
 * @param {boolean} closeAfter - Whether to close dropdown after click
 */
function handleDropdownItemClick(callback, closeAfter = true) {
    return function(event) {
        if (callback) {
            callback(event);
        }
        if (closeAfter && this.closeAll) {
            this.closeAll();
        }
    };
}

// Export for use in modules (if using ES6 modules)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        calculateSubmenuPosition,
        dropdownMenu,
        createDropdownConfig,
        handleDropdownItemClick
    };
}
