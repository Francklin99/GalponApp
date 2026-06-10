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

        // Valores: "Saludable", "En observación", "Enfermo"
        [ObservableProperty]
        private string status = "Saludable";

        [JsonIgnore]
        public string StatusIcon => Status switch
        {
            "Saludable" => "💚",
            "Enfermo"   => "🔴",
            _           => "🟡"
        };

        [JsonIgnore]
        public Color StatusBg => Status switch
        {
            "Saludable" => Color.FromArgb("#F0FDF4"),
            "Enfermo"   => Color.FromArgb("#FFF1F2"),
            _           => Color.FromArgb("#FFFBEB")
        };

        [JsonIgnore]
        public Color StatusTextColor => Status switch
        {
            "Saludable" => Color.FromArgb("#15803D"),
            "Enfermo"   => Color.FromArgb("#991B1B"),
            _           => Color.FromArgb("#92400E")
        };

        public void NotifyStatusChanged()
        {
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(StatusBg));
            OnPropertyChanged(nameof(StatusTextColor));
        }
    }
}
