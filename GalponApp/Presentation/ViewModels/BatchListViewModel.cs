using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using GalponApp.Domain.Models;
using GalponApp.Infrastructure.Services;
using GalponApp.Presentation.Views;

namespace GalponApp.Presentation.ViewModels
{
    public partial class BatchListViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private readonly QRCodeService _qrCodeService;
        private List<Batch> _allBatches = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        [ObservableProperty]
        private AnimalCategory? selectedCategory;

        public ObservableCollection<Batch> Batches { get; } = new();
        public ObservableCollection<AnimalCategory> Categories { get; } = new();

        public BatchListViewModel(FileStorageService storageService, QRCodeService qrCodeService)
        {
            _storageService = storageService;
            _qrCodeService = qrCodeService;
            Title = "Lotes de Crianza";
        }

        [RelayCommand]
        public async Task LoadBatchesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Cargar categorías para el filtro
                var cats = await _storageService.GetCategoriesAsync();
                Categories.Clear();
                Categories.Add(new AnimalCategory { Id = "all", Name = "Todos", Icon = "🐾" });
                foreach (var c in cats)
                {
                    Categories.Add(c);
                }

                if (SelectedCategory == null)
                {
                    SelectedCategory = Categories.FirstOrDefault();
                }

                // Cargar lotes
                _allBatches = await _storageService.GetBatchesAsync();

                // Calcular IsCompleted para cada lote
                var allVacs = await _storageService.GetVaccinationsAsync();
                foreach (var b in _allBatches)
                {
                    var batchVacs = allVacs.Where(v => v.BatchId == b.Id).ToList();
                    b.IsCompleted = batchVacs.Count > 0 && batchVacs.All(v => v.Status == "Aplicada");
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando lotes: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ApplyFilters()
        {
            var filtered = _allBatches.AsEnumerable();

            // Filtrar por categoría
            if (SelectedCategory != null && SelectedCategory.Id != "all")
            {
                filtered = filtered.Where(b => b.CategoryId == SelectedCategory.Id);
            }

            // Filtrar por texto de búsqueda
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLower().Trim();
                filtered = filtered.Where(b => 
                    b.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                    b.Breed.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                    b.Purpose.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            // Ordenar: No divididos primero, divididos al final. Para los no divididos, ordenamos por IsCompleted (incompletos primero, completados después), luego por fecha desc
            filtered = filtered.OrderBy(b => b.IsDivided)
                               .ThenBy(b => b.IsCompleted)
                               .ThenByDescending(b => b.CreatedAt);

            Batches.Clear();
            foreach (var b in filtered)
            {
                Batches.Add(b);
            }
        }

        [RelayCommand]
        public async Task GoToBatchDetailAsync(Batch batch)
        {
            if (batch == null) return;

            // Navegar al detalle pasando el objeto lote
            await Shell.Current.GoToAsync(nameof(BatchDetailPage), new Dictionary<string, object>
            {
                { "Batch", batch }
            });
        }

        [RelayCommand]
        public async Task GoToAddBatchAsync()
        {
            await Shell.Current.GoToAsync(nameof(AddBatchPage));
        }

        [RelayCommand]
        public async Task DeleteBatchAsync(Batch batch)
        {
            if (batch == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Eliminar Lote", 
                $"¿Está seguro de que desea eliminar el lote '{batch.Name}' y todos sus registros asociados? Esta acción no se puede deshacer.", 
                "Sí, Eliminar", "Cancelar");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _storageService.DeleteBatchAsync(batch.Id);
                await LoadBatchesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar lote: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "No se pudo eliminar el lote.", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
