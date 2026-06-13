using System;

namespace GalponApp.Domain.Models
{
    public class Batch
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        private string _categoryId = string.Empty;
        public string CategoryId 
        { 
            get => _categoryId ?? string.Empty; 
            set => _categoryId = value ?? string.Empty; 
        }

        private string _categoryName = string.Empty;
        public string CategoryName 
        { 
            get => _categoryName ?? string.Empty; 
            set => _categoryName = value ?? string.Empty; 
        }

        private string _breed = string.Empty;
        public string Breed 
        { 
            get => _breed ?? string.Empty; 
            set => _breed = value ?? string.Empty; 
        }

        public int Quantity { get; set; } // Cantidad actual
        public int InitialQuantity { get; set; } // Cantidad inicial registrada
        public int MortalityCount { get; set; } // Animales fallecidos
        public DateTime BirthDate { get; set; } // Fecha de nacimiento
        public string Gender { get; set; } = "Mixto"; // Macho, Hembra, Mixto
        public double InitialWeight { get; set; } // Peso inicial promedio (kg)
        public double CurrentWeight { get; set; } // Peso actual promedio (kg)

        private string _sanitaryStatus = "Excelente";
        public string SanitaryStatus 
        { 
            get => _sanitaryStatus ?? "Excelente"; 
            set => _sanitaryStatus = value ?? "Excelente"; 
        }

        private string _purpose = string.Empty;
        public string Purpose 
        { 
            get => _purpose ?? string.Empty; 
            set => _purpose = value ?? string.Empty; 
        }

        private string _notes = string.Empty;
        public string Notes 
        { 
            get => _notes ?? string.Empty; 
            set => _notes = value ?? string.Empty; 
        }

        private string _qrCode = string.Empty;
        public string QRCode 
        { 
            get => _qrCode ?? string.Empty; 
            set => _qrCode = value ?? string.Empty; 
        }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;
        public bool IsDivided { get; set; } = false;

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

        [System.Text.Json.Serialization.JsonIgnore]
        public string CardBgColor => IsDivided 
            ? "#E2E8F0" 
            : CategoryId switch
            {
                "porcinos" => "#FCE7F3",
                "bovinos_leche" => "#E0E7FF",
                "bovinos_carne" => "#FEF3C7",
                "avicolas_engorde" => "#FEF9C3",
                "avicolas_postura" => "#FFEDD5",
                "ovinos" => "#F1F5F9",
                "caprinos" => "#F3E8FF",
                "cunicultura" => "#CCFBF1",
                _ => "#D1FAE5"
            };

        [System.Text.Json.Serialization.JsonIgnore]
        public string CardStrokeColor => IsDivided 
            ? "#94A3B8" 
            : CategoryId switch
            {
                "porcinos" => "#F472B6",
                "bovinos_leche" => "#818CF8",
                "bovinos_carne" => "#FBBF24",
                "avicolas_engorde" => "#FACC15",
                "avicolas_postura" => "#FB923C",
                "ovinos" => "#94A3B8",
                "caprinos" => "#C084FC",
                "cunicultura" => "#2DD4BF",
                _ => "#34D399"
            };

        [System.Text.Json.Serialization.JsonIgnore]
        public double CardOpacity => IsDivided ? 0.65 : 1.0;
    }
}
