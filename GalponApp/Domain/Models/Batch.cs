using System;

namespace GalponApp.Domain.Models
{
    public class Batch
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty; // e.g. "porcinos"
        public string CategoryName { get; set; } = string.Empty; // e.g. "Porcinos"
        public string Breed { get; set; } = string.Empty; // Raza
        public int Quantity { get; set; } // Cantidad actual
        public int InitialQuantity { get; set; } // Cantidad inicial registrada
        public int MortalityCount { get; set; } // Animales fallecidos
        public DateTime BirthDate { get; set; } // Fecha de nacimiento
        public string Gender { get; set; } = "Mixto"; // Macho, Hembra, Mixto
        public double InitialWeight { get; set; } // Peso inicial promedio (kg)
        public double CurrentWeight { get; set; } // Peso actual promedio (kg)
        public string SanitaryStatus { get; set; } = "Excelente"; // Excelente, Regular, Enfermo, Aislado
        public string Purpose { get; set; } = string.Empty; // Engorde, Reproducción, Carne, Huevos, etc.
        public string Notes { get; set; } = string.Empty;
        public string QRCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Propiedad calculada para la edad en semanas o días
        public int AgeInDays => (DateTime.Today - BirthDate.Date).Days;
        public int AgeInWeeks => AgeInDays / 7;

        // Icono dinámico según la categoría
        public string CategoryIcon => CategoryId switch
        {
            "porcinos" => "🐷",
            "avicolas_engorde" => "🐣",
            "avicolas_postura" => "🐔",
            "bovinos_leche" => "🥛",
            "bovinos_carne" => "🐂",
            "ovinos" => "🐑",
            "caprinos" => "🐐",
            "equinos" => "🐴",
            "cunicultura" => "🐇",
            _ => "🐾"
        };
    }
}
