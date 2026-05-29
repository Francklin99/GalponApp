using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using GalponApp.Domain.Models;
using GalponApp.Infrastructure.Services;

namespace GalponApp.Presentation.ViewModels
{
    [QueryProperty(nameof(Batch), "Batch")]
    public partial class BatchDetailViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private readonly FeedingCalculator _feedingCalculator;
        private readonly ReportService _reportService;

        [ObservableProperty]
        private Batch? batch;

        [ObservableProperty]
        private string recommendedFeedType = "Cargando...";

        [ObservableProperty]
        private double dailyFeedAmount;

        [ObservableProperty]
        private double dailyWaterAmount;

        [ObservableProperty]
        private string nutritionalInfo = string.Empty;

        [ObservableProperty]
        private int frequencyPerDay = 2;

        [ObservableProperty]
        private string recommendedHours = "07:00, 16:00";

        [ObservableProperty]
        private string alternativeFeeds = string.Empty;

        public ObservableCollection<WeightLog> WeightLogs { get; } = new();
        public ObservableCollection<Vaccination> Vaccinations { get; } = new();
        public ObservableCollection<SanitaryRecord> SanitaryRecords { get; } = new();

        public BatchDetailViewModel(FileStorageService storageService, FeedingCalculator feedingCalculator, ReportService reportService)
        {
            _storageService = storageService;
            _feedingCalculator = feedingCalculator;
            _reportService = reportService;
            Title = "Ficha de Lote";
        }

        partial void OnBatchChanged(Batch? value)
        {
            if (value != null)
            {
                Title = value.Name;
                _ = LoadDetailsAsync();
            }
        }

        [RelayCommand]
        public async Task LoadDetailsAsync()
        {
            if (Batch == null) return;
            IsBusy = true;

            try
            {
                // Cargar registros históricos
                var weights = await _storageService.GetWeightLogsForBatchAsync(Batch.Id);
                WeightLogs.Clear();
                foreach (var w in weights)
                {
                    WeightLogs.Add(w);
                }

                var vacs = await _storageService.GetVaccinationsForBatchAsync(Batch.Id);
                Vaccinations.Clear();
                foreach (var v in vacs)
                {
                    Vaccinations.Add(v);
                }

                var san = await _storageService.GetSanitaryRecordsForBatchAsync(Batch.Id);
                SanitaryRecords.Clear();
                foreach (var s in san)
                {
                    SanitaryRecords.Add(s);
                }

                // Cargar guía de alimentación inteligente
                var feedConfig = await _storageService.GetFeedingConfigForBatchAsync(Batch.CategoryId, Batch.Purpose, Batch.AgeInWeeks);
                var feedResult = _feedingCalculator.CalculateCurrentDailyNeeds(Batch, feedConfig, 1.2);
                
                RecommendedFeedType = feedResult.RecommendedFeedType;
                DailyFeedAmount = feedResult.DailyFeedNeededKg;
                DailyWaterAmount = feedResult.DailyWaterNeededLiters;
                NutritionalInfo = feedResult.NutritionalInfo;
                FrequencyPerDay = feedResult.FrequencyPerDay;
                RecommendedHours = feedResult.RecommendedHours;
                AlternativeFeeds = feedResult.Alternatives;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando ficha del lote: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Permite al usuario registrar un pesaje de forma ágil desde un Prompt
        [RelayCommand]
        public async Task AddWeightLogAsync()
        {
            if (Batch == null) return;

            string weightResult = await Shell.Current.DisplayPromptAsync(
                "Registrar Control de Peso", 
                "Ingresa el peso promedio actual en kg por animal:", 
                accept: "Guardar", 
                cancel: "Cancelar", 
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(weightResult) || !double.TryParse(weightResult, out double avgWeight) || avgWeight <= 0)
                return;

            string sizeResult = await Shell.Current.DisplayPromptAsync(
                "Registrar Control de Tamaño", 
                "Ingresa el tamaño promedio en cm (opcional, cancela para omitir):", 
                accept: "Guardar", 
                cancel: "Omitir", 
                keyboard: Keyboard.Numeric);

            double.TryParse(sizeResult, out double avgSize);

            string notesResult = await Shell.Current.DisplayPromptAsync(
                "Observación", 
                "Añade observaciones adicionales del pesaje:", 
                accept: "Guardar", 
                cancel: "Omitir");

            IsBusy = true;
            try
            {
                var newLog = new WeightLog
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = Batch.Id,
                    Date = DateTime.Today,
                    AverageWeight = avgWeight,
                    AverageSize = avgSize,
                    MortalityCount = 0,
                    Notes = string.IsNullOrWhiteSpace(notesResult) ? "Control periódico de crecimiento." : notesResult
                };

                await _storageService.SaveWeightLogAsync(newLog);
                
                // Actualizar lote en memoria
                Batch.CurrentWeight = avgWeight;
                OnPropertyChanged(nameof(Batch));

                await LoadDetailsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Registra tratamientos médicos y sanitarios
        [RelayCommand]
        public async Task AddSanitaryRecordAsync()
        {
            if (Batch == null) return;

            string diag = await Shell.Current.DisplayPromptAsync("Control Sanitario", "Diagnóstico o Enfermedad detectada:", accept: "Siguiente", cancel: "Cancelar");
            if (string.IsNullOrWhiteSpace(diag)) return;

            string countStr = await Shell.Current.DisplayPromptAsync("Animales Afectados", "Cantidad de animales enfermos:", accept: "Siguiente", cancel: "Cancelar", keyboard: Keyboard.Numeric);
            if (!int.TryParse(countStr, out int count) || count <= 0) return;

            string treat = await Shell.Current.DisplayPromptAsync("Tratamiento", "Tratamiento o procedimiento:", accept: "Siguiente", cancel: "Cancelar");
            string med = await Shell.Current.DisplayPromptAsync("Medicamento", "Nombre de medicamento / Antibiótico:", accept: "Siguiente", cancel: "Cancelar");
            string dose = await Shell.Current.DisplayPromptAsync("Dosis", "Dosis indicada (ej: 2ml subcutáneo):", accept: "Siguiente", cancel: "Cancelar");

            bool isolated = await Shell.Current.DisplayAlertAsync("Medida de Control", "¿Requiere aislamiento / cuarentena?", "Aislar Animales", "Permanecer en lote");

            string costStr = await Shell.Current.DisplayPromptAsync("Costos", "Costo estimado veterinario / fármacos en USD:", accept: "Guardar", cancel: "Omitir", keyboard: Keyboard.Numeric);
            double.TryParse(costStr, out double cost);

            IsBusy = true;
            try
            {
                var record = new SanitaryRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = Batch.Id,
                    BatchName = Batch.Name,
                    Diagnosis = diag,
                    AffectedCount = count,
                    Treatment = treat ?? "Ninguno",
                    Medication = med ?? "Ninguno",
                    Dose = dose ?? "No especificada",
                    StartDate = DateTime.Today,
                    IsIsolated = isolated,
                    Cost = cost,
                    Status = "Bajo Tratamiento",
                    Notes = isolated ? "Animales trasladados a zona de cuarentena y observación." : "Bajo tratamiento en galpón común."
                };

                await _storageService.SaveSanitaryRecordAsync(record);
                
                // Si hay diagnóstico de enfermedad grave, podemos cambiar el estado visual del lote
                Batch.SanitaryStatus = "Regular";
                await _storageService.SaveBatchAsync(Batch);
                
                await LoadDetailsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Aplica o deshace la aplicación de una vacuna
        [RelayCommand]
        public async Task ApplyVaccinationAsync(Vaccination vac)
        {
            if (vac == null) return;

            if (vac.AppliedDate.HasValue)
            {
                bool revert = await Shell.Current.DisplayAlertAsync("Vacuna Aplicada", "¿Deseas revertir la aplicación de esta dosis a 'Pendiente'?", "Sí, Revertir", "Cancelar");
                if (revert)
                {
                    vac.AppliedDate = null;
                    await _storageService.SaveVaccinationAsync(vac);
                    await LoadDetailsAsync();
                }
            }
            else
            {
                bool apply = await Shell.Current.DisplayAlertAsync("Aplicar Dosis", $"¿Confirmas la aplicación de '{vac.Name}' hoy?", "Sí, Aplicar", "Cancelar");
                if (apply)
                {
                    vac.AppliedDate = DateTime.Now;
                    await _storageService.SaveVaccinationAsync(vac);
                    await LoadDetailsAsync();
                }
            }
        }

        // Exportación y Compartición de Reporte CSV
        [RelayCommand]
        public async Task ExportCsvAsync()
        {
            if (Batch == null) return;
            IsBusy = true;

            try
            {
                string path = await _reportService.GenerateBatchReportCsvAsync(
                    Batch, 
                    WeightLogs.ToList(), 
                    Vaccinations.ToList(), 
                    SanitaryRecords.ToList());

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Exportar Excel - {Batch.Name}",
                    File = new ShareFile(path)
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error de Exportación", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Exportación y Compartición de Reporte HTML/PDF
        [RelayCommand]
        public async Task ExportHtmlAsync()
        {
            if (Batch == null) return;
            IsBusy = true;

            try
            {
                string path = await _reportService.GenerateBatchReportHtmlAsync(
                    Batch, 
                    WeightLogs.ToList(), 
                    Vaccinations.ToList(), 
                    SanitaryRecords.ToList());

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Exportar Ficha Imprimible - {Batch.Name}",
                    File = new ShareFile(path, "text/html")
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error de Exportación", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Eliminar lote
        [RelayCommand]
        public async Task DeleteBatchAsync()
        {
            if (Batch == null) return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Eliminar Lote", 
                $"¿Estás seguro de que deseas eliminar permanentemente el lote '{Batch.Name}' y todo su historial de vacunas, tratamientos y pesajes?", 
                "Sí, Eliminar Todo", 
                "Cancelar");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _storageService.DeleteBatchAsync(Batch.Id);
                await Shell.Current.DisplayAlertAsync("Lote Eliminado", "El lote fue retirado del sistema.", "Aceptar");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
