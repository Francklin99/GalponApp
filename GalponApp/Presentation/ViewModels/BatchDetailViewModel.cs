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

        [ObservableProperty]
        private string currentStageName = "Desconocido";

        [ObservableProperty]
        private double dailyFeedPerAnimal;

        [ObservableProperty]
        private double dailyWaterPerAnimal;

        [ObservableProperty]
        private string activeTab = "Vacunas";

        [ObservableProperty]
        private bool isVacunasTabVisible = true;

        [ObservableProperty]
        private bool isAlimentacionTabVisible = false;

        [ObservableProperty]
        private bool isAnimalesTabVisible = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDistributionValid))]
        [NotifyPropertyChangedFor(nameof(IsDistributionInvalid))]
        [NotifyPropertyChangedFor(nameof(DistributionWarningText))]
        private int healthyCount;

        [ObservableProperty]
        private int followUpCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDistributionValid))]
        [NotifyPropertyChangedFor(nameof(IsDistributionInvalid))]
        [NotifyPropertyChangedFor(nameof(DistributionWarningText))]
        private int sickCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDistributionValid))]
        [NotifyPropertyChangedFor(nameof(IsDistributionInvalid))]
        [NotifyPropertyChangedFor(nameof(DistributionWarningText))]
        private int observingCount;

        [ObservableProperty]
        private bool isClassificationRequired;

        public bool IsPigletBatch => Batch != null && (Batch.CategoryId == "porcinos" || Batch.Purpose.Equals("Lechones", StringComparison.OrdinalIgnoreCase));
        public bool ShowDivisionUndo => Batch != null && Batch.IsDivided;
        public bool ShowDivisionPending => IsPigletBatch && !Batch.IsDivided && !IsClassificationRequired;
        public bool ShowDivisionActive => IsPigletBatch && !Batch.IsDivided && IsClassificationRequired;
        public bool ShowDivisionCard => IsPigletBatch || (Batch != null && Batch.IsDivided);

        public ObservableCollection<WeightLog> WeightLogs { get; } = new();
        public ObservableCollection<Vaccination> Vaccinations { get; } = new();
        public ObservableCollection<SanitaryRecord> SanitaryRecords { get; } = new();
        [ObservableProperty]
        private ObservableCollection<FeedingStageItem> feedingStages = new();

        [ObservableProperty]
        private ObservableCollection<Animal> animals = new();

        [ObservableProperty]
        private int totalAnimalsCount;

        [ObservableProperty]
        private string animalesInfoText = string.Empty;

        [ObservableProperty]
        private bool showAnimalesLimit = false;

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
                    // Sync saved custom fields into observable properties
                    v.CustomMedicationName = v.SavedCustomMedicationName ?? string.Empty;
                    v.CustomDoseAmount = v.SavedCustomDoseAmount ?? string.Empty;
                    v.ShowCustomFields = false;
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

                DailyFeedPerAnimal = Batch.Quantity > 0 ? feedResult.DailyFeedNeededKg / Batch.Quantity : 0;
                DailyWaterPerAnimal = Batch.Quantity > 0 ? feedResult.DailyWaterNeededLiters / Batch.Quantity : 0;
                CurrentStageName = DetermineStageName(Batch.CategoryId, Batch.AgeInWeeks);

                // Cargar histórico y cronograma de etapas de la guía de alimentación
                var allConfigs = await _storageService.GetFeedingConfigsAsync();
                var categoryConfigs = allConfigs
                    .Where(c => c.CategoryId == Batch.CategoryId && 
                                (c.Purpose.Equals(Batch.Purpose, StringComparison.OrdinalIgnoreCase) ||
                                 ((c.Purpose.Equals("Reproducción", StringComparison.OrdinalIgnoreCase) || c.Purpose.Equals("Madres", StringComparison.OrdinalIgnoreCase)) &&
                                  (Batch.Purpose.Equals("Reproducción", StringComparison.OrdinalIgnoreCase) || Batch.Purpose.Equals("Madres", StringComparison.OrdinalIgnoreCase)))))
                    .OrderBy(c => c.MinAgeWeeks)
                    .ToList();

                FeedingStages.Clear();
                foreach (var config in categoryConfigs)
                {
                    var item = new FeedingStageItem
                    {
                        Config = config,
                        StageName = DetermineStageName(Batch.CategoryId, config.MinAgeWeeks),
                        DurationText = config.MaxAgeWeeks >= 999 
                            ? "Duración: Indefinida (Etapa Final)" 
                            : $"Duración: {config.MaxAgeWeeks - config.MinAgeWeeks + 1} semanas"
                    };

                    // Calcular fechas de vigencia
                    var start = Batch.BirthDate.AddDays(config.MinAgeWeeks * 7);
                    if (config.MaxAgeWeeks >= 999)
                    {
                        item.DateRangeText = $"{start:dd/MM/yyyy} en adelante";
                    }
                    else
                    {
                        var end = Batch.BirthDate.AddDays((config.MaxAgeWeeks * 7) + 6);
                        item.DateRangeText = $"{start:dd/MM/yyyy} al {end:dd/MM/yyyy}";
                    }

                    // Determinar estado de la etapa respecto a la edad actual
                    int currentAge = Batch.AgeInWeeks;
                    if (currentAge > config.MaxAgeWeeks)
                    {
                        item.Status = "Past";
                        item.StatusText = "Cumplida";
                        item.IsExpanded = false;
                    }
                    else if (currentAge >= config.MinAgeWeeks && currentAge <= config.MaxAgeWeeks)
                    {
                        item.Status = "Current";
                        item.StatusText = "Etapa Actual";
                        item.IsExpanded = true; // Expandido por defecto
                    }
                    else
                    {
                        item.Status = "Future";
                        item.StatusText = "Próxima Etapa";
                        item.IsExpanded = false;
                    }

                    FeedingStages.Add(item);
                }
                // Cargar animales del lote
                // NOTA: BindableLayout no virtualiza; para evitar crash con lotes grandes
                // se limita el render a 50 animales y se muestra el total real.
                const int MaxDisplayedAnimals = 50;
                var animalsList = await _storageService.GetAnimalsForBatchAsync(Batch.Id, Batch.Quantity, Batch.CurrentWeight);
                TotalAnimalsCount = animalsList.Count;
                var displayedAnimals = animalsList.Take(MaxDisplayedAnimals).ToList();
                Animals = new ObservableCollection<Animal>(displayedAnimals);
                ShowAnimalesLimit = TotalAnimalsCount > MaxDisplayedAnimals;
                AnimalesInfoText = ShowAnimalesLimit
                    ? $"Mostrando {MaxDisplayedAnimals} de {TotalAnimalsCount} animales"
                    : $"{TotalAnimalsCount} animales en este lote";
                UpdateAnimalCounters(animalsList);
                IsClassificationRequired = Batch.IsActive && 
                                           !Batch.IsDivided && 
                                           Vaccinations.Count > 0 && 
                                           Vaccinations.All(v => v.Status == "Aplicada") && 
                                           (Batch.CategoryId == "porcinos" ? Batch.AgeInWeeks >= 8 : true);

                OnPropertyChanged(nameof(IsPigletBatch));
                OnPropertyChanged(nameof(ShowDivisionUndo));
                OnPropertyChanged(nameof(ShowDivisionPending));
                OnPropertyChanged(nameof(ShowDivisionActive));
                OnPropertyChanged(nameof(ShowDivisionCard));
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
                    vac.Status = vac.DetermineStatus();
                    vac.SavedCustomMedicationName = string.Empty;
                    vac.SavedCustomDoseAmount = string.Empty;
                    vac.CustomMedicationName = string.Empty;
                    vac.CustomDoseAmount = string.Empty;
                    vac.ShowCustomFields = false;
                    await _storageService.SaveVaccinationAsync(vac);
                    await LoadDetailsAsync();
                }
            }
            else
            {
                string message = $"¿Confirmas la aplicación de '{vac.Name}' hoy?";
                if (!string.IsNullOrWhiteSpace(vac.CustomMedicationName))
                {
                    message = $"¿Confirmas la aplicación de '{vac.CustomMedicationName}' (dosis: {vac.CustomDoseAmount}) hoy?";
                }

                bool apply = await Shell.Current.DisplayAlertAsync("Aplicar Dosis", message, "Sí, Aplicar", "Cancelar");
                if (apply)
                {
                    vac.AppliedDate = DateTime.Now;
                    vac.Status = "Aplicada";
                    vac.ShowCustomFields = false;
                    if (!string.IsNullOrWhiteSpace(vac.CustomMedicationName))
                    {
                        // Persist the custom fields to saved properties for JSON serialization
                        vac.SavedCustomMedicationName = vac.CustomMedicationName;
                        vac.SavedCustomDoseAmount = vac.CustomDoseAmount;
                        vac.Notes = string.IsNullOrWhiteSpace(vac.Notes)
                            ? $"Aplicada vacuna alternativa: {vac.CustomMedicationName} - Dosis: {vac.CustomDoseAmount}."
                            : $"Aplicada vacuna alternativa: {vac.CustomMedicationName} - Dosis: {vac.CustomDoseAmount}. {vac.Notes}";
                    }
                    await _storageService.SaveVaccinationAsync(vac);
                    await LoadDetailsAsync();
                }
            }
        }

        [RelayCommand]
        public void ToggleCustomFields(Vaccination vac)
        {
            if (vac == null) return;
            // ShowCustomFields is an [ObservableProperty] on Vaccination (ObservableObject)
            // so the UI will automatically update via INotifyPropertyChanged
            vac.ShowCustomFields = !vac.ShowCustomFields;
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

        [RelayCommand]
        public void SetTab(string tabName)
        {
            ActiveTab = tabName;
            IsVacunasTabVisible = tabName == "Vacunas";
            IsAlimentacionTabVisible = tabName == "Alimentación";
            IsAnimalesTabVisible = tabName == "Animales";
        }

        [RelayCommand]
        public void ToggleStageExpanded(FeedingStageItem stageItem)
        {
            if (stageItem != null)
            {
                stageItem.IsExpanded = !stageItem.IsExpanded;
            }
        }

        private string DetermineStageName(string categoryId, int ageInWeeks)
        {
            if (categoryId == "porcinos")
            {
                if (ageInWeeks <= 4) return "Pre-inicio";
                if (ageInWeeks <= 8) return "Inicio";
                if (ageInWeeks <= 16) return "Crecimiento";
                return "Engorde";
            }
            else if (categoryId.StartsWith("avicolas"))
            {
                if (ageInWeeks <= 6) return "Crianza";
                if (ageInWeeks <= 18) return "Desarrollo";
                return "Postura";
            }
            else if (categoryId.StartsWith("bovinos"))
            {
                if (ageInWeeks <= 12) return "Ternero Iniciador";
                if (ageInWeeks <= 50) return "Crecimiento";
                return "Lactancia / Producción";
            }
            return "Desarrollo General";
        }

        public bool IsDistributionValid => (HealthyCount + ObservingCount + SickCount) == TotalAnimalsCount;
        public bool IsDistributionInvalid => !IsDistributionValid;
        public string DistributionWarningText => $"La suma ({HealthyCount + ObservingCount + SickCount}) debe ser igual al total del lote ({TotalAnimalsCount})";

        private bool _isAdjustingCounters = false;

        partial void OnHealthyCountChanged(int value)
        {
            if (_isAdjustingCounters || Batch == null) return;
            _isAdjustingCounters = true;
            try
            {
                if (value > TotalAnimalsCount)
                {
                    HealthyCount = TotalAnimalsCount;
                }
                else if (value < 0)
                {
                    HealthyCount = 0;
                }

                int remaining = TotalAnimalsCount - HealthyCount;
                if (SickCount <= remaining)
                {
                    ObservingCount = remaining - SickCount;
                }
                else
                {
                    SickCount = remaining;
                    ObservingCount = 0;
                }
            }
            finally
            {
                _isAdjustingCounters = false;
            }
        }

        partial void OnObservingCountChanged(int value)
        {
            if (_isAdjustingCounters || Batch == null) return;
            _isAdjustingCounters = true;
            try
            {
                if (value > TotalAnimalsCount)
                {
                    ObservingCount = TotalAnimalsCount;
                }
                else if (value < 0)
                {
                    ObservingCount = 0;
                }

                int remaining = TotalAnimalsCount - ObservingCount;
                if (SickCount <= remaining)
                {
                    HealthyCount = remaining - SickCount;
                }
                else
                {
                    SickCount = remaining;
                    HealthyCount = 0;
                }
            }
            finally
            {
                _isAdjustingCounters = false;
            }
        }

        partial void OnSickCountChanged(int value)
        {
            if (_isAdjustingCounters || Batch == null) return;
            _isAdjustingCounters = true;
            try
            {
                if (value > TotalAnimalsCount)
                {
                    SickCount = TotalAnimalsCount;
                }
                else if (value < 0)
                {
                    SickCount = 0;
                }

                int remaining = TotalAnimalsCount - SickCount;
                if (ObservingCount <= remaining)
                {
                    HealthyCount = remaining - ObservingCount;
                }
                else
                {
                    ObservingCount = remaining;
                    HealthyCount = 0;
                }
            }
            finally
            {
                _isAdjustingCounters = false;
            }
        }

        [RelayCommand]
        public async Task ApplyDistributionAsync()
        {
            if (Batch == null) return;
            if (!IsDistributionValid)
            {
                await Shell.Current.DisplayAlert("Distribución Inválida", DistributionWarningText, "Aceptar");
                return;
            }

            IsBusy = true;
            try
            {
                var allAnimals = await _storageService.GetAnimalsForBatchAsync(Batch.Id, Batch.Quantity, Batch.CurrentWeight);
                
                int targetHealthy = HealthyCount;
                int targetObserving = ObservingCount;
                int targetSick = SickCount;

                int index = 0;
                for (int i = 0; i < targetHealthy && index < allAnimals.Count; i++, index++)
                {
                    allAnimals[index].Status = "Saludable";
                }
                for (int i = 0; i < targetObserving && index < allAnimals.Count; i++, index++)
                {
                    allAnimals[index].Status = "En observación";
                }
                for (int i = 0; i < targetSick && index < allAnimals.Count; i++, index++)
                {
                    allAnimals[index].Status = "Enfermo";
                }

                foreach (var animal in allAnimals)
                {
                    await _storageService.SaveAnimalAsync(animal);
                }

                await LoadDetailsAsync();
                await Shell.Current.DisplayAlert("Distribución Guardada", "Se ha actualizado el estado de todos los animales.", "Aceptar");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateAnimalCounters(List<Animal> allAnimals)
        {
            HealthyCount = allAnimals.Count(a => a.Status == "Saludable");
            SickCount = allAnimals.Count(a => a.Status == "Enfermo");
            ObservingCount = allAnimals.Count(a => a.Status == "En observación");
            FollowUpCount = SickCount + ObservingCount;
        }

        [RelayCommand]
        public async Task RenameAnimalAsync(Animal animal)
        {
            if (animal == null) return;
            string newName = await Shell.Current.DisplayPromptAsync(
                "Renombrar Animal",
                $"Nuevo nombre para {animal.Name}:",
                accept: "Guardar",
                cancel: "Cancelar",
                initialValue: animal.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != animal.Name)
            {
                animal.Name = newName.Trim();
                await _storageService.SaveAnimalAsync(animal);
                await LoadDetailsAsync();
            }
        }

        [RelayCommand]
        public async Task ChangeAnimalStatusAsync(Animal animal)
        {
            if (animal == null) return;

            string status = await Shell.Current.DisplayActionSheetAsync(
                "Estado de Salud",
                "Cancelar",
                null,
                "Saludable",
                "En observación",
                "Enfermo");

            if (!string.IsNullOrEmpty(status) && status != "Cancelar" && status != animal.Status)
            {
                animal.Status = status;
                animal.NotifyStatusChanged();
                await _storageService.SaveAnimalAsync(animal);
                await LoadDetailsAsync();
            }
        }

        [RelayCommand]
        public async Task SetAllAnimalsStatusAsync()
        {
            if (Batch == null) return;

            string status = await Shell.Current.DisplayActionSheetAsync(
                "Establecer Estado del Grupo",
                "Cancelar",
                null,
                "Saludable",
                "En observación",
                "Enfermo");

            if (!string.IsNullOrEmpty(status) && status != "Cancelar")
            {
                IsBusy = true;
                try
                {
                    var allAnimals = await _storageService.GetAnimalsForBatchAsync(Batch.Id, Batch.Quantity, Batch.CurrentWeight);
                    foreach (var animal in allAnimals)
                    {
                        if (animal.Status != status)
                        {
                            animal.Status = status;
                            await _storageService.SaveAnimalAsync(animal);
                        }
                    }
                    await LoadDetailsAsync();
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        public async Task SplitBatchAsync()
        {
            if (Batch == null) return;
            await Shell.Current.GoToAsync("ClassifyBatchPage", new Dictionary<string, object>
            {
                { "Batch", Batch }
            });
        }

        [RelayCommand]
        public async Task UndoSplitBatchAsync()
        {
            if (Batch == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Deshacer División", 
                "¿Está seguro de que desea deshacer la división? Esto eliminará los lotes creados a partir de este y devolverá los animales a este lote.", 
                "Sí, Deshacer", "Cancelar");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                // 1. Obtener todos los lotes del sistema
                var allBatches = await _storageService.GetBatchesAsync();
                
                // 2. Encontrar los sub-lotes creados por este lote original
                var subBatches = allBatches
                    .Where(b => b.Notes.Contains($"(ID: {Batch.Id})"))
                    .ToList();

                // 3. Obtener todos los animales
                var allAnimals = await _storageService.GetAnimalsAsync();

                foreach (var sb in subBatches)
                {
                    // Mover los animales de vuelta al lote padre
                    var sbAnimals = allAnimals.Where(a => a.BatchId == sb.Id).ToList();
                    foreach (var animal in sbAnimals)
                    {
                        animal.BatchId = Batch.Id;
                        await _storageService.SaveAnimalAsync(animal);
                    }

                    // Eliminar las vacunas asociadas al sub-lote
                    var sbVacs = await _storageService.GetVaccinationsForBatchAsync(sb.Id);
                    foreach (var v in sbVacs)
                    {
                        await _storageService.DeleteVaccinationAsync(v.Id);
                    }

                    // Eliminar el sub-lote
                    await _storageService.DeleteBatchAsync(sb.Id);
                }

                // 4. Reactivar el lote padre y quitar el flag de división
                Batch.IsActive = true;
                Batch.IsDivided = false;
                await _storageService.SaveBatchAsync(Batch);

                await Shell.Current.DisplayAlert("División Revertida", 
                    "La división se ha deshecho con éxito. Los animales han retornado a este lote.", 
                    "Aceptar");

                // Recargar los detalles de la vista
                await LoadDetailsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", 
                    $"No se pudo deshacer la división: {ex.Message}", 
                    "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public partial class FeedingStageItem : ObservableObject
    {
        [ObservableProperty]
        private FeedingConfig config = new();

        [ObservableProperty]
        private string stageName = string.Empty;

        [ObservableProperty]
        private string status = string.Empty; // Past, Current, Future

        [ObservableProperty]
        private string statusText = string.Empty;

        [ObservableProperty]
        private string dateRangeText = string.Empty;

        [ObservableProperty]
        private string durationText = string.Empty;

        [ObservableProperty]
        private bool isExpanded;

        public bool IsCurrent => Status == "Current";
        public bool IsPast => Status == "Past";
        public bool IsFuture => Status == "Future";
    }
}
