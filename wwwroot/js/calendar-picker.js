// ========================================
// Reusable Calendar Date Picker Component
// ========================================
// Usage:
// 1. Include this script in your page
// 2. Add the calendar picker HTML (see CalendarPickerPartial.cshtml)
// 3. Call initCalendarPicker(inputId, options) for each date input

class CalendarPicker {
    constructor(inputId, options = {}) {
        this.inputId = inputId;
        this.input = document.getElementById(inputId);
        this.pickerId = options.pickerId || `${inputId}Picker`;
        this.onDateSelect = options.onDateSelect || null;
        this.pickerDate = new Date();
        this.pickerView = 'days'; // 'days', 'months', 'years'

        // Range mode (opt-in): two clicks inside ONE calendar select a
        // start + end. Single-date mode (the default) is unchanged.
        this.range = !!options.range;
        this.rangeStart = null;   // Date (00:00:00)
        this.rangeEnd = null;     // Date (00:00:00) or null while selecting

        if (!this.input) {
            console.error(`Calendar Picker: Input element with id "${inputId}" not found`);
            return;
        }

        this.init();
    }

    init() {
        // Create picker container if it doesn't exist
        if (!document.getElementById(this.pickerId)) {
            this.createPickerHTML();
        }

        this.picker = document.getElementById(this.pickerId);
        this.grid = this.picker.querySelector('.calendar-picker-grid');
        this.dayHeaders = this.picker.querySelector('.calendar-picker-day-headers');
        this.pickerMonth = this.picker.querySelector('.picker-month');
        this.pickerYear = this.picker.querySelector('.picker-year');
        this.monthSelector = this.picker.querySelector('.month-selector');
        this.yearSelector = this.picker.querySelector('.year-selector');

        // Set up event listeners
        this.setupEventListeners();
    }

    createPickerHTML() {
        const pickerHTML = `
            <div id="${this.pickerId}" class="hidden absolute top-full left-0 mt-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-xl z-50 p-4 min-w-[320px]">
                <!-- Header with Month/Year Selection -->
                <div class="flex items-center justify-between mb-4">
                    <button type="button" class="picker-prev-btn h-8 w-8 flex items-center justify-center rounded hover:bg-gray-100 dark:hover:bg-gray-700">
                        <svg class="fill-current" width="16" height="16" viewBox="0 0 24 24"><path d="M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z"/></svg>
                    </button>
                    
                    <div class="flex items-center gap-2">
                        <!-- Month Selector -->
                        <div class="relative">
                            <button type="button" class="month-toggle flex items-center gap-1 px-2 py-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
                                <span class="picker-month text-sm font-bold text-gray-900 dark:text-white"></span>
                                <svg class="fill-current text-gray-600 dark:text-gray-400" width="10" height="10" viewBox="0 0 24 24">
                                    <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z"/>
                                </svg>
                            </button>
                            
                            <!-- Month Dropdown -->
                            <div class="month-selector hidden absolute top-full left-0 mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg p-2 min-w-[140px] z-10 max-h-64 overflow-y-auto">
                                <!-- Will be populated by JavaScript -->
                            </div>
                        </div>
                        
                        <!-- Year Selector -->
                        <div class="relative">
                            <button type="button" class="year-toggle flex items-center gap-1 px-2 py-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
                                <span class="picker-year text-sm font-bold text-gray-900 dark:text-white"></span>
                                <svg class="fill-current text-gray-600 dark:text-gray-400" width="10" height="10" viewBox="0 0 24 24">
                                    <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z"/>
                                </svg>
                            </button>
                            
                            <!-- Year Dropdown -->
                            <div class="year-selector hidden absolute top-full left-0 mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg p-2 min-w-[100px] z-10 max-h-64 overflow-y-auto">
                                <!-- Will be populated by JavaScript -->
                            </div>
                        </div>
                    </div>
                    
                    <button type="button" class="picker-next-btn h-8 w-8 flex items-center justify-center rounded hover:bg-gray-100 dark:hover:bg-gray-700">
                        <svg class="fill-current" width="16" height="16" viewBox="0 0 24 24"><path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/></svg>
                    </button>
                </div>
                
                <!-- Day View Headers -->
                <div class="calendar-picker-day-headers grid grid-cols-7 gap-1 mb-2">
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Sun</div>
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Mon</div>
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Tue</div>
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Wed</div>
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Thu</div>
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Fri</div>
                    <div class="text-center text-xs font-medium text-gray-400 dark:text-gray-500 py-1">Sat</div>
                </div>
                
                <!-- Content Grid (Days/Months/Years) -->
                <div class="calendar-picker-grid grid grid-cols-7 gap-1">
                    <!-- Calendar dates will be populated by JavaScript -->
                </div>
            </div>
        `;

        // Insert picker after input's parent
        const wrapper = this.input.closest('.relative') || this.input.parentElement;
        wrapper.insertAdjacentHTML('beforeend', pickerHTML);
    }

    setupEventListeners() {
        // Previous/Next buttons
        this.picker.querySelector('.picker-prev-btn').addEventListener('click', () => this.changePickerPeriod(-1));
        this.picker.querySelector('.picker-next-btn').addEventListener('click', () => this.changePickerPeriod(1));

        // Month/Year toggle buttons
        this.picker.querySelector('.month-toggle').addEventListener('click', () => this.toggleMonthSelector());
        this.picker.querySelector('.year-toggle').addEventListener('click', () => this.toggleYearSelector());
    }

    toggle() {
        if (this.picker.classList.contains('hidden')) {
            this.open();
        } else {
            this.close();
        }
    }

    open() {
        // Initialize picker with current input value or today.
        // In range mode the input holds a display string ("a → b"), so base
        // the calendar view on the stored rangeStart instead.
        if (this.range) {
            this.pickerDate = this.rangeStart ? new Date(this.rangeStart) : new Date();
        } else {
            const currentValue = this.input.value;
            this.pickerDate = currentValue ? new Date(currentValue) : new Date();
        }
        this.pickerView = 'days';

        this.render();
        this.picker.classList.remove('hidden');

        // Close picker when clicking outside
        setTimeout(() => {
            document.addEventListener('click', this.closeOnOutsideClick.bind(this));
        }, 0);
    }

    close() {
        this.picker.classList.add('hidden');
        this.monthSelector.classList.add('hidden');
        this.yearSelector.classList.add('hidden');
        document.removeEventListener('click', this.closeOnOutsideClick.bind(this));
    }

    closeOnOutsideClick(event) {
        const target = event.target;
        // If the target was removed from the DOM during the click (e.g. a date
        // cell that selectRangeDate() replaced via render()), the contains()
        // checks below would falsely report "outside" and close the picker
        // mid-range-selection. Treat a detached target as inside and ignore.
        if (!document.contains(target)) return;
        // Check if click is outside the picker and not on the input
        if (!this.picker.contains(target) && target !== this.input && !this.input.contains(target)) {
            this.close();
        }
    }

    toggleMonthSelector() {
        this.yearSelector.classList.add('hidden');

        if (this.monthSelector.classList.contains('hidden')) {
            this.renderMonthSelector();
            this.monthSelector.classList.remove('hidden');
        } else {
            this.monthSelector.classList.add('hidden');
        }
    }

    toggleYearSelector() {
        this.monthSelector.classList.add('hidden');

        if (this.yearSelector.classList.contains('hidden')) {
            this.renderYearSelector();
            this.yearSelector.classList.remove('hidden');
        } else {
            this.yearSelector.classList.add('hidden');
        }
    }

    renderMonthSelector() {
        const currentMonth = this.pickerDate.getMonth();
        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
            'July', 'August', 'September', 'October', 'November', 'December'];

        let html = '<div class="space-y-1">';

        monthNames.forEach((month, index) => {
            const isCurrentMonth = index === currentMonth;
            const classes = isCurrentMonth
                ? 'px-3 py-2 rounded hover:bg-gray-200 dark:hover:bg-gray-600 cursor-pointer text-sm bg-gray-200 dark:bg-gray-600 font-semibold text-gray-900 dark:text-white'
                : 'px-3 py-2 rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer text-sm text-gray-700 dark:text-gray-300';

            html += `<div class="${classes}" data-month="${index}">${month}</div>`;
        });

        html += '</div>';
        this.monthSelector.innerHTML = html;

        // Add click listeners to month items
        this.monthSelector.querySelectorAll('[data-month]').forEach(item => {
            item.addEventListener('click', () => {
                const monthIndex = parseInt(item.dataset.month);
                this.selectMonth(monthIndex);
            });
        });
    }

    renderYearSelector() {
        const currentYear = this.pickerDate.getFullYear();
        const startYear = currentYear - 5;
        const endYear = currentYear + 5;

        let html = '<div class="space-y-1">';

        for (let year = endYear; year >= startYear; year--) {
            const isCurrentYear = year === currentYear;
            const classes = isCurrentYear
                ? 'px-3 py-2 rounded hover:bg-gray-200 dark:hover:bg-gray-600 cursor-pointer text-sm bg-gray-200 dark:bg-gray-600 font-semibold text-gray-900 dark:text-white'
                : 'px-3 py-2 rounded hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer text-sm text-gray-700 dark:text-gray-300';

            html += `<div class="${classes}" data-year="${year}">${year}</div>`;
        }

        html += '</div>';
        this.yearSelector.innerHTML = html;

        // Add click listeners to year items
        this.yearSelector.querySelectorAll('[data-year]').forEach(item => {
            item.addEventListener('click', () => {
                const year = parseInt(item.dataset.year);
                this.selectYear(year);
            });
        });
    }

    selectMonth(monthIndex) {
        this.pickerDate.setMonth(monthIndex);
        this.monthSelector.classList.add('hidden');
        this.pickerView = 'days';
        this.render();
    }

    selectYear(year) {
        this.pickerDate.setFullYear(year);
        this.yearSelector.classList.add('hidden');
        this.pickerView = 'days';
        this.render();
    }

    changePickerPeriod(delta) {
        if (this.pickerView === 'days') {
            this.pickerDate.setMonth(this.pickerDate.getMonth() + delta);
        } else if (this.pickerView === 'months') {
            this.pickerDate.setFullYear(this.pickerDate.getFullYear() + delta);
        } else if (this.pickerView === 'years') {
            this.pickerDate.setFullYear(this.pickerDate.getFullYear() + (delta * 12));
        }
        this.render();
    }

    render() {
        if (this.pickerView === 'days') {
            this.dayHeaders.classList.remove('hidden');
            this.renderDaysPicker();
        } else if (this.pickerView === 'months') {
            this.dayHeaders.classList.add('hidden');
            this.renderMonthsPicker();
        } else if (this.pickerView === 'years') {
            this.dayHeaders.classList.add('hidden');
            this.renderYearsPicker();
        }
    }

    renderDaysPicker() {
        const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
            'July', 'August', 'September', 'October', 'November', 'December'];
        this.pickerMonth.textContent = monthNames[this.pickerDate.getMonth()];
        this.pickerYear.textContent = this.pickerDate.getFullYear();

        // Get first day of month and number of days
        const firstDay = new Date(this.pickerDate.getFullYear(), this.pickerDate.getMonth(), 1);
        const lastDay = new Date(this.pickerDate.getFullYear(), this.pickerDate.getMonth() + 1, 0);
        const daysInMonth = lastDay.getDate();
        const startDayOfWeek = firstDay.getDay();

        // Get current date for highlighting
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Selected date(s) for highlighting.
        // Single mode: parse the input value. Range mode: use stored range.
        let selectedDate = null;
        let rangeStart = null;
        let rangeEnd = null;
        if (this.range) {
            rangeStart = this.rangeStart;
            rangeEnd = this.rangeEnd;
        } else {
            selectedDate = this.input.value ? new Date(this.input.value) : null;
            if (selectedDate) selectedDate.setHours(0, 0, 0, 0);
        }

        let html = '';

        // Add empty cells for days before month starts
        for (let i = 0; i < startDayOfWeek; i++) {
            html += '<div class="h-8"></div>';
        }

        // Add day cells
        for (let day = 1; day <= daysInMonth; day++) {
            const cellDate = new Date(this.pickerDate.getFullYear(), this.pickerDate.getMonth(), day);
            cellDate.setHours(0, 0, 0, 0);

            const isToday = cellDate.getTime() === today.getTime();
            const isSelected = selectedDate && cellDate.getTime() === selectedDate.getTime();

            const isRangeStart = rangeStart && cellDate.getTime() === rangeStart.getTime();
            const isRangeEnd = rangeEnd && cellDate.getTime() === rangeEnd.getTime();
            const isInRange = rangeStart && rangeEnd
                && cellDate > rangeStart && cellDate < rangeEnd;

            let classes = 'h-8 flex items-center justify-center rounded text-sm cursor-pointer transition-colors ';

            if (isRangeStart || isRangeEnd) {
                classes += 'bg-blue-600 text-white font-bold';
            } else if (isInRange) {
                classes += 'bg-blue-100 text-blue-600 dark:bg-blue-900/40 dark:text-blue-300';
            } else if (isSelected) {
                classes += 'bg-blue-600 text-white font-bold';
            } else if (isToday) {
                classes += 'bg-blue-100 text-blue-600 dark:bg-blue-900 dark:text-blue-300 font-medium';
            } else {
                classes += 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700';
            }

            const dateStr = this.formatDateForInput(cellDate);
            html += `<div class="${classes}" data-date="${dateStr}">${day}</div>`;
        }

        this.grid.className = 'calendar-picker-grid grid grid-cols-7 gap-1';
        this.grid.innerHTML = html;

        // Add click listeners to date cells
        this.grid.querySelectorAll('[data-date]').forEach(cell => {
            cell.addEventListener('click', () => {
                const dateStr = cell.dataset.date;
                this.selectDate(dateStr);
            });
        });
    }

    renderMonthsPicker() {
        this.pickerMonth.textContent = 'Select';
        this.pickerYear.textContent = this.pickerDate.getFullYear();

        const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
            'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

        const currentMonth = new Date().getMonth();
        const currentYear = new Date().getFullYear();
        const selectedMonth = this.pickerDate.getMonth();
        const selectedYear = this.pickerDate.getFullYear();

        let html = '';

        monthNames.forEach((month, index) => {
            const isCurrent = index === currentMonth && selectedYear === currentYear;
            const isSelected = index === selectedMonth;

            let classes = 'h-12 flex items-center justify-center rounded text-sm cursor-pointer transition-colors font-medium ';

            if (isSelected) {
                classes += 'bg-blue-600 text-white font-bold';
            } else if (isCurrent) {
                classes += 'bg-blue-100 text-blue-600 dark:bg-blue-900 dark:text-blue-300';
            } else {
                classes += 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700';
            }

            html += `<div class="${classes}" data-month="${index}">${month}</div>`;
        });

        this.grid.className = 'calendar-picker-grid grid grid-cols-3 gap-2';
        this.grid.innerHTML = html;

        // Add click listeners
        this.grid.querySelectorAll('[data-month]').forEach(cell => {
            cell.addEventListener('click', () => {
                const monthIndex = parseInt(cell.dataset.month);
                this.selectMonth(monthIndex);
            });
        });
    }

    renderYearsPicker() {
        const startYear = Math.floor(this.pickerDate.getFullYear() / 12) * 12;
        const endYear = startYear + 11;

        this.pickerMonth.textContent = 'Select';
        this.pickerYear.textContent = `${startYear} - ${endYear}`;

        const currentYear = new Date().getFullYear();
        const selectedYear = this.pickerDate.getFullYear();

        let html = '';

        for (let year = startYear; year <= endYear; year++) {
            const isCurrent = year === currentYear;
            const isSelected = year === selectedYear;

            let classes = 'h-12 flex items-center justify-center rounded text-sm cursor-pointer transition-colors font-medium ';

            if (isSelected) {
                classes += 'bg-blue-600 text-white font-bold';
            } else if (isCurrent) {
                classes += 'bg-blue-100 text-blue-600 dark:bg-blue-900 dark:text-blue-300';
            } else {
                classes += 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700';
            }

            html += `<div class="${classes}" data-year="${year}">${year}</div>`;
        }

        this.grid.className = 'calendar-picker-grid grid grid-cols-3 gap-2';
        this.grid.innerHTML = html;

        // Add click listeners
        this.grid.querySelectorAll('[data-year]').forEach(cell => {
            cell.addEventListener('click', () => {
                const year = parseInt(cell.dataset.year);
                this.selectYear(year);
            });
        });
    }

    selectDate(dateStr) {
        // Range mode: first click sets the start, second click sets the end
        // (and highlights everything in between). A click before the current
        // start restarts the range at that earlier date.
        if (this.range) {
            this.selectRangeDate(dateStr);
            return;
        }

        // Set the input value
        this.input.value = dateStr;

        // Trigger change event on input
        const event = new Event('change', { bubbles: true });
        this.input.dispatchEvent(event);

        // Call custom callback if provided
        if (this.onDateSelect) {
            this.onDateSelect(dateStr);
        }

        // Close the picker
        this.close();
    }

    selectRangeDate(dateStr) {
        const date = new Date(dateStr);
        date.setHours(0, 0, 0, 0);

        const hasStart = !!this.rangeStart;
        const hasEnd = !!this.rangeEnd;

        // No start yet, OR both already set → start a new range.
        if (!hasStart || (hasStart && hasEnd)) {
            this.rangeStart = date;
            this.rangeEnd = null;
            this.input.value = this.formatRangeForInput();
            this.render();
            if (this.onDateSelect) this.onDateSelect(this.getRange());
            return;
        }

        // We have a start but no end. Clicking before the start restarts.
        if (date < this.rangeStart) {
            this.rangeStart = date;
            this.rangeEnd = null;
            this.input.value = this.formatRangeForInput();
            this.render();
            if (this.onDateSelect) this.onDateSelect(this.getRange());
            return;
        }

        // Second click completes the range.
        this.rangeEnd = date;
        this.input.value = this.formatRangeForInput();

        const event = new Event('change', { bubbles: true });
        this.input.dispatchEvent(event);

        if (this.onDateSelect) this.onDateSelect(this.getRange());
        this.close();
    }

    // Returns { start, end } as 'YYYY-MM-DD' strings (end may be null while
    // the user is mid-selection). Only meaningful in range mode.
    getRange() {
        return {
            start: this.rangeStart ? this.formatDateForInput(this.rangeStart) : null,
            end: this.rangeEnd ? this.formatDateForInput(this.rangeEnd) : null,
        };
    }

    formatRangeForInput() {
        const s = this.rangeStart ? this.formatDateForInput(this.rangeStart) : '';
        const e = this.rangeEnd ? this.formatDateForInput(this.rangeEnd) : '';
        if (s && e) return `${s} → ${e}`;
        if (s) return `${s} → …`;
        return '';
    }

    formatDateForInput(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

// Helper function to initialize calendar pickers
function initCalendarPicker(inputId, options = {}) {
    return new CalendarPicker(inputId, options);
}

// Export for use in other scripts
if (typeof window !== 'undefined') {
    window.CalendarPicker = CalendarPicker;
    window.initCalendarPicker = initCalendarPicker;
}
