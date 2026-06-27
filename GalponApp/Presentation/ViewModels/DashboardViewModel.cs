using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using GalponApp.Domain.Models;
using GalponApp.Infrastructure.Services;
using GalponApp.Presentation.Views;

namespace GalponApp.Presentation.ViewModels
{
    public class DashboardAlert
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BadgeText { get; set; } = "➕";
        public string BadgeBg { get; set; } = "#FEE2E2";
        public string BadgeTextColor { get; set; } = "#EF4444";
    }

    public class MonthChartItem : ObservableObject
    {
        public string MonthName { get; set; } = string.Empty;
        public double VentaValue { get; set; }
        public double ReprValue { get; set; }
        public double MortValue { get; set; }

        public double VentaHeight => VentaValue * 1.1;
        public double ReprHeight => ReprValue * 1.1;
        public double MortHeight => MortValue * 1.1;
    }

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
        private int mortalityCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DailyFeedConsumptionKgDisplay))]
        private double dailyFeedConsumptionKg;

        public string DailyFeedConsumptionKgDisplay => $"{DailyFeedConsumptionKg:F1}kg";

        [ObservableProperty]
        private double feedCostEstimationToday;

        [ObservableProperty]
        private double feedCostEstimationMonth;

        [ObservableProperty]
        private double vaccinationProgress; // 0.0 a 1.0

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HealthIndex))]
        private double healthComplianceProgress; // 0.0 a 1.0

        public double HealthIndex => Math.Round(HealthComplianceProgress * 100);

        // Properties matching requested design
        [ObservableProperty]
        private int mothersCount;

        [ObservableProperty]
        private string mothersSubtext = "G:0 | L:0";

        [ObservableProperty]
        private int lechonesCount;

        [ObservableProperty]
        private string lechonesSubtext = "Recién nacidos";

        [ObservableProperty]
        private int engordeCount;

        [ObservableProperty]
        private string engordeSubtext = "Prod. Final";

        [ObservableProperty]
        private int padrillosCount;

        [ObservableProperty]
        private string padrillosSubtext = "Reproductores";

        [ObservableProperty]
        private string selectedFeedCategory = "Todos";

        [ObservableProperty]
        private string activeFeedAmountDisplay = "12.5kg";

        [ObservableProperty]
        private double activeFeedProgress = 1.0;

        [ObservableProperty]
        private double lechonesFeedKg = 3.8;

        [ObservableProperty]
        private double mothersFeedKg = 2.5;

        [ObservableProperty]
        private double engordeFeedKg = 5.6;

        [ObservableProperty]
        private double padrillosFeedKg = 0.6;

        [ObservableProperty]
        private int selectedFilterMonths = 3;

        public ObservableCollection<MonthChartItem> FilteredChartItems { get; } = new();
        private List<MonthChartItem> _allChartItems = new();

        public ObservableCollection<DashboardAlert> CriticalAlerts { get; } = new();
        public ObservableCollection<Batch> TopBatches { get; } = new();

        public DashboardViewModel(FileStorageService storageService, FeedingCalculator feedingCalculator)
        {
            _storageService = storageService;
            _feedingCalculator = feedingCalculator;
            Title = "Dashboard";

            // Inicializar datos del gráfico
            _allChartItems = new List<MonthChartItem>
            {
                new MonthChartItem { MonthName = "Ene", VentaValue = 40, ReprValue = 20, MortValue = 10 },
                new MonthChartItem { MonthName = "Feb", VentaValue = 60, ReprValue = 25, MortValue = 5 },
                new MonthChartItem { MonthName = "Mar", VentaValue = 80, ReprValue = 15, MortValue = 12 },
                new MonthChartItem { MonthName = "Abr", VentaValue = 50, ReprValue = 30, MortValue = 8 },
                new MonthChartItem { MonthName = "May", VentaValue = 70, ReprValue = 22, MortValue = 10 },
                new MonthChartItem { MonthName = "Jun", VentaValue = 95, ReprValue = 18, MortValue = 3 }
            };

            // Detectar ancho de pantalla por defecto de forma responsiva
            try
            {
                double mainDisplayWidth = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo.Width;
                double density = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo.Density;
                double widthDp = density > 0 ? mainDisplayWidth / density : 360;

                if (widthDp >= 600)
                {
                    SelectedFilterMonths = 6;
                }
                else
                {
                    SelectedFilterMonths = 3; // Por defecto 3 meses para pantallas de teléfonos más angostas
                }
            }
            catch
            {
                SelectedFilterMonths = 3;
            }

            ApplyChartFilter();
        }

        [RelayCommand]
        public async Task GoToRegisterBatchAsync()
        {
            await Shell.Current.GoToAsync(nameof(AddBatchPage));
        }

        [RelayCommand]
        public void SelectFeedCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return;
            SelectedFeedCategory = category;
        }

        [RelayCommand]
        public void ChangeFilterMonths(object parameter)
        {
            if (parameter == null) return;
            int months = 3;
            if (parameter is int mInt)
            {
                months = mInt;
            }
            else if (parameter is string mStr && int.TryParse(mStr, out int mParsed))
            {
                months = mParsed;
            }

            SelectedFilterMonths = months;
            ApplyChartFilter();
        }

        private void ApplyChartFilter()
        {
            FilteredChartItems.Clear();
            var itemsToTake = _allChartItems.Skip(Math.Max(0, _allChartItems.Count - SelectedFilterMonths)).ToList();
            foreach (var item in itemsToTake)
            {
                FilteredChartItems.Add(item);
            }
        }

        partial void OnSelectedFeedCategoryChanged(string value)
        {
            UpdateActiveFeedChart();
        }

        private void UpdateActiveFeedChart()
        {
            double total = LechonesFeedKg + EngordeFeedKg + MothersFeedKg + PadrillosFeedKg;
            if (SelectedFeedCategory == "Todos")
            {
                ActiveFeedAmountDisplay = $"{DailyFeedConsumptionKg:F1}kg";
                ActiveFeedProgress = 1.0;
            }
            else if (SelectedFeedCategory == "Lechones")
            {
                ActiveFeedAmountDisplay = $"{LechonesFeedKg:F1}kg";
                ActiveFeedProgress = total > 0 ? LechonesFeedKg / total : 0.30;
            }
            else if (SelectedFeedCategory == "Engorde")
            {
                ActiveFeedAmountDisplay = $"{EngordeFeedKg:F1}kg";
                ActiveFeedProgress = total > 0 ? EngordeFeedKg / total : 0.45;
            }
            else if (SelectedFeedCategory == "Madres")
            {
                ActiveFeedAmountDisplay = $"{MothersFeedKg:F1}kg";
                ActiveFeedProgress = total > 0 ? MothersFeedKg / total : 0.20;
            }
            else if (SelectedFeedCategory == "Padrillos")
            {
                ActiveFeedAmountDisplay = $"{PadrillosFeedKg:F1}kg";
                ActiveFeedProgress = total > 0 ? PadrillosFeedKg / total : 0.05;
            }
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

                if (ActiveBatchesCount == 0)
                {
                    // --- MODO DEMOSTRACIÓN (Base de datos sin lotes activos) ---
                    TotalAnimals = 225;
                    MothersCount = 20;
                    MothersSubtext = "G:12 | L:8";
                    LechonesCount = 80;
                    LechonesSubtext = "Recién nacidos";
                    EngordeCount = 120;
                    EngordeSubtext = "Prod. Final";
                    PadrillosCount = 5;
                    PadrillosSubtext = "Reproductores";

                    MortalityRate = 3;
                    MortalityCount = 3;
                    SickAnimalsCount = 5;
                    HealthComplianceProgress = 0.97;

                    DailyFeedConsumptionKg = 12.5;
                    LechonesFeedKg = 3.8;
                    EngordeFeedKg = 5.6;
                    MothersFeedKg = 2.5;
                    PadrillosFeedKg = 0.6;
                    
                    FeedCostEstimationToday = 15.00;
                    FeedCostEstimationMonth = 450.00;

                    CriticalAlerts.Clear();
                    CriticalAlerts.Add(new DashboardAlert
                    {
                        Title = "🚨 ¡Alerta Sanitaria en Lote 3! Se detectaron cerdos con Diarrea.",
                        Description = "Acción recomendada: Aislar a los animales afectados y revisar la guía de tratamiento rápido.",
                        BadgeText = "➕",
                        BadgeBg = "#FEE2E2",
                        BadgeTextColor = "#EF4444"
                    });
                    CriticalAlerts.Add(new DashboardAlert
                    {
                        Title = "📅 Tarea de Hoy: Vacunación pendiente para el Lote 1.",
                        Description = "Aplicar dosis programada contra Peste Porcina. (0 de 40 cerdos completados).",
                        BadgeText = "🗓️",
                        BadgeBg = "#F1F5F9",
                        BadgeTextColor = "#0F172A"
                    });
                }
                else
                {
                    // --- MODO REAL (Datos de la Base de Datos) ---
                    var animals = await _storageService.GetAnimalsAsync();
                    var activeBatchIds = activeBatches.Select(b => b.Id).ToHashSet();
                    
                    // Sincronizar y cargar animales para cada lote activo
                    var activeAnimals = new List<Animal>();
                    foreach (var batch in activeBatches)
                    {
                        var batchAnimals = animals.Where(a => a.BatchId == batch.Id).ToList();
                        if (batchAnimals.Count == 0 && batch.Quantity > 0)
                        {
                            batchAnimals = await _storageService.GetAnimalsForBatchAsync(batch.Id, batch.Quantity, batch.CurrentWeight);
                        }
                        activeAnimals.AddRange(batchAnimals);
                    }

                    TotalAnimals = activeBatches.Sum(b => b.Quantity);
                    
                    int totalInitial = activeBatches.Sum(b => b.InitialQuantity);
                    int totalMortality = activeBatches.Sum(b => b.MortalityCount);
                    MortalityRate = totalInitial > 0 ? ((double)totalMortality / totalInitial) * 100 : 0;
                    MortalityCount = totalMortality;

                    // Control de Vacunas
                    var relevantVaccinations = vaccinations.Where(v => activeBatchIds.Contains(v.BatchId)).ToList();
                    
                    int appliedVacs = relevantVaccinations.Count(v => v.Status == "Aplicada");
                    int totalVacs = relevantVaccinations.Count;
                    VaccinationProgress = totalVacs > 0 ? (double)appliedVacs / totalVacs : 1.0;

                    // Próximas vacunas (futuras o atrasadas)
                    UpcomingVaccinationsCount = relevantVaccinations.Count(v => v.Status == "Pendiente" || v.Status == "Atrasada");

                    // Control de Salud
                    var activeSanitary = sanitaryRecords.Where(s => activeBatchIds.Contains(s.BatchId) && s.Status == "Bajo Tratamiento").ToList();
                    
                    // Sumar cerdos con estado "Enfermo" de los animales cargados, y cerdos de tratamientos activos
                    int sickCountFromAnimals = activeAnimals.Count(a => a.Status == "Enfermo");
                    int sickCountFromSanitary = activeSanitary.Sum(s => s.AffectedCount);
                    SickAnimalsCount = Math.Max(sickCountFromAnimals, sickCountFromSanitary);
                    
                    HealthComplianceProgress = TotalAnimals > 0 ? (double)(TotalAnimals - SickAnimalsCount) / TotalAnimals : 1.0;

                    // Inventario por categorías
                    var mothersList = activeBatches.Where(b => 
                        b.Purpose.Equals("Madres", StringComparison.OrdinalIgnoreCase) || 
                        b.Purpose.Equals("Reproducción", StringComparison.OrdinalIgnoreCase) ||
                        b.Purpose.Contains("leche") || 
                        b.Purpose.Contains("lechera") ||
                        b.Purpose.Contains("huevos") || 
                        b.Purpose.Contains("postura") || 
                        b.Purpose.Contains("Postura")).ToList();
                    MothersCount = mothersList.Sum(b => b.Quantity);

                    int gestantes = mothersList.Sum(mb => activeAnimals.Where(a => a.BatchId == mb.Id).Count(a => a.Status == "Inseminada"));
                    int lactantes = MothersCount - gestantes;
                    if (lactantes < 0) lactantes = 0;
                    MothersSubtext = $"G:{gestantes} | L:{lactantes}";

                    // Lechones (Lechones / Crianza)
                    var lechonesList = activeBatches.Where(b => 
                        b.Purpose.Equals("Lechones", StringComparison.OrdinalIgnoreCase) || 
                        b.Purpose.Equals("Crianza", StringComparison.OrdinalIgnoreCase)).ToList();
                    LechonesCount = lechonesList.Sum(b => b.Quantity);
                    LechonesSubtext = "Recién nacidos";

                    // Engorde (Engorde / Producción de carne)
                    var engordeList = activeBatches.Where(b => 
                        b.Purpose.Equals("Engorde", StringComparison.OrdinalIgnoreCase) || 
                        b.Purpose.Equals("Producción de carne", StringComparison.OrdinalIgnoreCase) ||
                        b.Purpose.Equals("Carne", StringComparison.OrdinalIgnoreCase)).ToList();
                    EngordeCount = engordeList.Sum(b => b.Quantity);
                    EngordeSubtext = "Prod. Final";

                    // Padrillos (Padrillos)
                    var padrillosList = activeBatches.Where(b => 
                        b.Purpose.Equals("Padrillos", StringComparison.OrdinalIgnoreCase) || 
                        b.Purpose.Equals("Padrillo", StringComparison.OrdinalIgnoreCase) || 
                        b.Purpose.Contains("macho") || 
                        b.Purpose.Contains("Macho")).ToList();
                    PadrillosCount = padrillosList.Sum(b => b.Quantity);
                    PadrillosSubtext = "Reproductores";

                    // Cálculos de Alimentación
                    double totalFeed = 0;
                    double totalCost = 0;
                    double feedCostPerKg = 1.20; // En USD/Soles equivalente

                    double lechonesFeed = 0;
                    double mothersFeed = 0;
                    double engordeFeed = 0;
                    double padrillosFeed = 0;

                    foreach (var batch in activeBatches)
                    {
                        var config = await _storageService.GetFeedingConfigForBatchAsync(batch.CategoryId, batch.Purpose, batch.AgeInWeeks);
                        var result = _feedingCalculator.CalculateCurrentDailyNeeds(batch, config, feedCostPerKg);
                        totalFeed += result.DailyFeedNeededKg;
                        totalCost += result.DailyCost;

                        if (batch.Purpose.Equals("Lechones", StringComparison.OrdinalIgnoreCase) || batch.Purpose.Equals("Crianza", StringComparison.OrdinalIgnoreCase))
                        {
                            lechonesFeed += result.DailyFeedNeededKg;
                        }
                        else if (batch.Purpose.Equals("Engorde", StringComparison.OrdinalIgnoreCase) || batch.Purpose.Equals("Producción de carne", StringComparison.OrdinalIgnoreCase))
                        {
                            engordeFeed += result.DailyFeedNeededKg;
                        }
                        else if (batch.Purpose.Equals("Padrillos", StringComparison.OrdinalIgnoreCase) || batch.Purpose.Equals("Padrillo", StringComparison.OrdinalIgnoreCase))
                        {
                            padrillosFeed += result.DailyFeedNeededKg;
                        }
                        else
                        {
                            mothersFeed += result.DailyFeedNeededKg;
                        }
                    }

                    DailyFeedConsumptionKg = totalFeed;
                    FeedCostEstimationToday = totalCost;
                    FeedCostEstimationMonth = totalCost * 30;

                    LechonesFeedKg = lechonesFeed;
                    EngordeFeedKg = engordeFeed;
                    MothersFeedKg = mothersFeed;
                    PadrillosFeedKg = padrillosFeed;

                    // Generar Alertas Críticas Reales
                    CriticalAlerts.Clear();
                    
                    // Vacunas atrasadas o pendientes urgentes
                    var overdueVaccines = relevantVaccinations.Where(v => v.Status == "Atrasada" || (v.Status == "Pendiente" && v.ScheduledDate.Date == DateTime.Today.Date)).ToList();
                    foreach (var v in overdueVaccines)
                    {
                        var batch = activeBatches.FirstOrDefault(b => b.Id == v.BatchId);
                        int qty = batch != null ? batch.Quantity : 40;
                        CriticalAlerts.Add(new DashboardAlert
                        {
                            Title = $"📅 Tarea de Hoy: Vacunación pendiente para el {v.BatchName}.",
                            Description = $"Aplicar dosis programada contra Peste Porcina. (0 de {qty} cerdos completados).",
                            BadgeText = "🗓️",
                            BadgeBg = "#F1F5F9",
                            BadgeTextColor = "#0F172A"
                        });
                    }

                    // 1. Alertas de animales enfermos de forma individual (estado = Enfermo)
                    var sickAnimalsByBatch = activeAnimals.Where(a => a.Status == "Enfermo").GroupBy(a => a.BatchId).ToList();
                    foreach (var group in sickAnimalsByBatch)
                    {
                        var batch = activeBatches.FirstOrDefault(b => b.Id == group.Key);
                        if (batch != null)
                        {
                            int count = group.Count();
                            string pigStr = count == 1 ? "1 cerdo enfermo" : $"{count} cerdos enfermos";
                            CriticalAlerts.Add(new DashboardAlert
                            {
                                Title = $"🚨 ¡Alerta Sanitaria en {batch.Name}! Se detectó {pigStr}.",
                                Description = "Acción recomendada: Aislar a los animales afectados y revisar la guía de tratamiento rápido.",
                                BadgeText = "➕",
                                BadgeBg = "#FEE2E2",
                                BadgeTextColor = "#EF4444"
                            });
                        }
                    }

                    // 2. Alertas de tratamientos sanitarios activos
                    foreach (var s in activeSanitary)
                    {
                        // Evitamos duplicar si ya agregamos una alerta de animal enfermo para este lote
                        if (sickAnimalsByBatch.Any(g => g.Key == s.BatchId))
                            continue;

                        string diagnosis = s.Diagnosis;
                        if (diagnosis.Equals("diarrea", StringComparison.OrdinalIgnoreCase)) diagnosis = "Diarrea";
                        CriticalAlerts.Add(new DashboardAlert
                        {
                            Title = $"🚨 ¡Alerta Sanitaria en {s.BatchName}! Se detectaron cerdos con {diagnosis}.",
                            Description = "Acción recomendada: Aislar a los animales afectados y revisar la guía de tratamiento rápido.",
                            BadgeText = "➕",
                            BadgeBg = "#FEE2E2",
                            BadgeTextColor = "#EF4444"
                        });
                    }

                    // Si no hay alertas reales, mostrar mensaje "Todo en orden"
                    if (CriticalAlerts.Count == 0)
                    {
                        CriticalAlerts.Add(new DashboardAlert
                        {
                            Title = "✅ Todo en Orden",
                            Description = "No se registran alertas sanitarias ni tareas de vacunación vencidas hoy.",
                            BadgeText = "✓",
                            BadgeBg = "#E8F5E9",
                            BadgeTextColor = "#2E7D32"
                        });
                    }
                }

                // Actualizar gráfico de Alimento interactivo
                UpdateActiveFeedChart();

                // Cargar los 3 lotes en el top
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
