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

namespace GalponApp.Presentation.ViewModels
{
    public partial class AddBatchViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private readonly QRCodeService _qrCodeService;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private AnimalCategory? selectedCategory;

        [ObservableProperty]
        private string selectedBreed = string.Empty;

        [ObservableProperty]
        private string quantityString = "1";

        [ObservableProperty]
        private DateTime birthDate = DateTime.Today;

        [ObservableProperty]
        private string selectedGender = "Mixto";

        [ObservableProperty]
        private string initialWeightString = "0";

        [ObservableProperty]
        private string selectedSanitaryStatus = "Excelente";

        [ObservableProperty]
        private string selectedPurpose = string.Empty;

        [ObservableProperty]
        private string notes = string.Empty;

        public ObservableCollection<AnimalCategory> Categories { get; } = new();
        public ObservableCollection<string> Breeds { get; } = new();
        public ObservableCollection<string> Purposes { get; } = new();
        public List<string> Genders { get; } = new() { "Macho", "Hembra", "Mixto" };
        public List<string> SanitaryStatuses { get; } = new() { "Excelente", "Regular", "Enfermo", "Aislado" };

        public AddBatchViewModel(FileStorageService storageService, QRCodeService qrCodeService)
        {
            _storageService = storageService;
            _qrCodeService = qrCodeService;
            Title = "Registrar Lote";
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var cats = await _storageService.GetCategoriesAsync();
                Categories.Clear();
                foreach (var c in cats)
                {
                    Categories.Add(c);
                }

                if (Categories.Count > 0)
                {
                    SelectedCategory = Categories.First();
                    UpdateBreedsAndPurposes();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando datos de formulario: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Se ejecuta al cambiar la categoría seleccionada
        public void UpdateBreedsAndPurposes()
        {
            Breeds.Clear();
            Purposes.Clear();

            if (SelectedCategory != null)
            {
                foreach (var b in SelectedCategory.Breeds)
                {
                    Breeds.Add(b);
                }
                SelectedBreed = Breeds.FirstOrDefault() ?? string.Empty;

                foreach (var p in SelectedCategory.Purposes)
                {
                    Purposes.Add(p);
                }
                SelectedPurpose = Purposes.FirstOrDefault() ?? string.Empty;
            }
        }

        [RelayCommand]
        public async Task SaveBatchAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlertAsync("Error", "Por favor ingresa un nombre para el lote.", "Aceptar");
                return;
            }

            if (SelectedCategory == null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Por favor selecciona una categoría.", "Aceptar");
                return;
            }

            if (!int.TryParse(QuantityString, out int qty) || qty <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Ingresa una cantidad válida de animales (mayor a 0).", "Aceptar");
                return;
            }

            if (!double.TryParse(InitialWeightString, out double initWeight) || initWeight < 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Ingresa un peso inicial válido.", "Aceptar");
                return;
            }

            IsBusy = true;

            try
            {
                string newId = Guid.NewGuid().ToString();
                string qrCode = _qrCodeService.GeneratePayload(newId);

                // 1. Crear el lote
                var batch = new Batch
                {
                    Id = newId,
                    Name = Name,
                    CategoryId = SelectedCategory.Id,
                    CategoryName = SelectedCategory.Name,
                    Breed = SelectedBreed,
                    Quantity = qty,
                    InitialQuantity = qty,
                    MortalityCount = 0,
                    BirthDate = BirthDate,
                    Gender = SelectedGender,
                    InitialWeight = initWeight,
                    CurrentWeight = initWeight, // Al inicio, el actual es el inicial
                    SanitaryStatus = SelectedSanitaryStatus,
                    Purpose = SelectedPurpose,
                    Notes = Notes,
                    QRCode = qrCode,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                // Guardar Lote
                await _storageService.SaveBatchAsync(batch);

                // 2. Generar primer registro de peso (Fecha = Nacimiento o Entrada)
                var weightLog = new WeightLog
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = newId,
                    Date = BirthDate.Date > DateTime.Today ? DateTime.Today : BirthDate,
                    AverageWeight = initWeight,
                    AverageSize = 0,
                    MortalityCount = 0,
                    Notes = "Registro automático de peso inicial al ingresar el lote."
                };
                await _storageService.SaveWeightLogAsync(weightLog);

                // 3. Generar Calendario Sanitario / Vacunación Automático
                await AutoGenerateVaccinationsAsync(batch);

                await Shell.Current.DisplayAlertAsync("Lote Registrado", $"El lote '{Name}' fue creado con éxito y su calendario de vacunación fue autogenerado.", "Aceptar");
                
                // Volver a la lista
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"No se pudo guardar el lote: {ex.Message}", "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AutoGenerateVaccinationsAsync(Batch batch)
        {
            await _storageService.AutoGenerateVaccinationsAsync(batch);
        }
    }
}
