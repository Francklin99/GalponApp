namespace GalponApp.Domain.Models
{
    public class FeedingConfig
    {
        public string Id { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty; // e.g. "porcinos"
        public string Purpose { get; set; } = string.Empty; // e.g. "Engorde", "Reproducción"
        public int MinAgeWeeks { get; set; }
        public int MaxAgeWeeks { get; set; }
        public string FeedType { get; set; } = string.Empty; // e.g. "Pre-iniciador", "Engorde", "Ponedora"
        public double DailyAmountPerAnimal { get; set; } // En kilogramos (kg)
        public int FrequencyPerDay { get; set; } // Veces al día
        public string NutritionalInfo { get; set; } = string.Empty; // e.g. "Proteína: 18%, Humedad: 12%"
        public double RecommendedWaterLiters { get; set; } // Litros de agua recomendados por animal
        public string Alternatives { get; set; } = string.Empty; // e.g. "Marca X / Marca Y"
    }
}
