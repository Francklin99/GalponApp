using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using GalponApp.Domain.Models;
using GalponApp.Infrastructure.Services;

namespace GalponApp.Presentation.ViewModels
{
    public partial class ReportsViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private readonly FeedingCalculator _feedingCalculator;
        private readonly ReportService _reportService;

        [ObservableProperty]
        private Batch? selectedBatch;

        [ObservableProperty]
        private double weeklyFeedConsumption;

        [ObservableProperty]
        private double weeklyFeedCost;

        public ObservableCollection<Batch> Batches { get; } = new();
        public ObservableCollection<string> SpeciesSummaries { get; } = new();

        public ReportsViewModel(FileStorageService storageService, FeedingCalculator feedingCalculator, ReportService reportService)
        {
            _storageService = storageService;
            _feedingCalculator = feedingCalculator;
            _reportService = reportService;
            Title = "Reportes y Estadísticas";
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var allBatches = await _storageService.GetBatchesAsync();
                var activeBatches = allBatches.Where(b => b.IsActive).ToList();

                // 1. Cargar combo de lotes
                Batches.Clear();
                // Lote nulo representa "Todos los lotes"
                Batches.Add(new Batch { Id = "all", Name = "Todos los Lotes (Consolidado)" });
                foreach (var b in activeBatches)
                {
                    Batches.Add(b);
                }

                if (SelectedBatch == null)
                {
                    SelectedBatch = Batches.FirstOrDefault();
                }

                // 2. Resumen por especie
                SpeciesSummaries.Clear();
                var speciesGroups = activeBatches.GroupBy(b => b.CategoryId);
                foreach (var group in speciesGroups)
                {
                    string emoji = "🐾";
                    string name = group.First().CategoryName;
                    
                    if (group.Key == "porcinos") emoji = "🐷";
                    else if (group.Key.StartsWith("avicolas")) emoji = "🐔";
                    else if (group.Key.StartsWith("bovinos")) emoji = "🐄";
                    else if (group.Key == "ovinos") emoji = "🐑";
                    else if (group.Key == "caprinos") emoji = "🐐";
                    else if (group.Key == "cunicultura") emoji = "🐇";

                    int sumCount = group.Sum(b => b.Quantity);
                    int sumBatches = group.Count();
                    SpeciesSummaries.Add($"{emoji} {name}: {sumCount} animales en {sumBatches} lote(s)");
                }

                if (SpeciesSummaries.Count == 0)
                {
                    SpeciesSummaries.Add("⚠️ No hay animales registrados actualmente.");
                }

                // 3. Consumo y costos semanales proyectados
                double dailyFeed = 0;
                double dailyCost = 0;
                double costPerKg = 1.2;

                foreach (var b in activeBatches)
                {
                    var config = await _storageService.GetFeedingConfigForBatchAsync(b.CategoryId, b.Purpose, b.AgeInWeeks);
                    var res = _feedingCalculator.CalculateCurrentDailyNeeds(b, config, costPerKg);
                    dailyFeed += res.DailyFeedNeededKg;
                    dailyCost += res.DailyCost;
                }

                WeeklyFeedConsumption = dailyFeed * 7;
                WeeklyFeedCost = dailyCost * 7;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando reportes: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Exporta reporte según la selección
        [RelayCommand]
        public async Task ExportReportAsync(string format)
        {
            if (SelectedBatch == null) return;
            IsBusy = true;

            try
            {
                if (SelectedBatch.Id == "all")
                {
                    // Reporte consolidado general
                    await ExportGeneralConsolidatedAsync(format);
                }
                else
                {
                    // Reporte específico de un lote
                    var weights = await _storageService.GetWeightLogsForBatchAsync(SelectedBatch.Id);
                    var vacs = await _storageService.GetVaccinationsForBatchAsync(SelectedBatch.Id);
                    var san = await _storageService.GetSanitaryRecordsForBatchAsync(SelectedBatch.Id);

                    string path;
                    if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                    {
                        path = await _reportService.GenerateBatchReportCsvAsync(SelectedBatch, weights, vacs, san);
                    }
                    else
                    {
                        path = await _reportService.GenerateBatchReportHtmlAsync(SelectedBatch, weights, vacs, san);
                    }

                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = $"Reporte Lote - {SelectedBatch.Name}",
                        File = new ShareFile(path)
                    });
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"No se pudo exportar el reporte: {ex.Message}", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportGeneralConsolidatedAsync(string format)
        {
            var allBatches = (await _storageService.GetBatchesAsync()).Where(b => b.IsActive).ToList();
            
            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("REPORTE CONSOLIDADO DE GRANJA - GALPONAPP");
                sb.AppendLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine();
                sb.AppendLine("ID Lote,Nombre,Categoría,Raza,Propósito,Cantidad Inicial,Cantidad Actual,Mortalidad,Peso Inicial (kg),Peso Actual (kg),Estado Sanitario");
                
                foreach (var b in allBatches)
                {
                    sb.AppendLine($"\"{b.Id}\",\"{b.Name}\",\"{b.CategoryName}\",\"{b.Breed}\",\"{b.Purpose}\",{b.InitialQuantity},{b.Quantity},{b.MortalityCount},{b.InitialWeight:F2},{b.CurrentWeight:F2},\"{b.SanitaryStatus}\"");
                }

                string fileName = $"Reporte_Consolidado_{DateTime.Now:yyyyMMdd}.csv";
                string path = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Consolidado de Granja (Excel)",
                    File = new ShareFile(path)
                });
            }
            else
            {
                // Consolidado HTML
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Consolidado de Granja</title>");
                sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:40px;color:#333;} h1{color:#2e7d32;} table{width:100%;border-collapse:collapse;margin-top:20px;} th{background-color:#e8f5e9;color:#2e7d32;padding:12px;text-align:left;border-bottom:2px solid #c8e6c9;} td{padding:10px;border-bottom:1px solid #ddd;}</style>");
                sb.AppendLine("</head><body>");
                sb.AppendLine("<h1>Consolidado General de Producción</h1>");
                sb.AppendLine($"<p>Generado el {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
                sb.AppendLine("<table><thead><tr><th>Nombre Lote</th><th>Especie</th><th>Raza</th><th>Propósito</th><th>Cantidad</th><th>Mortalidad</th><th>Peso Actual</th><th>Salud</th></tr></thead><tbody>");
                
                foreach (var b in allBatches)
                {
                    sb.AppendLine($"<tr><td><strong>{b.Name}</strong></td><td>{b.CategoryName}</td><td>{b.Breed}</td><td>{b.Purpose}</td><td>{b.Quantity} cabezas</td><td>{b.MortalityCount} bajas</td><td>{b.CurrentWeight:F1} kg</td><td>{b.SanitaryStatus}</td></tr>");
                }

                sb.AppendLine("</tbody></table></body></html>");

                string fileName = $"Reporte_Consolidado_{DateTime.Now:yyyyMMdd}.html";
                string path = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Consolidado de Granja (Imprimible)",
                    File = new ShareFile(path, "text/html")
                });
            }
        }
    }
}
