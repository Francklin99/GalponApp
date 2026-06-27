using CommunityToolkit.Mvvm.ComponentModel;

namespace GalponApp.Domain.Models
{
    public partial class Disease : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // e.g. "Porcinos", "Bovinos", "Aves", "Ovinos / Caprinos"
        public string Symptoms { get; set; } = string.Empty;
        public string WhatToDo { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        public string AlternativeTreatment { get; set; } = string.Empty;
        public string FeedingGuidance { get; set; } = string.Empty;
        public string Icon { get; set; } = "🦠";

        [ObservableProperty]
        private bool isExpanded;
    }
}
