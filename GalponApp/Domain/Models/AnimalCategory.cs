using System.Collections.Generic;

namespace GalponApp.Domain.Models
{
    public class AnimalCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty; // Icono representativo
        public List<string> Breeds { get; set; } = new(); // Razas comunes
        public List<string> Purposes { get; set; } = new(); // Propósitos (Engorde, Reproducción, etc.)
    }
}
