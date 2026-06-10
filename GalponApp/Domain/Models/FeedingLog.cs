using System;

namespace GalponApp.Domain.Models
{
    public class FeedingLog
    {
        public string Id { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string StageName { get; set; } = string.Empty; // e.g. "Pre-inicio (Semanas 0-4)"
        public string FeedType { get; set; } = string.Empty; // e.g. "Pre-iniciador Porcino"
        public double DailyAmountPerAnimal { get; set; } // in kg
        public double TotalConsumedKg { get; set; } // total consumed in the period or daily distribution
        public string DateRange { get; set; } = string.Empty; // e.g. "12/04/2026 - 10/05/2026"
        public string Notes { get; set; } = string.Empty;
    }
}
