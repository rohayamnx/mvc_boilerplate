import "jsvectormap/dist/jsvectormap.min.css";
import "flatpickr/dist/flatpickr.min.css";
import "dropzone/dist/dropzone.css";
import "@fortawesome/fontawesome-free/css/all.min.css";
// import "../../css/tailadmin/tailadmin.css";

import Alpine from "alpinejs";
import persist from "@alpinejs/persist";
import flatpickr from "flatpickr";
import Dropzone from "dropzone";
import ApexCharts from "apexcharts";
import Chart from "chart.js/auto";
import QRCode from "qrcodejs";

import chart01 from "./components/charts/chart-01";
import chart02 from "./components/charts/chart-02";
import chart03 from "./components/charts/chart-03";
import map01 from "./components/map-01";
import "./components/calendar-init.js";
import "./components/image-resize";
import Iconify from '@iconify/iconify';
// import * as lucide from "lucide";

Alpine.plugin(persist);
window.Alpine = Alpine;
window.ApexCharts = ApexCharts;
window.Chart = Chart;
window.QRCode = QRCode;
// Defer component walk until deferred page scripts have run so any
// x-data globals they define exist before Alpine initializes components.
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", () => Alpine.start());
} else {
  Alpine.start();
}

// Init flatpickr
document.addEventListener("DOMContentLoaded", () => {
  const flatpickrBaseOptions = {
    appendTo: document.body,
    monthSelectorType: "static",
    dateFormat: "M j, Y",
    prevArrow:
      '<svg class="stroke-current" width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M15.25 6L9 12.25L15.25 18.5" stroke="" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    nextArrow:
      '<svg class="stroke-current" width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M8.75 19L15 12.75L8.75 6.5" stroke="" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  };

  const applyCustomClass = (instance) => {
    const customClass = instance.element.getAttribute("data-class");
    if (customClass) {
      instance.calendarContainer.classList.add(customClass);
    }
  };

  const getDateFormat = (element) =>
    element?.dataset?.dateFormat ?? flatpickrBaseOptions.dateFormat;

  const parseLocalDate = (value) => {
    if (!value) {
      return null;
    }
    const [year, month, day] = value.split("-").map(Number);
    if (!year || !month || !day) {
      return null;
    }
    return new Date(year, month - 1, day);
  };

  const formatLocalDate = (dateObj) => {
    if (!dateObj) {
      return "";
    }
    const pad = (value) => String(value).padStart(2, "0");
    return `${dateObj.getFullYear()}-${pad(dateObj.getMonth() + 1)}-${pad(dateObj.getDate())}`;
  };

  const datepickers = document.querySelectorAll(".datepicker");

  datepickers.forEach((datepicker) => {
    // Check if there are associated hidden fields (for form submission)
    const hiddenStartDate = document.getElementById("hiddenStartDate");
    const hiddenEndDate = document.getElementById("hiddenEndDate");

    // Get default dates from hidden fields if they exist
    const now = new Date();
    const sevenDaysAgo = new Date(now);
    sevenDaysAgo.setDate(now.getDate() - 6);
    let defaultDates = [sevenDaysAgo, now];
    if (hiddenStartDate && hiddenStartDate.value) {
      const startObj = parseLocalDate(hiddenStartDate.value);
      const endObj = hiddenEndDate?.value ? parseLocalDate(hiddenEndDate.value) : null;
      if (startObj && endObj) {
        defaultDates = [startObj, endObj];
      } else if (startObj) {
        defaultDates = [startObj];
      }
    }
    if (hiddenStartDate && defaultDates.length > 0) {
      hiddenStartDate.value = formatLocalDate(defaultDates[0]);
    }
    if (hiddenEndDate && defaultDates.length > 1) {
      hiddenEndDate.value = formatLocalDate(defaultDates[1]);
    }

    flatpickr(datepicker, {
      ...flatpickrBaseOptions,
      mode: "range",
      static: false,
      dateFormat: getDateFormat(datepicker),
      defaultDate: defaultDates,
      onReady: (selectedDates, dateStr, instance) => {
        // eslint-disable-next-line no-param-reassign
        instance.element.value = dateStr.replace("to", "-");
        applyCustomClass(instance);
      },
      onChange: (selectedDates, dateStr, instance) => {
        // eslint-disable-next-line no-param-reassign
        instance.element.value = dateStr.replace("to", "-");

        // Update hidden fields if they exist (for form submission)
        if (hiddenStartDate && hiddenEndDate) {
          if (selectedDates.length > 0) {
            const startDate = selectedDates[0];
            hiddenStartDate.value = formatLocalDate(startDate);

            if (selectedDates.length > 1) {
              const endDate = selectedDates[1];
              hiddenEndDate.value = formatLocalDate(endDate);
            } else {
              hiddenEndDate.value = hiddenStartDate.value;
            }
          } else {
            hiddenStartDate.value = '';
            hiddenEndDate.value = '';
          }
        }
      },
    });
  });

  const singleDateInputs = document.querySelectorAll(".flatpickr-single-date");

  const formatDateForHidden = (date) => {
    const pad = (value) => String(value).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  };

  const syncSingleDateHidden = (selectedDates, hiddenInput) => {
    if (!hiddenInput) {
      return;
    }

    if (selectedDates.length > 0) {
      hiddenInput.value = formatDateForHidden(selectedDates[0]);
    } else {
      hiddenInput.value = "";
    }
  };

  const initializeSingleDatePicker = (singleDateInput) => {
    const hiddenSelector = singleDateInput.dataset.flatpickrHiddenTarget;
    const hiddenInput = hiddenSelector ? document.querySelector(hiddenSelector) : null;
    const defaultSingleDate = hiddenInput?.value?.trim() ? hiddenInput.value : undefined;

    flatpickr(singleDateInput, {
      ...flatpickrBaseOptions,
      dateFormat: getDateFormat(singleDateInput),
      defaultDate: defaultSingleDate,
      parseDate: (datestr, format) => {
        // Parse ISO date strings (YYYY-MM-DD) correctly
        if (/^\d{4}-\d{2}-\d{2}$/.test(datestr)) {
          const [year, month, day] = datestr.split('-').map(Number);
          return new Date(year, month - 1, day);
        }
        return flatpickr.parseDate(datestr, format);
      },
      onReady: (selectedDates, dateStr, instance) => {
        if (!dateStr && defaultSingleDate) {
          instance.setDate(defaultSingleDate, true, getDateFormat(singleDateInput));
        }
        applyCustomClass(instance);
        syncSingleDateHidden(selectedDates, hiddenInput);
      },
      onChange: (selectedDates, dateStr, instance) => {
        instance.element.value = dateStr;
        syncSingleDateHidden(selectedDates, hiddenInput);
      },
    });
  };

  singleDateInputs.forEach(initializeSingleDatePicker);
});

// Init Dropzone
const dropzoneArea = document.querySelectorAll("#demo-upload");

if (dropzoneArea.length) {
  let myDropzone = new Dropzone("#demo-upload", { url: "/file/post" });
}

// Document Loaded
document.addEventListener("DOMContentLoaded", () => {
  chart01();
  chart02();
  chart03();
  map01();
  Iconify.scan();
  // lucide.createIcons();
});

// Get the current year
const year = document.getElementById("year");
if (year) {
  year.textContent = new Date().getFullYear();
}

// For Copy//
document.addEventListener("DOMContentLoaded", () => {
  const copyInput = document.getElementById("copy-input");
  if (copyInput) {
    // Select the copy button and input field
    const copyButton = document.getElementById("copy-button");
    const copyText = document.getElementById("copy-text");
    const websiteInput = document.getElementById("website-input");

    // Event listener for the copy button
    copyButton.addEventListener("click", () => {
      // Copy the input value to the clipboard
      navigator.clipboard.writeText(websiteInput.value).then(() => {
        // Change the text to "Copied"
        copyText.textContent = "Copied";

        // Reset the text back to "Copy" after 2 seconds
        setTimeout(() => {
          copyText.textContent = "Copy";
        }, 2000);
      });
    });
  }
});

document.addEventListener("DOMContentLoaded", function () {
  const searchInput = document.getElementById("search-input");
  const searchButton = document.getElementById("search-button");

  // Only set up search functionality if both elements exist
  if (searchInput && searchButton) {
    // Function to focus the search input
    function focusSearchInput() {
      searchInput.focus();
    }

    // Add click event listener to the search button
    searchButton.addEventListener("click", focusSearchInput);

    // Add keyboard event listener for Cmd+K (Mac) or Ctrl+K (Windows/Linux)
    document.addEventListener("keydown", function (event) {
      if ((event.metaKey || event.ctrlKey) && event.key === "k") {
        event.preventDefault(); // Prevent the default browser behavior
        focusSearchInput();
      }
    });

    // Add keyboard event listener for "/" key
    document.addEventListener("keydown", function (event) {
      if (event.key === "/" && document.activeElement !== searchInput) {
        event.preventDefault(); // Prevent the "/" character from being typed
        focusSearchInput();
      }
    });
  }
});
