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
    public partial class AlertsViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private List<Vaccination> _allVaccinations = new();

        [ObservableProperty]
        private string filterStatus = "Pendientes"; // Todas, Pendientes, Aplicadas, Atrasadas

        public ObservableCollection<Vaccination> Vaccinations { get; } = new();
        public List<string> FilterOptions { get; } = new() { "Pendientes", "Atrasadas", "Aplicadas", "Todas" };

        public AlertsViewModel(FileStorageService storageService)
        {
            _storageService = storageService;
            Title = "Alertas Sanitarias";
        }

        [RelayCommand]
        public async Task LoadVaccinationsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var activeBatches = (await _storageService.GetBatchesAsync()).Where(b => b.IsActive).Select(b => b.Id).ToHashSet();
                var vacs = await _storageService.GetVaccinationsAsync();
                
                // Solo cargar vacunas de lotes que aún están activos
                _allVaccinations = vacs.Where(v => activeBatches.Contains(v.BatchId)).ToList();
                
                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar calendario de vacunas (alertas): {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void ApplyFilter()
        {
            var filtered = _allVaccinations.AsEnumerable();

            switch (FilterStatus)
            {
                case "Pendientes":
                    filtered = filtered.Where(v => v.Status == "Pendiente");
                    break;
                case "Aplicadas":
                    filtered = filtered.Where(v => v.Status == "Aplicada");
                    break;
                case "Atrasadas":
                    filtered = filtered.Where(v => v.Status == "Atrasada");
                    break;
            }

            Vaccinations.Clear();
            foreach (var v in filtered)
            {
                Vaccinations.Add(v);
            }
        }

        // Marca o desmarca una vacuna directamente desde la lista unificada
        [RelayCommand]
        public async Task ToggleVaccinationAsync(Vaccination vac)
        {
            if (vac == null) return;

            if (vac.AppliedDate.HasValue)
            {
                bool revert = await Shell.Current.DisplayAlertAsync("Vacuna Aplicada", "¿Deseas marcar esta vacuna como 'Pendiente' de nuevo?", "Sí, Revertir", "Cancelar");
                if (revert)
                {
                    vac.AppliedDate = null;
                    vac.CustomMedicationName = string.Empty;
                    vac.CustomDoseAmount = string.Empty;
                    vac.ShowCustomFields = false;
                    await _storageService.SaveVaccinationAsync(vac);
                    await LoadVaccinationsAsync();
                }
            }
            else
            {
                string message = $"¿Confirmas la aplicación de '{vac.Name}' en el lote '{vac.BatchName}'?";
                if (!string.IsNullOrWhiteSpace(vac.CustomMedicationName))
                {
                    message = $"¿Confirmas la aplicación de '{vac.CustomMedicationName}' (dosis: {vac.CustomDoseAmount}) en el lote '{vac.BatchName}'?";
                }

                bool apply = await Shell.Current.DisplayAlertAsync("Aplicar Dosis", message, "Confirmar", "Cancelar");
                if (apply)
                {
                    vac.AppliedDate = DateTime.Now;
                    vac.ShowCustomFields = false;
                    if (!string.IsNullOrWhiteSpace(vac.CustomMedicationName))
                    {
                        vac.Notes = string.IsNullOrWhiteSpace(vac.Notes)
                            ? $"Aplicada vacuna alternativa: {vac.CustomMedicationName} - Dosis: {vac.CustomDoseAmount}."
                            : $"Aplicada vacuna alternativa: {vac.CustomMedicationName} - Dosis: {vac.CustomDoseAmount}. {vac.Notes}";
                    }
                    await _storageService.SaveVaccinationAsync(vac);
                    await LoadVaccinationsAsync();
                }
            }
        }

        [RelayCommand]
        public void ToggleAlternatives(Vaccination vac)
        {
            if (vac == null) return;
            vac.ShowAlternatives = !vac.ShowAlternatives;
        }

        // Navega a la ficha del lote correspondiente desde la vacuna, activando la pestaña de Vacunas
        [RelayCommand]
        public async Task GoToBatchAsync(Vaccination vac)
        {
            if (vac == null) return;

            var batches = await _storageService.GetBatchesAsync();
            var targetBatch = batches.FirstOrDefault(b => b.Id == vac.BatchId);

            if (targetBatch != null)
            {
                await Shell.Current.GoToAsync(nameof(BatchDetailPage), new Dictionary<string, object>
                {
                    { "Batch", targetBatch },
                    { "ActiveTab", "Vacunas" }
                });
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Lote No Encontrado", "El lote al que pertenece esta dosis no se encuentra disponible.", "Aceptar");
            }
        }
    }
}
