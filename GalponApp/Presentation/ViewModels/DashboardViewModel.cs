using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalponApp.Domain.Models;
using GalponApp.Infrastructure.Services;

namespace GalponApp.Presentation.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private readonly FeedingCalculator _feedingCalculator;

        [ObservableProperty]
        private int totalAnimals;

        [ObservableProperty]
        private int activeBatchesCount;

        [ObservableProperty]
        private int upcomingVaccinationsCount;

        [ObservableProperty]
        private int sickAnimalsCount;

        [ObservableProperty]
        private double mortalityRate;

        [ObservableProperty]
        private double dailyFeedConsumptionKg;

        [ObservableProperty]
        private double feedCostEstimationToday;

        [ObservableProperty]
        private double vaccinationProgress; // 0.0 a 1.0 (Vacunas aplicadas vs total)

        [ObservableProperty]
        private double healthComplianceProgress; // 0.0 a 1.0 (Animales sanos vs total)

        public ObservableCollection<string> CriticalAlerts { get; } = new();
        public ObservableCollection<Batch> TopBatches { get; } = new();

        public DashboardViewModel(FileStorageService storageService, FeedingCalculator feedingCalculator)
        {
            _storageService = storageService;
            _feedingCalculator = feedingCalculator;
            Title = "Dashboard";
        }

        [RelayCommand]
        public async Task LoadStatsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Cargar datos
                var batches = await _storageService.GetBatchesAsync();
                var activeBatches = batches.Where(b => b.IsActive).ToList();
                var vaccinations = await _storageService.GetVaccinationsAsync();
                var sanitaryRecords = await _storageService.GetSanitaryRecordsAsync();

                // 1. Estadísticas básicas de Lotes
                ActiveBatchesCount = activeBatches.Count;
                TotalAnimals = activeBatches.Sum(b => b.Quantity);
                
                int totalInitial = activeBatches.Sum(b => b.InitialQuantity);
                int totalMortality = activeBatches.Sum(b => b.MortalityCount);
                MortalityRate = totalInitial > 0 ? ((double)totalMortality / totalInitial) * 100 : 0;

                // 2. Control de Vacunas
                var activeBatchIds = activeBatches.Select(b => b.Id).ToHashSet();
                var relevantVaccinations = vaccinations.Where(v => activeBatchIds.Contains(v.BatchId)).ToList();
                
                int appliedVacs = relevantVaccinations.Count(v => v.Status == "Aplicada");
                int totalVacs = relevantVaccinations.Count;
                VaccinationProgress = totalVacs > 0 ? (double)appliedVacs / totalVacs : 1.0;

                // Próximas vacunas (futuras o atrasadas)
                UpcomingVaccinationsCount = relevantVaccinations.Count(v => v.Status == "Pendiente" || v.Status == "Atrasada");

                // 3. Control de Salud
                var activeSanitary = sanitaryRecords.Where(s => activeBatchIds.Contains(s.BatchId) && s.Status == "Bajo Tratamiento").ToList();
                SickAnimalsCount = activeSanitary.Sum(s => s.AffectedCount);
                
                HealthComplianceProgress = TotalAnimals > 0 ? (double)(TotalAnimals - SickAnimalsCount) / TotalAnimals : 1.0;

                // 4. Cálculos de Alimentación
                double totalFeed = 0;
                double totalCost = 0;
                double feedCostPerKg = 1.2; // Costo por kg por defecto en USD

                foreach (var batch in activeBatches)
                {
                    var config = await _storageService.GetFeedingConfigForBatchAsync(batch.CategoryId, batch.Purpose, batch.AgeInWeeks);
                    var result = _feedingCalculator.CalculateCurrentDailyNeeds(batch, config, feedCostPerKg);
                    totalFeed += result.DailyFeedNeededKg;
                    totalCost += result.DailyCost;
                }

                DailyFeedConsumptionKg = totalFeed;
                FeedCostEstimationToday = totalCost;

                // 5. Generar Alertas Críticas
                CriticalAlerts.Clear();
                
                // Vacunas atrasadas
                var overdueVaccines = relevantVaccinations.Where(v => v.Status == "Atrasada").ToList();
                foreach (var v in overdueVaccines)
                {
                    CriticalAlerts.Add($"⚠️ VACUNA ATRASADA: \"{v.Name}\" en el lote {v.BatchName} (Debió aplicarse el {v.ScheduledDate:dd/MM})");
                }

                // Animales enfermos/aislados
                foreach (var s in activeSanitary)
                {
                    if (s.IsIsolated)
                    {
                        CriticalAlerts.Add($"🚨 ENFERMEDAD: {s.AffectedCount} animales con \"{s.Diagnosis}\" aislados en {s.BatchName}");
                    }
                    else
                    {
                        CriticalAlerts.Add($"🩺 TRATAMIENTO: {s.AffectedCount} animales con \"{s.Diagnosis}\" en {s.BatchName}");
                    }
                }

                // Mortalidad elevada
                foreach (var b in activeBatches)
                {
                    double batchMortality = b.InitialQuantity > 0 ? ((double)b.MortalityCount / b.InitialQuantity) * 100 : 0;
                    if (batchMortality > 5.0)
                    {
                        CriticalAlerts.Add($"❌ ALTA MORTALIDAD: {b.Name} registra {b.MortalityCount} bajas ({batchMortality:F1}%)");
                    }
                }

                // Si no hay alertas
                if (CriticalAlerts.Count == 0)
                {
                    CriticalAlerts.Add("✅ Granja operativa: Todo en orden y sin alertas pendientes.");
                }

                // 6. Cargar los 3 lotes más importantes en el top
                TopBatches.Clear();
                foreach (var b in activeBatches.Take(3))
                {
                    TopBatches.Add(b);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar estadísticas del Dashboard: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
