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

        // Genera vacunas automáticas basadas en la categoría, propósito y fecha de nacimiento
        private async Task AutoGenerateVaccinationsAsync(Batch batch)
        {
            var vacs = new List<Vaccination>();
            DateTime birth = batch.BirthDate;
            string purpose = batch.Purpose.Trim();

            switch (batch.CategoryId)
            {
                case "porcinos":
                    if (purpose.Equals("Reproducción", StringComparison.OrdinalIgnoreCase) || purpose.Equals("Padrillos", StringComparison.OrdinalIgnoreCase))
                    {
                        // Esquema reproductivo porcino
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Circovirus Porcino (PCV2)", Type = "Vacuna",
                            Description = "Prevención del síndrome de desmedro multisistémico porcino. Vía Intramuscular (IM) en la tabla del cuello.", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                            Alternatives = "Circoflex (Boehringer) / Suvaxyn PCV2 (Zoetis)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Mycoplasma hyopneumoniae", Type = "Vacuna",
                            Description = "Prevención de la neumonía enzoótica (Dosis 1). Vía Intramuscular (IM) en la tabla del cuello.", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                            Alternatives = "RespiSure (Zoetis) / M+Pac (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Refuerzo contra Mycoplasma hyopneumoniae", Type = "Refuerzo",
                            Description = "Segunda dosis para inmunidad protectora pulmonar. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(42), Status = "Pendiente",
                            Alternatives = "RespiSure (Zoetis) / M+Pac (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra la Peste Porcina Clásica (Cólera Porcino)", Type = "Vacuna",
                            Description = "Inmunización obligatoria oficial contra el Cólera Porcino (Cepa China). Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                            Alternatives = "Pest-Vac (Lab local) / Cólera Porcino (Bayer)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Triple Reproductiva Porcina Dosis 1 (PLE)", Type = "Vacuna",
                            Description = "Protección contra Parvovirus, Erisipela y Leptospirosis en futuros reproductores. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(150), Status = "Pendiente",
                            Alternatives = "FarrowSure Gold (Zoetis) / Porcilis Ery+Parvo (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Triple Reproductiva Porcina Dosis 2 (PLE)", Type = "Refuerzo",
                            Description = "Refuerzo pre-servicio para inmunidad sólida en gestación. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(171), Status = "Pendiente",
                            Alternatives = "FarrowSure Gold (Zoetis) / Porcilis Ery+Parvo (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Desparasitante (Ivermectina 1%)", Type = "Desparasitación",
                            Description = "Control de sarna y parásitos antes de la monta/servicio. Vía Subcutánea (SC).", Dose = 1.0, DoseUnit = "ml/33kg",
                            ScheduledDate = birth.AddDays(180), Status = "Pendiente",
                            Alternatives = "Dectomax (Zoetis) / Ivomec (Boehringer)"
                        });
                    }
                    else if (purpose.Equals("Lechones", StringComparison.OrdinalIgnoreCase))
                    {
                        // Esquema especial para lechones en destete temprano o cría
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Hierro Dextrano Fortificado", Type = "Vitamina",
                            Description = "Prevención de anemia ferropénica en lechones neonatos. Vía Intramuscular (IM) profunda en el muslo.", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(3), Status = "Pendiente",
                            Alternatives = "Gleptosil (Alanco) / Iron-Dex (Zoetis)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Circovirus Porcino (PCV2)", Type = "Vacuna",
                            Description = "Protección contra desmedro. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                            Alternatives = "Circoflex (Boehringer) / Suvaxyn PCV2 (Zoetis)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Mycoplasma hyopneumoniae", Type = "Vacuna",
                            Description = "Prevención de neumonía enzoótica. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                            Alternatives = "RespiSure (Zoetis) / M+Pac (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra la Peste Porcina Clásica (Cólera Porcino)", Type = "Vacuna",
                            Description = "Vacunación obligatoria oficial de lechones. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                            Alternatives = "Pest-Vac (Lab local) / Cólera Porcino (Bayer)"
                        });
                    }
                    else
                    {
                        // Esquema comercial porcino estándar (Engorde)
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Circovirus Porcino (PCV2)", Type = "Vacuna",
                            Description = "Prevención del síndrome de desmedro multisistémico porcino. Vía Intramuscular (IM) en la tabla del cuello.", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                            Alternatives = "Circoflex (Boehringer) / Suvaxyn PCV2 (Zoetis)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Mycoplasma hyopneumoniae", Type = "Vacuna",
                            Description = "Prevención de la neumonía enzoótica (Dosis 1). Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                            Alternatives = "RespiSure (Zoetis) / M+Pac (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Refuerzo contra Mycoplasma hyopneumoniae", Type = "Refuerzo",
                            Description = "Segunda dosis para inmunidad protectora pulmonar. Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(42), Status = "Pendiente",
                            Alternatives = "RespiSure (Zoetis) / M+Pac (MSD)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra la Peste Porcina Clásica (Cólera Porcino)", Type = "Vacuna",
                            Description = "Inmunización obligatoria oficial contra el Cólera Porcino (Cepa China). Vía Intramuscular (IM).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                            Alternatives = "Pest-Vac (Lab local) / Cólera Porcino (Bayer)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Desparasitante (Ivermectina 1%)", Type = "Desparasitación",
                            Description = "Control de parásitos gastrointestinales, pulmonares, sarna y piojos. Vía Subcutánea (SC).", Dose = 1.0, DoseUnit = "ml/33kg",
                            ScheduledDate = birth.AddDays(60), Status = "Pendiente",
                            Alternatives = "Dectomax (Zoetis) / Ivomec (Boehringer)"
                        });
                    }
                    break;

                case "avicolas_engorde":
                    // Esquema broiler de engorde acelerado
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Marek (HVT)", Type = "Vacuna",
                        Description = "Prevención de la enfermedad de Marek. Vía Subcutánea (SC) en la nuca (usualmente en planta de incubación).", Dose = 0.2, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(1), Status = "Pendiente",
                        Alternatives = "Nobilis Rismavac (MSD) / Poulvac Marek (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Newcastle + Bronquitis Infecciosa", Type = "Vacuna",
                        Description = "Prevención de Newcastle y Bronquitis (Cepa Ma5). Vía Gota Ocular o Nasal.", Dose = 0.03, DoseUnit = "ml (1 gota)",
                        ScheduledDate = birth.AddDays(7), Status = "Pendiente",
                        Alternatives = "Nobilis Ma5+Clone 30 (MSD) / Poulvac ND-IB (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Gumboro", Type = "Vacuna",
                        Description = "Prevención de la enfermedad de la Bolsa de Fabricio. Administrada en agua de bebida (sin cloro).", Dose = 1.0, DoseUnit = "dosis/ave",
                        ScheduledDate = birth.AddDays(12), Status = "Pendiente",
                        Alternatives = "Nobilis Gumboro D78 (MSD) / Poulvac Gumboro (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Refuerzo Newcastle + Bronquitis Infecciosa", Type = "Refuerzo",
                        Description = "Refuerzo inmunológico en agua de bebida o atomización.", Dose = 1.0, DoseUnit = "dosis/ave",
                        ScheduledDate = birth.AddDays(21), Status = "Pendiente",
                        Alternatives = "Nobilis Ma5+Clone 30 (MSD) / Poulvac ND-IB (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra la Viruela Aviar", Type = "Vacuna",
                        Description = "Prevención de Viruela. Vía Punción en la membrana del ala (doble aguja).", Dose = 1.0, DoseUnit = "dosis/ave",
                        ScheduledDate = birth.AddDays(28), Status = "Pendiente",
                        Alternatives = "Poulvac Fowl Pox (Zoetis) / Nobilis Vario (MSD)"
                    });
                    break;

                case "avicolas_postura":
                    // Esquema pollitas ponedoras/reproductoras de ciclo largo
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Marek (HVT)", Type = "Vacuna",
                        Description = "Protección inicial. Vía Subcutánea (SC) en la nuca al primer día.", Dose = 0.2, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(1), Status = "Pendiente",
                        Alternatives = "Nobilis Rismavac (MSD) / Poulvac Marek (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Newcastle + Bronquitis", Type = "Vacuna",
                        Description = "Dosis inicial protectora ocular. Cepa LaSota + Mass.", Dose = 0.03, DoseUnit = "ml (1 gota)",
                        ScheduledDate = birth.AddDays(7), Status = "Pendiente",
                        Alternatives = "Nobilis Ma5+Clone 30 (MSD) / Poulvac ND-IB (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Gumboro", Type = "Vacuna",
                        Description = "Primera dosis protectora humoral. Vía Agua de bebida sin cloro.", Dose = 1.0, DoseUnit = "dosis/ave",
                        ScheduledDate = birth.AddDays(14), Status = "Pendiente",
                        Alternatives = "Nobilis Gumboro D78 (MSD) / Poulvac Gumboro (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Refuerzo contra Newcastle + Bronquitis", Type = "Refuerzo",
                        Description = "Segunda inmunización para protección respiratoria. Vía Agua de bebida.", Dose = 1.0, DoseUnit = "dosis/ave",
                        ScheduledDate = birth.AddDays(28), Status = "Pendiente",
                        Alternatives = "Nobilis Ma5+Clone 30 (MSD) / Poulvac ND-IB (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Viruela Aviar", Type = "Vacuna",
                        Description = "Prevención de la Viruela Aviar. Vía Punción en el ala (doble aguja).", Dose = 1.0, DoseUnit = "dosis/ave",
                        ScheduledDate = birth.AddDays(70), Status = "Pendiente",
                        Alternatives = "Poulvac Fowl Pox (Zoetis) / Nobilis Vario (MSD)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra Coriza Infecciosa Dosis 1", Type = "Vacuna",
                        Description = "Prevención de Coriza aviar (moquillo). Vacuna inactivada oleosa. Vía Intramuscular (IM) en la pechuga.", Dose = 0.5, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(84), Status = "Pendiente",
                        Alternatives = "Corymune (Ceva) / Nobilis Corvac (MSD)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Refuerzo contra Coriza Infecciosa Dosis 2", Type = "Refuerzo",
                        Description = "Segunda inmunización requerida para fase de postura. Vía Intramuscular (IM) en la pechuga.", Dose = 0.5, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(112), Status = "Pendiente",
                        Alternatives = "Corymune (Ceva) / Nobilis Corvac (MSD)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Multivitamínico AD3E + Calcio", Type = "Vitamina",
                        Description = "Preparación fisiológica del tracto reproductivo antes de iniciar postura.", Dose = 2.0, DoseUnit = "g/L de agua",
                        ScheduledDate = birth.AddDays(126), Status = "Pendiente",
                        Alternatives = "Vitapoli (Lab local) / Promotor-L (Calier)"
                    });
                    break;
 
                case "bovinos_leche":
                    // Esquema lechero especializado
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra la Fiebre Aftosa", Type = "Vacuna",
                        Description = "Control nacional oficial obligatorio (Campaña). Vía Subcutánea (SC) en la tabla del cuello.", Dose = 2.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(15), Status = "Pendiente",
                        Alternatives = "Aftogen (Biogénesis) / Aftosan (Lab Oficial)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna Triple Clostridial", Type = "Vacuna",
                        Description = "Protección contra Carbón Sintomático, Edema Maligno y Septicemia Clostridial. Vía Subcutánea (SC) en la tabla del cuello.", Dose = 5.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(90), Status = "Pendiente",
                        Alternatives = "Covexin 8 (Zoetis) / Tasvax (Coopers)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Refuerzo Triple Clostridial", Type = "Refuerzo",
                        Description = "Inmunización de memoria de larga duración. Vía Subcutánea (SC) en la tabla del cuello.", Dose = 5.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(120), Status = "Pendiente",
                        Alternatives = "Covexin 8 (Zoetis) / Tasvax (Coopers)"
                    });
                    if (batch.Gender == "Hembra")
                    {
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Brucelosis Bovina (Cepa 19)", Type = "Vacuna",
                            Description = "Prevención del aborto contagioso. Aplicar obligatoriamente solo a terneras hembras de 3 a 8 meses. Vía Subcutánea (SC).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(150), Status = "Pendiente",
                            Alternatives = "Brucelosis Cepa 19 (Lab Oficial) / RB51 (Zoetis)"
                        });
                    }
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Complejo Respiratorio y Reproductivo Bovino (IBR/DVB/Leptospira)", Type = "Vacuna",
                        Description = "Protección contra virus reproductivos y Leptospira para evitar abortos. Vía Subcutánea (SC) o Intramuscular (IM).", Dose = 5.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(180), Status = "Pendiente",
                        Alternatives = "Bovi-Shield Gold FP5 (Zoetis) / CattleMaster Gold (Zoetis)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna preventiva contra Mastitis (Startvac)", Type = "Vacuna",
                        Description = "Inmunización contra S. aureus y coliformes productores de mastitis. Vía Intramuscular (IM) profunda en la tabla del cuello.", Dose = 2.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(240), Status = "Pendiente",
                        Alternatives = "Startvac (Hipra) / Mastivac (Lab local)"
                    });
                    break;
 
                case "bovinos_carne":
                    // Esquema para bovinos de carne
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna contra la Fiebre Aftosa", Type = "Vacuna",
                        Description = "Control nacional oficial obligatorio (Campaña). Vía Subcutánea (SC) en la tabla del cuello.", Dose = 2.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(15), Status = "Pendiente",
                        Alternatives = "Aftogen (Biogénesis) / Aftosan (Lab Oficial)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna Triple Clostridial", Type = "Vacuna",
                        Description = "Protección contra Carbón Sintomático, Edema Maligno y Septicemia Clostridial (Dosis 1). Vía Subcutánea (SC) en la tabla del cuello.", Dose = 5.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(90), Status = "Pendiente",
                        Alternatives = "Covexin 8 (Zoetis) / Tasvax (Coopers)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Refuerzo Triple Clostridial", Type = "Refuerzo",
                        Description = "Inmunización de memoria contra muerte súbita. Vía Subcutánea (SC) en la tabla del cuello.", Dose = 5.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(120), Status = "Pendiente",
                        Alternatives = "Covexin 8 (Zoetis) / Tasvax (Coopers)"
                    });
                    if (batch.Gender == "Hembra")
                    {
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Brucelosis Bovina (Cepa 19)", Type = "Vacuna",
                            Description = "Prevención del aborto contagioso. Aplicar a terneras hembras de 3 a 8 meses. Vía Subcutánea (SC).", Dose = 2.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(150), Status = "Pendiente",
                            Alternatives = "Brucelosis Cepa 19 (Lab Oficial) / RB51 (Zoetis)"
                        });
                    }
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Desparasitante + Vitamínico ADE inyectable", Type = "Desparasitación",
                        Description = "Desparasitación antiparasitaria (Ivermectina) y soporte de vitaminas al destete. Vía Subcutánea (SC).", Dose = 5.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(180), Status = "Pendiente",
                        Alternatives = "Dectomax (Zoetis) / Ivomec (Boehringer)"
                    });
                    if (purpose.Equals("Reproducción", StringComparison.OrdinalIgnoreCase))
                    {
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Complejo Respiratorio y Reproductivo Bovino (IBR/DVB/Leptospira)", Type = "Vacuna",
                            Description = "Protección de vacas de cría contra abortos de origen viral y Leptospira. Vía Subcutánea (SC).", Dose = 5.0, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(210), Status = "Pendiente",
                            Alternatives = "Bovi-Shield Gold FP5 (Zoetis) / CattleMaster Gold (Zoetis)"
                        });
                    }
                    break;

                case "ovinos":
                case "caprinos":
                    // Esquema pequeños rumiantes
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna Anticlostridial (Tétanos, Enterotoxemia)", Type = "Vacuna",
                        Description = "Prevención de enterotoxemia, gangrena y tétanos (Dosis 1). Vía Subcutánea (SC) en la axila o ingle.", Dose = 2.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(30), Status = "Pendiente",
                        Alternatives = "Covexin 8 (Zoetis) / Tasvax (Coopers)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Desparasitante (Fenbendazol u Oral)", Type = "Desparasitación",
                        Description = "Control de parásitos gastrointestinales y nematodos. Vía Oral.", Dose = 1.0, DoseUnit = "ml/10kg",
                        ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                        Alternatives = "Panacur (MSD) / Vermisil (Lab local)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Refuerzo Anticlostridial", Type = "Refuerzo",
                        Description = "Refuerzo inmunológico contra clostridiosis. Vía Subcutánea (SC).", Dose = 2.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(60), Status = "Pendiente",
                        Alternatives = "Covexin 8 (Zoetis) / Tasvax (Coopers)"
                    });
                    if (batch.CategoryId == "caprinos")
                    {
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Ectima Contagioso", Type = "Vacuna",
                            Description = "Prevención del boqueras o ectima viral. Vía Escarificación cutánea local (muslo interno).", Dose = 1.0, DoseUnit = "dosis/animal",
                            ScheduledDate = birth.AddDays(75), Status = "Pendiente",
                            Alternatives = "Ectivax (Lab local) / Ectima Prevent (Veterinaria)"
                        });
                    }
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vitamina AD3E inyectable", Type = "Vitamina",
                        Description = "Soporte vitamínico de desarrollo y fortalecimiento inmune. Vía Intramuscular (IM).", Dose = 1.5, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(90), Status = "Pendiente",
                        Alternatives = "Vigantol (Bayer) / Ganavet ADE (Lab local)"
                    });
                    break;
 
                case "cunicultura":
                    if (purpose.Equals("Reproducción", StringComparison.OrdinalIgnoreCase))
                    {
                        // Conejos reproductores de ciclo largo
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Desparasitante al Destete (Oral)", Type = "Desparasitación",
                            Description = "Control parasitario de coccidiosis y nematodos gastrointestinales. Vía Oral.", Dose = 0.5, DoseUnit = "ml/conejo",
                            ScheduledDate = birth.AddDays(30), Status = "Pendiente",
                            Alternatives = "Panacur Oral (MSD) / Coccidiol (Veterinaria)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Mixomatosis", Type = "Vacuna",
                            Description = "Prevención de Mixomatosis viral. Inmunidad básica. Vía Subcutánea (SC).", Dose = 0.5, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                            Alternatives = "Mixovac (Lab local) / Dercunimix (Veterinaria)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra la Enfermedad Hemorrágica Viral (EHV)", Type = "Vacuna",
                            Description = "Prevención de neumonía hemorrágica del conejo. Vía Subcutánea (SC).", Dose = 0.5, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(60), Status = "Pendiente",
                            Alternatives = "Cunipravac RHD (Hipra) / RHDV2 (Lab Oficial)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Refuerzo semestral Mixomatosis", Type = "Refuerzo",
                            Description = "Revacunación semestral obligatoria para reproductoras. Vía Subcutánea (SC).", Dose = 0.5, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(180), Status = "Pendiente",
                            Alternatives = "Mixovac (Lab local) / Dercunimix (Veterinaria)"
                        });
                    }
                    else
                    {
                        // Conejos de engorde/carne (ciclo corto)
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Desparasitante al Destete (Oral)", Type = "Desparasitación",
                            Description = "Control parasitario al destete. Vía Oral.", Dose = 0.5, DoseUnit = "ml/conejo",
                            ScheduledDate = birth.AddDays(30), Status = "Pendiente",
                            Alternatives = "Panacur Oral (MSD) / Coccidiol (Veterinaria)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra Mixomatosis", Type = "Vacuna",
                            Description = "Prevención de Mixomatosis viral. Vía Subcutánea (SC).", Dose = 0.5, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                            Alternatives = "Mixovac (Lab local) / Dercunimix (Veterinaria)"
                        });
                        vacs.Add(new() {
                            Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                            Name = "Vacuna contra la Enfermedad Hemorrágica Viral (EHV)", Type = "Vacuna",
                            Description = "Prevención de neumonía hemorrágica. Vía Subcutánea (SC).", Dose = 0.5, DoseUnit = "ml",
                            ScheduledDate = birth.AddDays(60), Status = "Pendiente",
                            Alternatives = "Cunipravac RHD (Hipra) / RHDV2 (Lab Oficial)"
                        });
                    }
                    break;
 
                default:
                    // Genérico para otras especies
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Desparasitante de Amplio Espectro", Type = "Desparasitación",
                        Description = "Tratamiento preventivo básico contra parásitos internos. Vía Oral.", Dose = 1.0, DoseUnit = "ml/10kg",
                        ScheduledDate = birth.AddDays(30), Status = "Pendiente",
                        Alternatives = "Panacur (MSD) / Albendazol local"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Multivitamínico Fortificante", Type = "Vitamina",
                        Description = "Refuerzo del sistema inmune durante el crecimiento. Vía Oral en agua.", Dose = 1.0, DoseUnit = "ml/L",
                        ScheduledDate = birth.AddDays(45), Status = "Pendiente",
                        Alternatives = "Promotor-L (Calier) / Vitapoli (Lab local)"
                    });
                    vacs.Add(new() {
                        Id = Guid.NewGuid().ToString(), BatchId = batch.Id, BatchName = batch.Name,
                        Name = "Vacuna Anticlostridial Genérica", Type = "Vacuna",
                        Description = "Prevención contra infecciones clostridiales. Vía Subcutánea.", Dose = 2.0, DoseUnit = "ml",
                        ScheduledDate = birth.AddDays(60), Status = "Pendiente",
                        Alternatives = "Covexin (Zoetis) / Tasvax (Coopers)"
                    });
                    break;
            }

            foreach (var v in vacs)
            {
                await _storageService.SaveVaccinationAsync(v);
            }
        }
    }
}
