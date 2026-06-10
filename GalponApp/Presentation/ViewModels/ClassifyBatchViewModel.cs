using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using GalponApp.Domain.Models;
using GalponApp.Infrastructure.Services;

namespace GalponApp.Presentation.ViewModels
{
    public class AnimalClassificationItem : ObservableObject
    {
        public Animal Animal { get; set; } = null!;

        private string _selectedPurpose = string.Empty; // "Engorde", "Madres", "Padrillos", or empty
        public string SelectedPurpose
        {
            get => _selectedPurpose;
            set
            {
                if (SetProperty(ref _selectedPurpose, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(IsEngorde));
                    OnPropertyChanged(nameof(IsMadres));
                    OnPropertyChanged(nameof(IsPadrillos));
                }
            }
        }

        public bool IsEngorde
        {
            get => SelectedPurpose == "Engorde";
            set { if (value) SelectedPurpose = "Engorde"; else if (SelectedPurpose == "Engorde") SelectedPurpose = string.Empty; }
        }

        public bool IsMadres
        {
            get => SelectedPurpose == "Madres";
            set { if (value) SelectedPurpose = "Madres"; else if (SelectedPurpose == "Madres") SelectedPurpose = string.Empty; }
        }

        public bool IsPadrillos
        {
            get => SelectedPurpose == "Padrillos";
            set { if (value) SelectedPurpose = "Padrillos"; else if (SelectedPurpose == "Padrillos") SelectedPurpose = string.Empty; }
        }
    }

    [QueryProperty(nameof(Batch), "Batch")]
    public partial class ClassifyBatchViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private bool _isSyncing;

        [ObservableProperty]
        private Batch? batch;

        private int _engordeCount;
        public int EngordeCount
        {
            get => _engordeCount;
            set
            {
                if (SetProperty(ref _engordeCount, value))
                {
                    OnCountEdited();
                }
            }
        }

        private int _madresCount;
        public int MadresCount
        {
            get => _madresCount;
            set
            {
                if (SetProperty(ref _madresCount, value))
                {
                    OnCountEdited();
                }
            }
        }

        private int _padrillosCount;
        public int PadrillosCount
        {
            get => _padrillosCount;
            set
            {
                if (SetProperty(ref _padrillosCount, value))
                {
                    OnCountEdited();
                }
            }
        }

        [ObservableProperty]
        private string engordePercentageText = "0%";

        [ObservableProperty]
        private string madresPercentageText = "0%";

        [ObservableProperty]
        private string padrillosPercentageText = "0%";

        [ObservableProperty]
        private int assignedCount;

        [ObservableProperty]
        private int unassignedCount;

        [ObservableProperty]
        private int totalCount;

        [ObservableProperty]
        private bool isConfirmEnabled;

        [ObservableProperty]
        private string confirmButtonText = "Faltan animales";

        public ObservableCollection<AnimalClassificationItem> Animals { get; } = new();
        public List<string> PurposeOptions { get; } = new() { "Sin asignar", "Engorde", "Madres", "Padrillos" };

        public ClassifyBatchViewModel(FileStorageService storageService)
        {
            _storageService = storageService;
            Title = "Clasificar Lote";
        }

        partial void OnBatchChanged(Batch? value)
        {
            if (value != null)
            {
                _ = LoadAnimalsAsync();
            }
        }

        public async Task LoadAnimalsAsync()
        {
            if (Batch == null) return;

            IsBusy = true;
            try
            {
                var list = await _storageService.GetAnimalsForBatchAsync(Batch.Id, Batch.Quantity, Batch.CurrentWeight);
                
                _isSyncing = true;
                Animals.Clear();
                foreach (var animal in list)
                {
                    var item = new AnimalClassificationItem { Animal = animal };
                    item.PropertyChanged += OnAnimalItemPropertyChanged;
                    Animals.Add(item);
                }
                _isSyncing = false;

                // Initial count recalculation
                _engordeCount = Animals.Count(a => a.SelectedPurpose == "Engorde");
                _madresCount = Animals.Count(a => a.SelectedPurpose == "Madres");
                _padrillosCount = Animals.Count(a => a.SelectedPurpose == "Padrillos");
                OnPropertyChanged(nameof(EngordeCount));
                OnPropertyChanged(nameof(MadresCount));
                OnPropertyChanged(nameof(PadrillosCount));

                RecalculateStatsOnly();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando animales para clasificar: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnAnimalItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (e.PropertyName == nameof(AnimalClassificationItem.SelectedPurpose))
            {
                _isSyncing = true;
                try
                {
                    _engordeCount = Animals.Count(a => a.SelectedPurpose == "Engorde");
                    _madresCount = Animals.Count(a => a.SelectedPurpose == "Madres");
                    _padrillosCount = Animals.Count(a => a.SelectedPurpose == "Padrillos");

                    OnPropertyChanged(nameof(EngordeCount));
                    OnPropertyChanged(nameof(MadresCount));
                    OnPropertyChanged(nameof(PadrillosCount));

                    RecalculateStatsOnly();
                }
                finally
                {
                    _isSyncing = false;
                }
            }
        }

        private void OnCountEdited()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                int total = Animals.Count;
                int engCount = Math.Max(0, _engordeCount);
                int madCount = Math.Max(0, _madresCount);
                int padCount = Math.Max(0, _padrillosCount);

                // Capped at total count
                if (engCount > total) engCount = total;
                if (engCount + madCount > total) madCount = total - engCount;
                if (engCount + madCount + padCount > total) padCount = total - engCount - madCount;

                _engordeCount = engCount;
                _madresCount = madCount;
                _padrillosCount = padCount;

                OnPropertyChanged(nameof(EngordeCount));
                OnPropertyChanged(nameof(MadresCount));
                OnPropertyChanged(nameof(PadrillosCount));

                // Sequential distribution
                for (int i = 0; i < total; i++)
                {
                    if (i < engCount)
                    {
                        Animals[i].SelectedPurpose = "Engorde";
                    }
                    else if (i < engCount + madCount)
                    {
                        Animals[i].SelectedPurpose = "Madres";
                    }
                    else if (i < engCount + madCount + padCount)
                    {
                        Animals[i].SelectedPurpose = "Padrillos";
                    }
                    else
                    {
                        Animals[i].SelectedPurpose = string.Empty;
                    }
                }

                RecalculateStatsOnly();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void RecalculateStatsOnly()
        {
            TotalCount = Animals.Count;
            AssignedCount = _engordeCount + _madresCount + _padrillosCount;
            UnassignedCount = TotalCount - AssignedCount;

            if (TotalCount > 0)
            {
                EngordePercentageText = $"{Math.Round((double)_engordeCount / TotalCount * 100)}%";
                MadresPercentageText = $"{Math.Round((double)_madresCount / TotalCount * 100)}%";
                PadrillosPercentageText = $"{Math.Round((double)_padrillosCount / TotalCount * 100)}%";
            }
            else
            {
                EngordePercentageText = "0%";
                MadresPercentageText = "0%";
                PadrillosPercentageText = "0%";
            }

            IsConfirmEnabled = UnassignedCount == 0 && TotalCount > 0;
            ConfirmButtonText = IsConfirmEnabled ? "Confirmar Clasificación" : $"Faltan {UnassignedCount} animales";
        }

        [RelayCommand]
        public async Task ConfirmClassificationAsync()
        {
            if (Batch == null || !IsConfirmEnabled) return;

            IsBusy = true;
            try
            {
                var grouped = Animals.GroupBy(a => a.SelectedPurpose).ToList();

                foreach (var group in grouped)
                {
                    string purpose = group.Key;
                    if (string.IsNullOrEmpty(purpose)) continue;

                    var groupAnimals = group.Select(a => a.Animal).ToList();
                    if (groupAnimals.Count == 0) continue;

                    string domainPurpose = purpose switch
                    {
                        "Engorde" => "Engorde",
                        "Madres" => "Reproducción",
                        "Padrillos" => "Padrillos",
                        _ => "General"
                    };

                    var newBatch = new Batch
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"{Batch.Name} - {purpose}",
                        CategoryId = Batch.CategoryId,
                        CategoryName = Batch.CategoryName,
                        Breed = Batch.Breed,
                        InitialQuantity = groupAnimals.Count,
                        Quantity = groupAnimals.Count,
                        MortalityCount = 0,
                        BirthDate = Batch.BirthDate,
                        Gender = purpose == "Madres" ? "Hembra" : (purpose == "Padrillos" ? "Macho" : "Mixto"),
                        InitialWeight = groupAnimals.Average(a => a.Weight),
                        CurrentWeight = groupAnimals.Average(a => a.Weight),
                        SanitaryStatus = "Excelente",
                        Purpose = domainPurpose,
                        Notes = $"Creado por clasificación del lote original {Batch.Name} (ID: {Batch.Id}).",
                        QRCode = $"GALPONAPP-LOTE-{Guid.NewGuid().ToString()[..8]}",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    await _storageService.SaveBatchAsync(newBatch);

                    // Generate specific vaccinations based on purpose
                    await _storageService.AutoGenerateVaccinationsAsync(newBatch);

                    foreach (var animal in groupAnimals)
                    {
                        animal.BatchId = newBatch.Id;
                        await _storageService.SaveAnimalAsync(animal);
                    }
                }

                Batch.IsActive = false;
                Batch.IsDivided = true;
                await _storageService.SaveBatchAsync(Batch);

                await Shell.Current.DisplayAlert("Clasificación Completada", "El lote ha sido dividido con éxito en nuevos lotes de propósito específico.", "Aceptar");
                await Shell.Current.GoToAsync("../..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"No se pudo completar la clasificación: {ex.Message}", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void SetEngorde(AnimalClassificationItem item)
        {
            if (item == null) return;
            item.SelectedPurpose = item.SelectedPurpose == "Engorde" ? string.Empty : "Engorde";
        }

        [RelayCommand]
        public void SetMadres(AnimalClassificationItem item)
        {
            if (item == null) return;
            item.SelectedPurpose = item.SelectedPurpose == "Madres" ? string.Empty : "Madres";
        }

        [RelayCommand]
        public void SetPadrillos(AnimalClassificationItem item)
        {
            if (item == null) return;
            item.SelectedPurpose = item.SelectedPurpose == "Padrillos" ? string.Empty : "Padrillos";
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
