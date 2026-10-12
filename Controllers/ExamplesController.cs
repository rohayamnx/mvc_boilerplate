using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mvc_boilerplate.Models.ViewModels;
using mvc_boilerplate.Services;
using System.Text.Json;

namespace mvc_boilerplate.Controllers
{
    /// <summary>
    /// Hosts reference sample pages (shared components, etc.) so they are
    /// reachable from the sidebar while developing. Not business data.
    /// </summary>
    [Authorize]
    public class ExamplesController : Controller
    {
        private readonly IStaffHubApiService _staffHubApiService;

        public ExamplesController(IStaffHubApiService staffHubApiService)
        {
            _staffHubApiService = staffHubApiService;
        }

        /// <summary>
        /// Sample: Staff search selector that loads staff data from StaffHub
        /// server-side (no client-side API call). Mirrors the TrainingEvaluation
        /// Index pattern in iform_hra — search by Staff Id / Name with a dropdown.
        ///
        /// Future samples (department list, designation list, etc.) will be
        /// added to this same page below the staff search section.
        /// </summary>
        public async Task<IActionResult> StaffHubSample()
        {
            ViewData["Module"] = "Examples";

            try
            {
                var staff = await _staffHubApiService.GetAllStaffAsync(includeInactive: false);
                var items = staff
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        staffId = s.Id,
                        departmentName = s.DepartmentName,
                        contactNo = s.ContactNo,
                        isActive = s.IsActive
                    })
                    .OrderBy(x => x.name)
                    .ToList();
                ViewBag.StaffSelectorJson = JsonSerializer.Serialize(items);
            }
            catch
            {
                ViewBag.StaffSelectorJson = "[]";
            }

            return View();
        }
        // GET: /Examples/SharedComponentsSample
        public IActionResult SharedComponentsSample()
        {
            ViewData["Module"] = "Examples";
            return View();
        }

        // GET: /Examples/CalendarSample?view=monthly&year=&month=&weekStart=&date=
        // Sample calendar with daily / weekly / monthly views. Data is mocked
        // deterministically from the date so the grid always shows something.
        public IActionResult CalendarSample(
            string view = "monthly",
            int? year = null,
            int? month = null,
            string? weekStart = null,
            string? date = null)
        {
            ViewData["Module"] = "Examples";

            var today = DateOnly.FromDateTime(DateTime.Today);
            int y = year ?? today.Year;
            int m = month ?? today.Month;

            var vm = new CalendarSampleViewModel { ViewMode = view, Year = y, Month = m };

            switch (view.ToLowerInvariant())
            {
                case "daily":
                {
                    DateOnly sel = DateOnly.TryParse(date, out var d) ? d : today;
                    vm.SelectedDate = sel;
                    vm.NavigationLabel = sel.ToString("dddd, dd MMM yyyy");
                    vm.DayItems = GenerateItems(sel);
                    break;
                }
                case "weekly":
                {
                    DateOnly ws = DateOnly.TryParse(weekStart, out var w) ? w : today;
                    // Align to Monday (DayOfWeek.Sunday == 0).
                    int dow = ((int)ws.DayOfWeek + 6) % 7;
                    ws = ws.AddDays(-dow);
                    vm.WeekStart = ws;
                    vm.WeekDays = Enumerable.Range(0, 7)
                        .Select(i => new CalendarSampleDay { Date = ws.AddDays(i), Rows = GenerateItems(ws.AddDays(i)) })
                        .ToList();
                    var we = ws.AddDays(6);
                    vm.NavigationLabel = $"{ws:dd MMM} – {we:dd MMM yyyy}";
                    break;
                }
                default: // monthly
                {
                    vm.ViewMode = "monthly";
                    var first = new DateOnly(y, m, 1);
                    int daysInMonth = DateTime.DaysInMonth(y, m);
                    vm.MonthDays = Enumerable.Range(1, daysInMonth)
                        .Select(d => new CalendarSampleDay { Date = new DateOnly(y, m, d), Rows = GenerateItems(new DateOnly(y, m, d)) })
                        .ToList();
                    vm.NavigationLabel = first.ToString("MMMM yyyy");
                    break;
                }
            }

            return View(vm);
        }

        /// <summary>
        /// Deterministic mock absences for a given date so the calendar grid is
        /// populated without a database. ~50% of days have no absences; the rest
        /// have 1–3 distinct staff absent. Replace with a real query when
        /// adapting this sample.
        /// </summary>
        private static readonly string[] Staff = {
            "Ahmad Faiz", "Siti Aminah", "Tan Wei Lun", "Raj Kumar",
            "Emily Tan", "Hafiz Omar", "Lisa Wong", "Daniel Lee",
            "Nurul Huda", "Kevin Ong"
        };
        private static readonly string[] AbsenceTypes = { "Annual Leave", "Sick", "Emergency", "Unpaid", "Maternity" };
        private static readonly string[] Durations = { "Full day", "Half day (AM)", "Half day (PM)" };

        private static List<CalendarSampleItem> GenerateItems(DateOnly date)
        {
            int seed = date.Year * 10000 + date.Month * 100 + date.Day;
            var rnd = new Random(seed);

            if (rnd.Next(100) < 50) return new List<CalendarSampleItem>();

            int count = rnd.Next(1, 4);
            // Pick distinct staff for this day.
            var pool = Staff.OrderBy(_ => rnd.Next()).ToList();
            var items = new List<CalendarSampleItem>();
            for (int i = 0; i < count; i++)
            {
                items.Add(new CalendarSampleItem
                {
                    StaffName = pool[i],
                    AbsenceType = AbsenceTypes[rnd.Next(AbsenceTypes.Length)],
                    Duration = Durations[rnd.Next(Durations.Length)],
                    Remarks = ""
                });
            }
            return items;
        }
    }
}