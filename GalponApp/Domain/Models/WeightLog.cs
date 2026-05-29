using System;

namespace GalponApp.Domain.Models
{
    public class WeightLog
    {
        public string Id { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double AverageWeight { get; set; } // Peso promedio (kg)
        public double AverageSize { get; set; } // Tamaño promedio (cm, opcional)
        public int MortalityCount { get; set; } // Bajas registradas en este pesaje (mortalidad)
        public string Notes { get; set; } = string.Empty;
    }
}
