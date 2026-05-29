using System;

namespace GalponApp.Domain.Models
{
    public class SanitaryRecord
    {
        public string Id { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty; // e.g. "Coccidiosis", "Mastitis"
        public int AffectedCount { get; set; } // Número de animales afectados
        public string Treatment { get; set; } = string.Empty; // Tratamiento aplicado
        public string Medication { get; set; } = string.Empty; // Medicamento suministrado
        public string Dose { get; set; } = string.Empty; // Dosis (ej: "1 ml por 5 días")
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsIsolated { get; set; } // Si están aislados/en cuarentena
        public double Cost { get; set; } // Costo del tratamiento/veterinario
        public string Status { get; set; } = "Bajo Tratamiento"; // Bajo Tratamiento, Recuperados, Fallecidos
        public string Notes { get; set; } = string.Empty;
    }
}
