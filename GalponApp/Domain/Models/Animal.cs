using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace GalponApp.Domain.Models
{
    public partial class Animal : ObservableObject
    {
        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private string batchId = string.Empty;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private double weight;

        // Valores: "Saludable", "En observación", "Enfermo", "Inseminación pendiente", "Inseminada"
        [ObservableProperty]
        private string status = "Saludable";

        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Saludable"              => "💚",
            "Enfermo"                => "🔴",
            "Inseminación pendiente" => "🧬",
            "Inseminada"             => "🤰",
            _                        => "🟡" // En observación
        };

        [JsonIgnore]
        public Color StatusBg => Status switch
        {
            "Saludable"              => Color.FromArgb("#F0FDF4"),
            "Enfermo"                => Color.FromArgb("#FFF1F2"),
            "Inseminación pendiente" => Color.FromArgb("#F3E8FF"), // Púrpura
            "Inseminada"             => Color.FromArgb("#FCE7F3"), // Rosa
            _                        => Color.FromArgb("#FFFBEB")  // Amarillo
        };

        [JsonIgnore]
        public Color StatusTextColor => Status switch
        {
            "Saludable"              => Color.FromArgb("#15803D"),
            "Enfermo"                => Color.FromArgb("#991B1B"),
            "Inseminación pendiente" => Color.FromArgb("#7E22CE"), // Púrpura oscuro
            "Inseminada"             => Color.FromArgb("#BE185D"), // Rosa oscuro
            _                        => Color.FromArgb("#92400E")  // Naranja/Marrón oscuro
        };

        public void NotifyStatusChanged()
        {
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(StatusBg));
            OnPropertyChanged(nameof(StatusTextColor));
        }
    }
}
