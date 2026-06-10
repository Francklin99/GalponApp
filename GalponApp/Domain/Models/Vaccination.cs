using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GalponApp.Domain.Models
{
    public partial class Vaccination : ObservableObject
    {
        public string Id { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; // e.g. "Triple Aviar", "Parvovirus"
        public string Type { get; set; } = "Vacuna"; // Vacuna, Desparasitación, Vitamina, Refuerzo
        public string Description { get; set; } = string.Empty;
        public double Dose { get; set; } // Cantidad de dosis (ml, g, etc.)
        public string DoseUnit { get; set; } = "ml";
        public DateTime ScheduledDate { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string Status { get; set; } = "Pendiente"; // Pendiente, Aplicada, Atrasada
        public string Notes { get; set; } = string.Empty;
        public string Alternatives { get; set; } = string.Empty; // e.g. "Marca A / Marca B"

        [ObservableProperty]
        [JsonIgnore]
        private string customMedicationName = string.Empty;

        [ObservableProperty]
        [JsonIgnore]
        private string customDoseAmount = string.Empty;

        // We store these in JSON so they persist across sessions
        public string SavedCustomMedicationName { get; set; } = string.Empty;
        public string SavedCustomDoseAmount { get; set; } = string.Empty;

        [ObservableProperty]
        [JsonIgnore]
        private bool showCustomFields = false;

        public int DayNumber { get; set; }

        public bool IsApplied => AppliedDate.HasValue;
        public string AppliedSymbol => IsApplied ? "✓" : "⏰";
        public string AppliedColor => IsApplied ? "#2E7D32" : "#475569";
        public string AppliedBgColor => IsApplied ? "#E8F5E9" : "#F1F5F9";

        // Comprueba y actualiza el estado de la vacuna dinámicamente si no se ha aplicado
        public string DetermineStatus()
        {
            if (AppliedDate.HasValue)
                return "Aplicada";
            
            if (ScheduledDate.Date < DateTime.Today)
                return "Atrasada";
            
            return "Pendiente";
        }
    }
}
