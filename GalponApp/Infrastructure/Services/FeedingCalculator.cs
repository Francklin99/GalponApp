using System;
using System.Collections.Generic;
using System.Linq;
using GalponApp.Domain.Models;

namespace GalponApp.Infrastructure.Services
{
    public class FeedingCalculator
    {
        public class FeedingCalculationResult
        {
            public double DailyFeedNeededKg { get; set; }
            public double DailyWaterNeededLiters { get; set; }
            public string RecommendedFeedType { get; set; } = "No configurado";
            public double DailyCost { get; set; }
            public string NutritionalInfo { get; set; } = string.Empty;
            public int FrequencyPerDay { get; set; } = 2;
            public string RecommendedHours { get; set; } = "07:00, 16:00";
            public string Alternatives { get; set; } = string.Empty;
        }

        public class ProfitabilityResult
        {
            public double TotalEstimatedFeedNeededKg { get; set; }
            public double TotalEstimatedFeedCost { get; set; }
            public double TotalInitialCost { get; set; }
            public double TotalProductionCost { get; set; }
            public double ProjectedRevenue { get; set; }
            public double NetProfit { get; set; }
            public double ReturnOnInvestmentPercentage { get; set; }
            public double FeedConversionRatio { get; set; } // FCR (Alimento / Ganancia de peso)
        }

        // Calcula el consumo diario actual de un lote
        public FeedingCalculationResult CalculateCurrentDailyNeeds(Batch batch, FeedingConfig? config, double feedCostPerKg)
        {
            var result = new FeedingCalculationResult();

            if (config != null)
            {
                result.DailyFeedNeededKg = config.DailyAmountPerAnimal * batch.Quantity;
                result.DailyWaterNeededLiters = config.RecommendedWaterLiters * batch.Quantity;
                result.RecommendedFeedType = config.FeedType;
                result.DailyCost = result.DailyFeedNeededKg * feedCostPerKg;
                result.NutritionalInfo = config.NutritionalInfo;
                result.FrequencyPerDay = config.FrequencyPerDay;
                result.RecommendedHours = GetRecommendedHours(config.FrequencyPerDay);
                result.Alternatives = config.Alternatives;
            }
            else
            {
                // Fallbacks básicos empíricos si no hay configuración para la especie y edad
                double dailyFeedPerAnimal = 0.1; // 100g de media genérica
                double dailyWaterPerAnimal = 0.3;
                int frequency = 2;
                string fallbackAlts = "Concentrado Genérico local";

                switch (batch.CategoryId)
                {
                    case "porcinos":
                        dailyFeedPerAnimal = batch.CurrentWeight * 0.04; // ~4% del peso vivo
                        dailyWaterPerAnimal = dailyFeedPerAnimal * 3;
                        result.RecommendedFeedType = "Concentrado Porcino Genérico";
                        frequency = 3;
                        fallbackAlts = "Purina Porcina / Solla Cerdos";
                        break;
                    case "bovinos_leche":
                    case "bovinos_carne":
                        dailyFeedPerAnimal = batch.CurrentWeight * 0.03 + 2.0; // ~3% peso vivo en materia seca
                        dailyWaterPerAnimal = batch.CurrentWeight * 0.1; // ~10% peso vivo
                        result.RecommendedFeedType = "Forraje + Suplemento";
                        frequency = 2;
                        fallbackAlts = "Sales Minerales + Concentrado comercial";
                        break;
                    case "avicolas_engorde":
                    case "avicolas_postura":
                        dailyFeedPerAnimal = 0.11; // 110 gramos
                        dailyWaterPerAnimal = 0.25;
                        result.RecommendedFeedType = "Alimento Balanceado Aves";
                        frequency = 3;
                        fallbackAlts = "Cargill Aves / Purina Engorde";
                        break;
                }

                result.DailyFeedNeededKg = dailyFeedPerAnimal * batch.Quantity;
                result.DailyWaterNeededLiters = dailyWaterPerAnimal * batch.Quantity;
                result.DailyCost = result.DailyFeedNeededKg * feedCostPerKg;
                result.NutritionalInfo = "Estimación empírica (sin tabla formal cargada)";
                result.FrequencyPerDay = frequency;
                result.RecommendedHours = GetRecommendedHours(frequency);
                result.Alternatives = fallbackAlts;
            }

            return result;
        }

        private static string GetRecommendedHours(int frequency)
        {
            return frequency switch
            {
                1 => "08:00",
                2 => "07:00, 16:00",
                3 => "07:00, 12:00, 17:00",
                4 => "06:00, 10:00, 14:00, 18:00",
                5 => "06:00, 09:00, 12:00, 15:00, 18:00",
                _ => "07:00, 16:00"
            };
        }

        // Calcula el alimento proyectado y la rentabilidad desde la edad actual hasta la edad objetivo de venta
        public ProfitabilityResult EstimateProfitability(
            Batch batch, 
            List<FeedingConfig> allConfigs,
            double costPerAnimalPurchase,
            double feedCostPerKg,
            double otherCosts, // Veterinarios, luz, agua, etc.
            double expectedSalePricePerUnit, // Puede ser por kg o por animal entero
            bool saleIsPerKg,
            double targetWeightKg,
            int targetAgeWeeks)
        {
            var result = new ProfitabilityResult();
            
            int currentAgeWeeks = batch.AgeInWeeks;
            int weeksRemaining = Math.Max(0, targetAgeWeeks - currentAgeWeeks);
            
            double totalFeedNeededForOneAnimal = 0;

            // Filtrar configs para esta categoría y propósito
            var categoryConfigs = allConfigs
                .Where(c => c.CategoryId == batch.CategoryId && c.Purpose.Equals(batch.Purpose, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.MinAgeWeeks)
                .ToList();

            // Simular el consumo semana a semana desde la edad actual hasta la edad objetivo
            for (int w = currentAgeWeeks; w < targetAgeWeeks; w++)
            {
                // Encontrar la configuración de alimentación que aplica para esta semana w
                var config = categoryConfigs.FirstOrDefault(c => w >= c.MinAgeWeeks && w <= c.MaxAgeWeeks);
                
                double dailyAmount = 0.1;
                if (config != null)
                {
                    dailyAmount = config.DailyAmountPerAnimal;
                }
                else
                {
                    // Fallback genérico por semana
                    if (batch.CategoryId == "porcinos")
                        dailyAmount = 1.0 + (w * 0.08); // va subiendo
                    else if (batch.CategoryId.StartsWith("avicolas"))
                        dailyAmount = 0.05 + (w * 0.005);
                }

                totalFeedNeededForOneAnimal += dailyAmount * 7; // Consumo en una semana
            }

            result.TotalEstimatedFeedNeededKg = totalFeedNeededForOneAnimal * batch.Quantity;
            result.TotalEstimatedFeedCost = result.TotalEstimatedFeedNeededKg * feedCostPerKg;
            
            result.TotalInitialCost = costPerAnimalPurchase * batch.Quantity;
            result.TotalProductionCost = result.TotalInitialCost + result.TotalEstimatedFeedCost + otherCosts;

            // Calcular ingresos proyectados
            if (saleIsPerKg)
            {
                result.ProjectedRevenue = batch.Quantity * targetWeightKg * expectedSalePricePerUnit;
            }
            else
            {
                result.ProjectedRevenue = batch.Quantity * expectedSalePricePerUnit;
            }

            result.NetProfit = result.ProjectedRevenue - result.TotalProductionCost;
            
            if (result.TotalProductionCost > 0)
            {
                result.ReturnOnInvestmentPercentage = (result.NetProfit / result.TotalProductionCost) * 100;
            }

            // FCR = Peso de alimento total / Ganancia de peso total
            double weightGain = targetWeightKg - batch.InitialWeight;
            if (weightGain > 0 && totalFeedNeededForOneAnimal > 0)
            {
                result.FeedConversionRatio = totalFeedNeededForOneAnimal / weightGain;
            }
            else
            {
                result.FeedConversionRatio = 0;
            }

            return result;
        }
    }
}
