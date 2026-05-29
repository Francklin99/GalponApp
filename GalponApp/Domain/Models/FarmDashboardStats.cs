using System.Collections.Generic;

namespace GalponApp.Domain.Models
{
    public class FarmDashboardStats
    {
        public int TotalAnimals { get; set; }
        public int ActiveBatchesCount { get; set; }
        public int UpcomingVaccinationsCount { get; set; }
        public int SickAnimalsCount { get; set; }
        public double MortalityRate { get; set; } // Porcentaje (%)
        public double DailyFeedConsumptionKg { get; set; } // Estimación de consumo diario total (kg)
        public double FeedCostEstimationToday { get; set; } // Costo estimado de alimentación diario
        public List<string> CriticalAlerts { get; set; } = new();
    }
}
