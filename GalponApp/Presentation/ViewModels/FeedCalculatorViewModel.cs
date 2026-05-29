using System;
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
    public partial class FeedCalculatorViewModel : BaseViewModel
    {
        private readonly FileStorageService _storageService;
        private readonly FeedingCalculator _feedingCalculator;

        [ObservableProperty]
        private Batch? selectedBatch;

        // Parámetros de simulación
        [ObservableProperty]
        private string costPerAnimalString = "45";

        [ObservableProperty]
        private string feedCostPerKgString = "1.2";

        [ObservableProperty]
        private string otherCostsString = "150";

        [ObservableProperty]
        private string salePriceString = "3.8"; // Precio de venta por kg o animal

        [ObservableProperty]
        private bool saleIsPerKg = true; // Si es true se vende por kilo, si es false por cabeza

        [ObservableProperty]
        private string targetWeightString = "105"; // Peso de venta objetivo (kg)

        [ObservableProperty]
        private string targetAgeWeeksString = "24"; // Edad objetivo (semanas)

        // Resultados
        [ObservableProperty]
        private bool hasResult;

        [ObservableProperty]
        private double resultTotalFeedKg;

        [ObservableProperty]
        private double resultTotalFeedCost;

        [ObservableProperty]
        private double resultTotalCost;

        [ObservableProperty]
        private double resultRevenue;

        [ObservableProperty]
        private double resultProfit;

        [ObservableProperty]
        private double resultROI;

        [ObservableProperty]
        private double resultFCR;

        public ObservableCollection<Batch> Batches { get; } = new();

        public FeedCalculatorViewModel(FileStorageService storageService, FeedingCalculator feedingCalculator)
        {
            _storageService = storageService;
            _feedingCalculator = feedingCalculator;
            Title = "Calculadora Alimenticia";
        }

        [RelayCommand]
        public async Task LoadBatchesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var list = await _storageService.GetBatchesAsync();
                Batches.Clear();
                foreach (var b in list.Where(b => b.IsActive))
                {
                    Batches.Add(b);
                }

                if (SelectedBatch == null && Batches.Count > 0)
                {
                    SelectedBatch = Batches.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar lotes en calculadora: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedBatchChanged(Batch? value)
        {
            if (value != null)
            {
                // Auto-completar defaults inteligentes según la especie elegida
                HasResult = false;
                switch (value.CategoryId)
                {
                    case "porcinos":
                        CostPerAnimalString = "45";
                        FeedCostPerKgString = "1.2";
                        OtherCostsString = "150";
                        SalePriceString = "3.8"; // precio por kg vivo
                        SaleIsPerKg = true;
                        TargetWeightString = "105";
                        TargetAgeWeeksString = "24";
                        break;
                    case "avicolas_engorde":
                        CostPerAnimalString = "0.8";
                        FeedCostPerKgString = "0.9";
                        OtherCostsString = "80";
                        SalePriceString = "2.5"; // precio por kg pollo
                        SaleIsPerKg = true;
                        TargetWeightString = "2.8";
                        TargetAgeWeeksString = "7";
                        break;
                    case "avicolas_postura":
                        CostPerAnimalString = "1.5";
                        FeedCostPerKgString = "0.95";
                        OtherCostsString = "120";
                        SalePriceString = "12"; // precio por cubeta de huevo
                        SaleIsPerKg = false; // venta por producto entero/unidad
                        TargetWeightString = "1.8";
                        TargetAgeWeeksString = "72";
                        break;
                    case "bovinos_leche":
                        CostPerAnimalString = "500";
                        FeedCostPerKgString = "1.4";
                        OtherCostsString = "400";
                        SalePriceString = "0.8"; // venta litro leche o animal
                        SaleIsPerKg = false;
                        TargetWeightString = "550";
                        TargetAgeWeeksString = "180";
                        break;
                    default:
                        CostPerAnimalString = "60";
                        FeedCostPerKgString = "1.1";
                        OtherCostsString = "100";
                        SalePriceString = "120";
                        SaleIsPerKg = false;
                        TargetWeightString = "45";
                        TargetAgeWeeksString = "26";
                        break;
                }
            }
        }

        [RelayCommand]
        public async Task CalculateAsync()
        {
            if (SelectedBatch == null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Por favor selecciona un lote activo para calcular.", "Aceptar");
                return;
            }

            if (!double.TryParse(CostPerAnimalString, out double costPerAnimal) || costPerAnimal < 0) return;
            if (!double.TryParse(FeedCostPerKgString, out double feedCost) || feedCost < 0) return;
            if (!double.TryParse(OtherCostsString, out double other) || other < 0) return;
            if (!double.TryParse(SalePriceString, out double price) || price < 0) return;
            if (!double.TryParse(TargetWeightString, out double targetWeight) || targetWeight <= 0) return;
            if (!int.TryParse(TargetAgeWeeksString, out int targetAge) || targetAge <= 0) return;

            IsBusy = true;

            try
            {
                var allConfigs = await _storageService.GetFeedingConfigsAsync();
                
                var projection = _feedingCalculator.EstimateProfitability(
                    SelectedBatch,
                    allConfigs,
                    costPerAnimal,
                    feedCost,
                    other,
                    price,
                    SaleIsPerKg,
                    targetWeight,
                    targetAge
                );

                ResultTotalFeedKg = projection.TotalEstimatedFeedNeededKg;
                ResultTotalFeedCost = projection.TotalEstimatedFeedCost;
                ResultTotalCost = projection.TotalProductionCost;
                ResultRevenue = projection.ProjectedRevenue;
                ResultProfit = projection.NetProfit;
                ResultROI = projection.ReturnOnInvestmentPercentage;
                ResultFCR = projection.FeedConversionRatio;
                
                HasResult = true;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error en Cálculo", ex.Message, "Aceptar");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
