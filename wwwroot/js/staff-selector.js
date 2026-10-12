/**
 * Reusable Staff Selector JavaScript (mvc_boilerplate sample)
 *
 * Adapted from iform_hra's staff-selector.js. Reads staff data from a
 * server-rendered inline JSON block (id="staffSelectorInlineData") — no
 * client-side /api/staffapi call is made.
 *
 * Usage on a page:
 *   1. Controller sets ViewBag.StaffSelectorJson (server-side fetch from StaffHub).
 *   2. View includes <partial name="_StaffSelectorInlineData" /> in the Scripts section.
 *   3. View invokes the StaffSelector ViewComponent for each search field.
 *   4. On DOMContentLoaded, initialize each .staff-search-input:
 *        window.staffSelectorManager.initializeInstance(input);
 *        window.staffSelectorManager.setupGlobalClickHandler();
 */
class StaffSelectorManager {
    constructor() {
        this.staffData = [];
        this.instances = new Map();
        this.isLoading = false;
        this.hasLoadedOnce = false;
    }

    /**
     * Initialize a specific staff selector instance
     */
    initializeInstance(searchInput) {
        if (!searchInput) return;

        const instanceId = searchInput.getAttribute('data-row') ||
            searchInput.getAttribute('data-instance-id') ||
            searchInput.closest('[data-row]')?.getAttribute('data-row') ||
            'default';

        const container = searchInput.closest('.staff-row') ||
            searchInput.closest('.participant-row') ||
            searchInput.closest('[class*="row"]') ||
            searchInput.parentElement;

        const instance = {
            searchInput,
            instanceId,
            hiddenStaffId: this.findHiddenInput(container, 'selected-staff-id', 'StaffId'),
            hiddenStaffName: this.findHiddenInput(container, 'selected-staff-name', 'StaffName'),
            dropdownResults: this.findDropdownElement(searchInput),
            loadingIndicator: this.findLoadingElement(searchInput),
            filteredStaff: [],
            selectedIndex: -1
        };

        if (!instance.dropdownResults) return;

        // Set initial value if hidden input has a value
        if (instance.hiddenStaffId && instance.hiddenStaffId.value) {
            const staff = this.staffData.find(s => s.staffId === instance.hiddenStaffId.value);
            if (staff) {
                searchInput.value = staff.name;
            }
        }

        this.setupEventListeners(instance);
        this.instances.set(instanceId, instance);
    }

    findHiddenInput(container, className, fieldSuffix) {
        let input = container.querySelector(`.${className}`);
        if (input) return input;

        input = container.querySelector(`input[name*="${fieldSuffix}"]`);
        if (input) return input;

        const parentContainer = container.parentElement;
        if (parentContainer) {
            input = parentContainer.querySelector(`.${className}`) ||
                parentContainer.querySelector(`input[name*="${fieldSuffix}"]`);
        }
        return input;
    }

    findDropdownElement(searchInput) {
        let dropdown = searchInput.nextElementSibling;
        if (dropdown && dropdown.classList.contains('staff-dropdown-results')) {
            return dropdown;
        }

        const relativeContainer = searchInput.closest('.relative');
        if (relativeContainer) {
            dropdown = relativeContainer.querySelector('.staff-dropdown-results');
            if (dropdown) return dropdown;
        }

        const instanceId = searchInput.getAttribute('data-instance-id') ||
            searchInput.getAttribute('data-row');
        if (instanceId) {
            dropdown = document.getElementById(`staffDropdownResults_${instanceId}`);
            if (dropdown) return dropdown;
        }

        // Create dropdown if none found
        const container = searchInput.closest('.relative') || searchInput.parentElement;
        dropdown = document.createElement('div');
        dropdown.className = 'staff-dropdown-results absolute z-50 w-full bg-white text-sm dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md shadow-lg max-h-60 overflow-y-auto hidden';
        container.appendChild(dropdown);
        return dropdown;
    }

    findLoadingElement(searchInput) {
        const relativeContainer = searchInput.closest('.relative');
        if (relativeContainer) {
            return relativeContainer.querySelector('.staff-loading-indicator');
        }
        const instanceId = searchInput.getAttribute('data-instance-id') ||
            searchInput.getAttribute('data-row');
        if (instanceId) {
            return document.getElementById(`loadingStaff_${instanceId}`);
        }
        return null;
    }

    /**
     * Remove a staff selector instance
     */
    removeInstance(instanceId) {
        if (this.instances.has(instanceId)) {
            this.instances.delete(instanceId);
        }
    }

    /**
     * Clear selection for a specific instance
     */
    clearSelection(instanceId) {
        const instance = this.instances.get(instanceId);
        if (instance) {
            instance.searchInput.value = '';
            if (instance.hiddenStaffId) instance.hiddenStaffId.value = '';
            if (instance.hiddenStaffName) instance.hiddenStaffName.value = '';
            this.updateRelatedFields(instance, {});
        }
    }

    setupEventListeners(instance) {
        const { searchInput } = instance;

        searchInput.addEventListener('input', async () => {
            await this.handleInput(instance);
        });
        searchInput.addEventListener('focus', async () => {
            await this.handleFocus(instance);
        });
        searchInput.addEventListener('keydown', (event) => {
            this.handleKeyNavigation(instance, event);
        });
        searchInput.addEventListener('blur', () => {
            setTimeout(() => this.hideDropdown(instance), 150);
        });
    }

    /**
     * Read server-rendered inline staff data.
     */
    loadInlineStaffData() {
        const el = document.getElementById('staffSelectorInlineData');
        if (!el) return null;
        try {
            const data = JSON.parse(el.textContent || '[]');
            if (!Array.isArray(data)) return [];
            return data.filter(s => s && s.isActive !== false)
                       .sort((a, b) => (a.name || '').localeCompare(b.name || ''));
        } catch (e) {
            console.error('Failed to parse inline staff data', e);
            return [];
        }
    }

    /**
     * Load staff data from the inline JSON block (no API call).
     */
    async loadStaffData() {
        if (this.staffData.length > 0) return;

        this.isLoading = true;
        this.hasLoadedOnce = true;
        this.showLoadingIndicators(true);

        try {
            const inline = this.loadInlineStaffData();
            if (inline && inline.length) {
                this.staffData = inline;
                document.querySelectorAll('[id$="staffApiError"]').forEach(el => el.style.display = 'none');
                return;
            }
            // No inline data — show error
            this.showApiError();
        } catch {
            this.showApiError();
        } finally {
            this.isLoading = false;
            this.showLoadingIndicators(false);
        }
    }

    async handleInput(instance) {
        if (this.staffData.length === 0 && !this.isLoading) {
            await this.loadStaffData();
        }
        const searchTerm = instance.searchInput.value.trim();
        instance.filteredStaff = this.filterStaff(searchTerm);
        instance.selectedIndex = -1;
        this.displayFilteredResults(instance);
    }

    async handleFocus(instance) {
        if (this.staffData.length === 0) {
            await this.ensureStaffDataLoaded();
        }
        const searchTerm = instance.searchInput.value.trim();
        instance.filteredStaff = this.filterStaff(searchTerm);
        this.displayFilteredResults(instance);
        this.showDropdown(instance);
    }

    async ensureStaffDataLoaded() {
        let retries = 0;
        const maxRetries = 3;
        while (this.staffData.length === 0 && retries < maxRetries) {
            if (!this.isLoading) {
                await this.loadStaffData();
            } else {
                while (this.isLoading) {
                    await new Promise(resolve => setTimeout(resolve, 100));
                }
            }
            if (this.staffData.length === 0) {
                retries++;
                if (retries < maxRetries) {
                    await new Promise(resolve => setTimeout(resolve, 500));
                }
            }
        }
    }

    filterStaff(searchTerm) {
        if (!searchTerm) return this.staffData.slice(0, 20);

        return this.staffData.filter(staff => {
            if (!staff) return false;
            const name = (staff.name || staff.Name || '').toLowerCase();
            const staffId = (staff.id || staff.Id || '').toLowerCase();
            const searchTermLower = searchTerm.toLowerCase();
            return name.includes(searchTermLower) || staffId.includes(searchTermLower);
        }).slice(0, 20);
    }

    displayFilteredResults(instance) {
        if (!instance.dropdownResults) return;

        const results = instance.filteredStaff;
        instance.dropdownResults.innerHTML = '';

        if (results.length === 0) {
            instance.dropdownResults.innerHTML = '<div class="p-3 text-gray-500 dark:text-gray-400 italic">No staff found</div>';
        } else {
            results.forEach((staff, index) => {
                const item = document.createElement('div');
                item.className = 'staff-result-item p-3 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 border-b border-gray-100 dark:border-gray-700 last:border-b-0';

                const staffName = staff.name || staff.Name || 'Unknown';
                const staffId = staff.id || staff.Id || 'N/A';
                const departmentName = staff.departmentName || staff.DepartmentName || 'No Department';

                item.innerHTML = `
                    <div class="font-medium text-gray-900 dark:text-white">${this.escapeHtml(staffName)}</div>
                    <div class="text-xs text-gray-500 dark:text-gray-400">ID: ${this.escapeHtml(staffId)} | ${this.escapeHtml(departmentName)}</div>
                `;

                item.addEventListener('click', () => {
                    this.selectStaffFromResult(instance, staff);
                });

                instance.dropdownResults.appendChild(item);
            });
        }

        this.showDropdown(instance);
    }

    handleKeyNavigation(instance, event) {
        const visibleResults = instance.dropdownResults?.querySelectorAll('.staff-result-item') || [];

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                instance.selectedIndex = Math.min(instance.selectedIndex + 1, visibleResults.length - 1);
                this.updateHighlight(instance, visibleResults);
                break;
            case 'ArrowUp':
                event.preventDefault();
                instance.selectedIndex = Math.max(instance.selectedIndex - 1, -1);
                this.updateHighlight(instance, visibleResults);
                break;
            case 'Enter':
                event.preventDefault();
                if (instance.selectedIndex >= 0 && visibleResults[instance.selectedIndex]) {
                    const staff = instance.filteredStaff[instance.selectedIndex];
                    this.selectStaffFromResult(instance, staff);
                }
                break;
            case 'Escape':
                this.hideDropdown(instance);
                instance.searchInput.blur();
                break;
        }
    }

    updateHighlight(instance, visibleResults) {
        visibleResults.forEach((item, index) => {
            if (index === instance.selectedIndex) {
                item.classList.add('bg-blue-100', 'dark:bg-blue-900/40');
            } else {
                item.classList.remove('bg-blue-100', 'dark:bg-blue-900/40');
            }
        });
    }

    selectStaffFromResult(instance, staff) {
        const staffName = staff.name || staff.Name || '';
        const staffId = staff.id || staff.Id || '';

        instance.searchInput.value = staffName;
        if (instance.hiddenStaffId) instance.hiddenStaffId.value = staffId;
        if (instance.hiddenStaffName) instance.hiddenStaffName.value = staffName;

        this.updateRelatedFields(instance, staff);
        this.hideDropdown(instance);

        if (typeof showSuccess === 'function') {
            showSuccess(`Selected: ${staffName} (${staffId})`);
        }
    }

    updateRelatedFields(instance, staff) {
        const departmentId = staff.departmentId || staff.DepartmentId || '';
        const departmentName = staff.departmentName || staff.DepartmentName || '';

        let gridContainer = instance.searchInput.closest('.grid');
        if (!gridContainer) {
            let currentElement = instance.searchInput;
            while (currentElement && currentElement.parentElement) {
                currentElement = currentElement.parentElement;
                if (currentElement.classList && currentElement.classList.contains('grid')) {
                    gridContainer = currentElement;
                    break;
                }
            }
        }
        const searchContainer = gridContainer || document;

        // Department
        const departmentIdInput = searchContainer.querySelector(`input[name*="DepartmentId"]`);
        const departmentNameInput = searchContainer.querySelector(`input[name*="DepartmentName"]:not([type="hidden"])`);
        if (departmentIdInput) departmentIdInput.value = departmentId;
        if (departmentNameInput) {
            if (departmentNameInput.hasAttribute('readonly')) {
                departmentNameInput.removeAttribute('readonly');
            }
            departmentNameInput.value = departmentName;
            if (departmentName) {
                setTimeout(() => departmentNameInput.setAttribute('readonly', 'readonly'), 50);
            }
        }

        // Staff ID display
        const staffIdDisplayInput = searchContainer.querySelector(`input[id*="StaffId_display"]`) ||
            document.querySelector(`input[id*="StaffId_display"]`);
        if (staffIdDisplayInput) {
            if (staffIdDisplayInput.hasAttribute('readonly')) {
                staffIdDisplayInput.removeAttribute('readonly');
            }
            staffIdDisplayInput.value = staff.id || staff.Id || '';
            if (staffIdDisplayInput.value) {
                setTimeout(() => staffIdDisplayInput.setAttribute('readonly', 'readonly'), 50);
            }
        }

    }

    showDropdown(instance) {
        if (instance.dropdownResults) {
            instance.dropdownResults.classList.remove('hidden');
        }
    }

    hideDropdown(instance) {
        if (instance.dropdownResults) {
            instance.dropdownResults.classList.add('hidden');
        }
    }

    showLoadingIndicators(show) {
        if (!show && !this.hasLoadedOnce) return;
        const loadingElements = document.querySelectorAll('[id^="loadingStaff_"]');
        loadingElements.forEach(el => {
            if (show) {
                el.classList.remove('hidden');
                el.classList.add('flex');
            } else {
                el.classList.add('hidden');
                el.classList.remove('flex');
            }
        });
    }

    showApiError() {
        this.instances.forEach(instance => {
            if (instance.searchInput) {
                instance.searchInput.placeholder = 'Service unavailable';
                instance.searchInput.disabled = true;
                instance.searchInput.style.opacity = '0.6';
            }
        });

        document.querySelectorAll('[id$="staffApiError"]').forEach(element => {
            if (element) element.style.display = 'block';
        });

        if (typeof showError === 'function') {
            showError('StaffHub service is unavailable. Please try again later.');
        }
    }

    setupGlobalClickHandler() {
        document.addEventListener('click', (event) => {
            if (!event.target.closest('.relative')) {
                this.instances.forEach(instance => {
                    this.hideDropdown(instance);
                });
            }
        });
    }

    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text ? text.replace(/[&<>"']/g, m => map[m]) : '';
    }
}

// Make globally available
window.StaffSelectorManager = StaffSelectorManager;
window.staffSelectorManager = new StaffSelectorManager();