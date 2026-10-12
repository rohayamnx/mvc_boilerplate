using System;
using System.Collections.Generic;
using System.Linq;

namespace mvc_boilerplate.Models.ViewModels
{
    /// <summary>
    /// Sample ViewModel backing the CalendarSample view (daily / weekly / monthly),
    /// themed as a STAFF ABSENCE calendar. Mirrors the shape of ckd_tracing's
    /// ProductionSequenceIndexViewModel so this file can be used as a reference
    /// template for real calendar pages.
    /// </summary>
    public class CalendarSampleViewModel
    {
        /// <summary>"daily" | "weekly" | "monthly"</summary>
        public string ViewMode { get; set; } = "monthly";

        public int Year { get; set; }
        public int Month { get; set; }

        /// <summary>Selected day in daily mode.</summary>
        public DateOnly SelectedDate { get; set; }

        /// <summary>Monday of the displayed week in weekly mode.</summary>
        public DateOnly WeekStart { get; set; }

        /// <summary>Human-readable label for the prev/next navigator.</summary>
        public string NavigationLabel { get; set; } = "";

        /// <summary>All days of the month for monthly mode.</summary>
        public List<CalendarSampleDay> MonthDays { get; set; } = new();

        /// <summary>The 7 days of the displayed week for weekly mode.</summary>
        public List<CalendarSampleDay> WeekDays { get; set; } = new();

        /// <summary>Absent staff for the selected day in daily mode.</summary>
        public List<CalendarSampleItem> DayItems { get; set; } = new();
    }

    /// <summary>
    /// One calendar day and the absences recorded on it.
    /// RowCount is derived so the view can show summary chips.
    /// </summary>
    public class CalendarSampleDay
    {
        public DateOnly Date { get; set; }
        public List<CalendarSampleItem> Rows { get; set; } = new();
        public int RowCount => Rows.Count;
    }

    /// <summary>
    /// A single staff absence entry. Names/types are mock data in this sample;
    /// a real page would map these from domain entities.
    /// </summary>
    public class CalendarSampleItem
    {
        public string StaffName { get; set; } = "";
        public string AbsenceType { get; set; } = "";   // Annual Leave, Sick, Emergency, Unpaid, Maternity
        public string Duration { get; set; } = "";       // Full day / Half day (AM) / Half day (PM)
        public string Remarks { get; set; } = "";
    }
}