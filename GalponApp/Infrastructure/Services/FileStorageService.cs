using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using GalponApp.Domain.Models;

namespace GalponApp.Infrastructure.Services
{
    public class FileStorageService
    {
        private readonly string _batchesPath = Path.Combine(FileSystem.AppDataDirectory, "batches.json");
        private readonly string _vaccinationsPath = Path.Combine(FileSystem.AppDataDirectory, "vaccinations.json");
        private readonly string _sanitaryPath = Path.Combine(FileSystem.AppDataDirectory, "sanitary.json");
        private readonly string _weightLogsPath = Path.Combine(FileSystem.AppDataDirectory, "weight_logs.json");
        private readonly string _categoriesPath = Path.Combine(FileSystem.AppDataDirectory, "categories.json");
        private readonly string _feedingConfigPath = Path.Combine(FileSystem.AppDataDirectory, "feeding_configs.json");
        private readonly string _animalsPath = Path.Combine(FileSystem.AppDataDirectory, "animals.json");

        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public FileStorageService()
        {
            // La inicialización se hace asíncronamente o bajo demanda
        }

        public async Task InitializeAsync()
        {
            bool needsReset = false;
            try
            {
                if (File.Exists(_feedingConfigPath))
                {
                    var current = await LoadListAsync<FeedingConfig>(_feedingConfigPath);
                    if (current.Count < 30 || current.All(c => string.IsNullOrEmpty(c.Alternatives)))
                    {
                        needsReset = true;
                    }
                }
            }
            catch
            {
                needsReset = true;
            }

            if (!File.Exists(_categoriesPath) || !File.Exists(_batchesPath) || !File.Exists(_feedingConfigPath) || needsReset)
            {
                if (File.Exists(_categoriesPath)) File.Delete(_categoriesPath);
                if (File.Exists(_batchesPath)) File.Delete(_batchesPath);
                if (File.Exists(_feedingConfigPath)) File.Delete(_feedingConfigPath);
                if (File.Exists(_vaccinationsPath)) File.Delete(_vaccinationsPath);
                if (File.Exists(_sanitaryPath)) File.Delete(_sanitaryPath);
                if (File.Exists(_weightLogsPath)) File.Delete(_weightLogsPath);
                await CreateSeedDataAsync();
            }
        }

        #region Generic Load/Save Helpers
        private async Task<List<T>> LoadListAsync<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<T>();

                using var stream = File.OpenRead(filePath);
                var list = await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonOptions);
                return list ?? new List<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando {filePath}: {ex.Message}");
                return new List<T>();
            }
        }

        private async Task SaveListAsync<T>(string filePath, List<T> data)
        {
            try
            {
                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, data, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando {filePath}: {ex.Message}");
            }
        }
        #endregion

        #region Animal Categories
        public async Task<List<AnimalCategory>> GetCategoriesAsync()
        {
            await InitializeAsync();
            return await LoadListAsync<AnimalCategory>(_categoriesPath);
        }
        #endregion

        #region Batches (Lotes)
        public async Task<List<Batch>> GetBatchesAsync()
        {
            await InitializeAsync();
            var batches = await LoadListAsync<Batch>(_batchesPath);
            return batches.OrderByDescending(b => b.CreatedAt).ToList();
        }

        public async Task SaveBatchAsync(Batch batch)
        {
            var batches = await GetBatchesAsync();
            var index = batches.FindIndex(b => b.Id == batch.Id);
            if (index >= 0)
            {
                batches[index] = batch;
            }
            else
            {
                batches.Add(batch);
            }
            await SaveListAsync(_batchesPath, batches);
        }

        public async Task DeleteBatchAsync(string batchId)
        {
            var batches = await GetBatchesAsync();
            var batch = batches.FirstOrDefault(b => b.Id == batchId);
            if (batch != null)
            {
                batches.Remove(batch);
                await SaveListAsync(_batchesPath, batches);

                // Limpieza en cascada de vacunas, sanitarios y pesos
                var vacs = await GetVaccinationsAsync();
                vacs.RemoveAll(v => v.BatchId == batchId);
                await SaveListAsync(_vaccinationsPath, vacs);

                var san = await GetSanitaryRecordsAsync();
                san.RemoveAll(s => s.BatchId == batchId);
                await SaveListAsync(_sanitaryPath, san);

                var weights = await GetWeightLogsAsync();
                weights.RemoveAll(w => w.BatchId == batchId);
                await SaveListAsync(_weightLogsPath, weights);

                var animals = await GetAnimalsAsync();
                animals.RemoveAll(a => a.BatchId == batchId);
                await SaveListAsync(_animalsPath, animals);
            }
        }
        #endregion

        #region Vaccinations
        public async Task<List<Vaccination>> GetVaccinationsAsync()
        {
            await InitializeAsync();
            var list = await LoadListAsync<Vaccination>(_vaccinationsPath);
            // Actualizar estados dinámicos al cargar
            foreach (var vac in list)
            {
                vac.Status = vac.DetermineStatus();
            }
            return list.OrderBy(v => v.ScheduledDate).ToList();
        }

        public async Task<List<Vaccination>> GetVaccinationsForBatchAsync(string batchId)
        {
            var list = await GetVaccinationsAsync();
            return list.Where(v => v.BatchId == batchId).ToList();
        }

        public async Task SaveVaccinationAsync(Vaccination vaccination)
        {
            var list = await GetVaccinationsAsync();
            vaccination.Status = vaccination.DetermineStatus();
            var index = list.FindIndex(v => v.Id == vaccination.Id);
            if (index >= 0)
            {
                list[index] = vaccination;
            }
            else
            {
                list.Add(vaccination);
            }
            await SaveListAsync(_vaccinationsPath, list);
        }

        public async Task DeleteVaccinationAsync(string vaccinationId)
        {
            var list = await GetVaccinationsAsync();
            list.RemoveAll(v => v.Id == vaccinationId);
            await SaveListAsync(_vaccinationsPath, list);
        }
        #endregion

        #region Sanitary Records
        public async Task<List<SanitaryRecord>> GetSanitaryRecordsAsync()
        {
            await InitializeAsync();
            var list = await LoadListAsync<SanitaryRecord>(_sanitaryPath);
            return list.OrderByDescending(s => s.StartDate).ToList();
        }

        public async Task<List<SanitaryRecord>> GetSanitaryRecordsForBatchAsync(string batchId)
        {
            var list = await GetSanitaryRecordsAsync();
            return list.Where(s => s.BatchId == batchId).ToList();
        }

        public async Task SaveSanitaryRecordAsync(SanitaryRecord record)
        {
            var list = await GetSanitaryRecordsAsync();
            var index = list.FindIndex(s => s.Id == record.Id);
            if (index >= 0)
            {
                list[index] = record;
            }
            else
            {
                list.Add(record);
            }
            await SaveListAsync(_sanitaryPath, list);
        }

        public async Task DeleteSanitaryRecordAsync(string recordId)
        {
            var list = await GetSanitaryRecordsAsync();
            list.RemoveAll(s => s.Id == recordId);
            await SaveListAsync(_sanitaryPath, list);
        }
        #endregion

        #region Weight Logs
        public async Task<List<WeightLog>> GetWeightLogsAsync()
        {
            await InitializeAsync();
            var list = await LoadListAsync<WeightLog>(_weightLogsPath);
            return list.OrderBy(w => w.Date).ToList();
        }

        public async Task<List<WeightLog>> GetWeightLogsForBatchAsync(string batchId)
        {
            var list = await GetWeightLogsAsync();
            return list.Where(w => w.BatchId == batchId).ToList();
        }

        public async Task SaveWeightLogAsync(WeightLog log)
        {
            var list = await GetWeightLogsAsync();
            var index = list.FindIndex(w => w.Id == log.Id);
            if (index >= 0)
            {
                list[index] = log;
            }
            else
            {
                list.Add(log);
            }
            await SaveListAsync(_weightLogsPath, list);

            // Actualizar el peso actual del lote al del último registro de peso
            var batchWeights = list.Where(w => w.BatchId == log.BatchId).OrderByDescending(w => w.Date).FirstOrDefault();
            if (batchWeights != null)
            {
                var batches = await GetBatchesAsync();
                var batch = batches.FirstOrDefault(b => b.Id == log.BatchId);
                if (batch != null)
                {
                    batch.CurrentWeight = batchWeights.AverageWeight;
                    await SaveListAsync(_batchesPath, batches);
                }
            }
        }

        public async Task DeleteWeightLogAsync(string logId)
        {
            var list = await GetWeightLogsAsync();
            var log = list.FirstOrDefault(w => w.Id == logId);
            if (log != null)
            {
                list.Remove(log);
                await SaveListAsync(_weightLogsPath, list);
            }
        }
        #endregion

        #region Feeding Configs
        public async Task<List<FeedingConfig>> GetFeedingConfigsAsync()
        {
            await InitializeAsync();
            return await LoadListAsync<FeedingConfig>(_feedingConfigPath);
        }

        public async Task<FeedingConfig?> GetFeedingConfigForBatchAsync(string categoryId, string purpose, int ageInWeeks)
        {
            var configs = await GetFeedingConfigsAsync();
            return configs.FirstOrDefault(c => 
                c.CategoryId == categoryId && 
                c.Purpose.Equals(purpose, StringComparison.OrdinalIgnoreCase) && 
                ageInWeeks >= c.MinAgeWeeks && 
                ageInWeeks <= c.MaxAgeWeeks);
        }
        #endregion

        #region Seed Data Generation
        private async Task CreateSeedDataAsync()
        {
            // 1. Categorías y Razas
            var categories = new List<AnimalCategory>
            {
                new() {
                    Id = "porcinos", Name = "Porcinos", Icon = "🐷",
                    Breeds = new() { "Landrace", "Yorkshire", "Duroc", "Hampshire", "Criollo" },
                    Purposes = new() { "Engorde", "Reproducción", "Lechones", "Padrillos" }
                },
                new() {
                    Id = "bovinos_leche", Name = "Bovinos (Lechero)", Icon = "🐄",
                    Breeds = new() { "Holstein", "Jersey", "Pardo Suizo", "Gyr" },
                    Purposes = new() { "Producción lechera", "Crianza", "Venta" }
                },
                new() {
                    Id = "bovinos_carne", Name = "Bovinos (Carne)", Icon = "🐂",
                    Breeds = new() { "Angus", "Hereford", "Brahman", "Nelore" },
                    Purposes = new() { "Engorde", "Producción de carne", "Reproducción" }
                },
                new() {
                    Id = "avicolas_engorde", Name = "Avícola (Engorde)", Icon = "🐤",
                    Breeds = new() { "Cobb 500", "Ross 308", "Hubbard" },
                    Purposes = new() { "Engorde", "Producción de carne" }
                },
                new() {
                    Id = "avicolas_postura", Name = "Avícola (Postura)", Icon = "🐔",
                    Breeds = new() { "Leghorn", "Rhode Island Red", "Hy-Line" },
                    Purposes = new() { "Producción de huevos", "Reproducción" }
                },
                new() {
                    Id = "ovinos", Name = "Ovinos", Icon = "🐑",
                    Breeds = new() { "Suffolk", "Hampshire Down", "Dorper", "Corriedale" },
                    Purposes = new() { "Carne", "Lana", "Reproducción" }
                },
                new() {
                    Id = "caprinos", Name = "Caprinos", Icon = "🐐",
                    Breeds = new() { "Saanen", "Alpina", "Boer", "Anglo Nubian" },
                    Purposes = new() { "Leche", "Carne", "Reproducción" }
                },
                new() {
                    Id = "cunicultura", Name = "Cunicultura", Icon = "🐇",
                    Breeds = new() { "Nueva Zelanda", "California", "Mariposa" },
                    Purposes = new() { "Engorde", "Reproducción", "Carne" }
                }
            };
            await SaveListAsync(_categoriesPath, categories);

            // 2. Configuraciones de Alimentación Inteligente
            var feedingConfigs = new List<FeedingConfig>
            {
                // Porcinos Engorde
                new() { Id = "fc_porc_1", CategoryId = "porcinos", Purpose = "Engorde", MinAgeWeeks = 0, MaxAgeWeeks = 4, FeedType = "Pre-iniciador Porcino", DailyAmountPerAnimal = 0.25, FrequencyPerDay = 4, NutritionalInfo = "Proteína: 21%, Lisina: 1.35%. Concentrado fino para lechones destetados.", RecommendedWaterLiters = 1.5, Alternatives = "Purina Iniciador / Solla Pre-destete" },
                new() { Id = "fc_porc_2", CategoryId = "porcinos", Purpose = "Engorde", MinAgeWeeks = 5, MaxAgeWeeks = 8, FeedType = "Iniciador Porcino", DailyAmountPerAnimal = 0.8, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 19%, Lisina: 1.15%. Alimento de transición y desarrollo temprano.", RecommendedWaterLiters = 3.0, Alternatives = "Cargill Cerdos 1 / Corina Inicial" },
                new() { Id = "fc_porc_3", CategoryId = "porcinos", Purpose = "Engorde", MinAgeWeeks = 9, MaxAgeWeeks = 16, FeedType = "Crecimiento Porcino", DailyAmountPerAnimal = 1.8, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%, Fibra: 4.5%. Maximiza conversión alimenticia.", RecommendedWaterLiters = 6.0, Alternatives = "Purina Crecimiento / Champion Engorde" },
                new() { Id = "fc_porc_4", CategoryId = "porcinos", Purpose = "Engorde", MinAgeWeeks = 17, MaxAgeWeeks = 26, FeedType = "Engorde / Acabado Porcino", DailyAmountPerAnimal = 2.8, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 14%, Energía Digestible: 3.3 Mcal. Optimización de magrez.", RecommendedWaterLiters = 9.0, Alternatives = "Solla Acabado / Cargill Finalizador" },

                // Porcinos Reproducción (Cerdas gestantes / lactantes)
                new() { Id = "fc_porc_rep1", CategoryId = "porcinos", Purpose = "Reproducción", MinAgeWeeks = 0, MaxAgeWeeks = 24, FeedType = "Desarrollo Hembras", DailyAmountPerAnimal = 2.0, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 15%, Lisina: 0.85%. Alimento de desarrollo para futuras madres.", RecommendedWaterLiters = 8.0, Alternatives = "Purina Desarrollo Madres / Solla Hembras R" },
                new() { Id = "fc_porc_rep2", CategoryId = "porcinos", Purpose = "Reproducción", MinAgeWeeks = 25, MaxAgeWeeks = 50, FeedType = "Gestación Cerda", DailyAmountPerAnimal = 2.2, FrequencyPerDay = 1, NutritionalInfo = "Proteína: 13%, Fibra: 8.0%. Control del estado graso para evitar partos difíciles.", RecommendedWaterLiters = 12.0, Alternatives = "Cargill Gestación / Corina Marranas" },
                new() { Id = "fc_porc_rep3", CategoryId = "porcinos", Purpose = "Reproducción", MinAgeWeeks = 51, MaxAgeWeeks = 1000, FeedType = "Lactancia Cerda", DailyAmountPerAnimal = 5.5, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 18%, Grasa: 5.5%. Alta densidad nutricional para lactancia de camadas.", RecommendedWaterLiters = 25.0, Alternatives = "Solla Lactancia / Purina Cerdas Lactantes" },

                // Porcinos Lechones
                new() { Id = "fc_porc_lech1", CategoryId = "porcinos", Purpose = "Lechones", MinAgeWeeks = 0, MaxAgeWeeks = 4, FeedType = "Pre-iniciador Fase 1", DailyAmountPerAnimal = 0.15, FrequencyPerDay = 5, NutritionalInfo = "Proteína: 22%. Alimento altamente digerible a base de lácteos.", RecommendedWaterLiters = 1.0, Alternatives = "Purina Fase 1 / Cargill Lechones" },
                new() { Id = "fc_porc_lech2", CategoryId = "porcinos", Purpose = "Lechones", MinAgeWeeks = 5, MaxAgeWeeks = 8, FeedType = "Pre-iniciador Fase 2", DailyAmountPerAnimal = 0.50, FrequencyPerDay = 4, NutritionalInfo = "Proteína: 20%. Nutrición especializada post-destete.", RecommendedWaterLiters = 2.0, Alternatives = "Solla Pre-inicio / Corina Inicial" },

                // Porcinos Padrillos
                new() { Id = "fc_porc_pad1", CategoryId = "porcinos", Purpose = "Padrillos", MinAgeWeeks = 25, MaxAgeWeeks = 1000, FeedType = "Concentrado Padrillos", DailyAmountPerAnimal = 2.5, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 14%, Selenio y Vitamina E para óptima fertilidad y libido.", RecommendedWaterLiters = 15.0, Alternatives = "Cargill Verracos / Purina Reproducción" },
                
                // Avícola Engorde (Pollos)
                new() { Id = "fc_avi_e1", CategoryId = "avicolas_engorde", Purpose = "Engorde", MinAgeWeeks = 0, MaxAgeWeeks = 2, FeedType = "Pre-inicio Pollito", DailyAmountPerAnimal = 0.03, FrequencyPerDay = 5, NutritionalInfo = "Proteína: 22%, Calcio: 0.9%. Alta asimilación inicial.", RecommendedWaterLiters = 0.08, Alternatives = "Purina Pre-inicio Aves / Solla Pollitos" },
                new() { Id = "fc_avi_e2", CategoryId = "avicolas_engorde", Purpose = "Engorde", MinAgeWeeks = 3, MaxAgeWeeks = 4, FeedType = "Crecimiento Pollos", DailyAmountPerAnimal = 0.10, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 20%, Fibra: 4.0%. Desarrollo esquelético rápido.", RecommendedWaterLiters = 0.22, Alternatives = "Cargill Engorde / Corina Crecimiento" },
                new() { Id = "fc_avi_e3", CategoryId = "avicolas_engorde", Purpose = "Engorde", MinAgeWeeks = 5, MaxAgeWeeks = 8, FeedType = "Finalizador Pollos", DailyAmountPerAnimal = 0.17, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 18%, Energía: 3200 kcal. Terminación de pechuga.", RecommendedWaterLiters = 0.35, Alternatives = "Solla Finalizador / Purina Acabado" },
                
                new() { Id = "fc_avi_e_car1", CategoryId = "avicolas_engorde", Purpose = "Producción de carne", MinAgeWeeks = 0, MaxAgeWeeks = 2, FeedType = "Pre-inicio Pollito", DailyAmountPerAnimal = 0.03, FrequencyPerDay = 5, NutritionalInfo = "Proteína: 22%. Alta densidad energética inicial.", RecommendedWaterLiters = 0.08, Alternatives = "Purina Pre-inicio / Cargill Iniciador" },
                new() { Id = "fc_avi_e_car2", CategoryId = "avicolas_engorde", Purpose = "Producción de carne", MinAgeWeeks = 3, MaxAgeWeeks = 8, FeedType = "Crecimiento/Acabado Pollos", DailyAmountPerAnimal = 0.14, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 19%. Desarrollo de masas musculares.", RecommendedWaterLiters = 0.28, Alternatives = "Champion Engorde / Solla Finalizador" },

                // Gallinas Ponedoras (Postura - Producción de huevos)
                new() { Id = "fc_avi_p1", CategoryId = "avicolas_postura", Purpose = "Producción de huevos", MinAgeWeeks = 0, MaxAgeWeeks = 6, FeedType = "Crianza Pollita", DailyAmountPerAnimal = 0.03, FrequencyPerDay = 4, NutritionalInfo = "Proteína: 19%, Calcio: 1.0%. Fomento inmunológico temprano.", RecommendedWaterLiters = 0.07, Alternatives = "Purina Pollitas / Solla Crianza" },
                new() { Id = "fc_avi_p2", CategoryId = "avicolas_postura", Purpose = "Producción de huevos", MinAgeWeeks = 7, MaxAgeWeeks = 18, FeedType = "Desarrollo Ponedora", DailyAmountPerAnimal = 0.07, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 15%, Fibra: 5.5%. Evita acumulación excesiva de grasa.", RecommendedWaterLiters = 0.15, Alternatives = "Cargill Desarrollo / Corina Ponedoras" },
                new() { Id = "fc_avi_p3", CategoryId = "avicolas_postura", Purpose = "Producción de huevos", MinAgeWeeks = 19, MaxAgeWeeks = 1000, FeedType = "Postura Fase 1", DailyAmountPerAnimal = 0.11, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16.5%, Calcio: 4.0%, Fósforo: 0.4%. Formación de cáscara óptima.", RecommendedWaterLiters = 0.25, Alternatives = "Solla Postura 1 / Purina Gallinas" },

                // Gallinas Reproductoras (Postura - Reproducción)
                new() { Id = "fc_avi_prep1", CategoryId = "avicolas_postura", Purpose = "Reproducción", MinAgeWeeks = 0, MaxAgeWeeks = 18, FeedType = "Desarrollo Reproductoras", DailyAmountPerAnimal = 0.08, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 15.5%. Crecimiento uniforme y controlado de reproductoras.", RecommendedWaterLiters = 0.18, Alternatives = "Purina Reproducción Pollitas / Solla Crianza R" },
                new() { Id = "fc_avi_prep2", CategoryId = "avicolas_postura", Purpose = "Reproducción", MinAgeWeeks = 19, MaxAgeWeeks = 1000, FeedType = "Postura Reproductoras", DailyAmountPerAnimal = 0.12, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16.5%, Calcio: 4.2%. Formación de cáscara y fertilidad del huevo.", RecommendedWaterLiters = 0.28, Alternatives = "Cargill Postura R / Corina Reproductoras" },

                // Bovinos Leche
                new() { Id = "fc_bov_l1", CategoryId = "bovinos_leche", Purpose = "Producción lechera", MinAgeWeeks = 50, MaxAgeWeeks = 1000, FeedType = "Concentrado Lactancia + Pastoreo", DailyAmountPerAnimal = 6.0, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 18%, FND: 30%, Calcio: 0.8%. Suplementación por litros producidos.", RecommendedWaterLiters = 80.0, Alternatives = "Purina Lechera 18% / Solla Vacas Leche" },
                new() { Id = "fc_bov_l_cr1", CategoryId = "bovinos_leche", Purpose = "Crianza", MinAgeWeeks = 0, MaxAgeWeeks = 12, FeedType = "Lactante Iniciador Ternera", DailyAmountPerAnimal = 1.2, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 20%. Sustituto lácteo + concentrado iniciador de terneras.", RecommendedWaterLiters = 10.0, Alternatives = "Cargill Terneras Inicial / Corina Sustituto Lácteo" },
                new() { Id = "fc_bov_l_cr2", CategoryId = "bovinos_leche", Purpose = "Crianza", MinAgeWeeks = 13, MaxAgeWeeks = 500, FeedType = "Crecimiento Novillas", DailyAmountPerAnimal = 3.0, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%. Desarrollo estructural de novillas de reemplazo.", RecommendedWaterLiters = 25.0, Alternatives = "Solla Novillas / Purina Crecimiento Bovino" },

                // Bovinos Carne
                new() { Id = "fc_bov_c1", CategoryId = "bovinos_carne", Purpose = "Engorde", MinAgeWeeks = 0, MaxAgeWeeks = 12, FeedType = "Iniciador Ternero", DailyAmountPerAnimal = 1.5, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 18%, Fibra: 10%. Desarrollo de papilas ruminales.", RecommendedWaterLiters = 12.0, Alternatives = "Purina Terneros / Cargill Iniciador Carne" },
                new() { Id = "fc_bov_c2", CategoryId = "bovinos_carne", Purpose = "Engorde", MinAgeWeeks = 13, MaxAgeWeeks = 36, FeedType = "Crecimiento Bovino", DailyAmountPerAnimal = 3.5, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%, Fibra: 15%. Crecimiento de estructura muscular.", RecommendedWaterLiters = 25.0, Alternatives = "Solla Novillos / Champion Crecimiento" },
                new() { Id = "fc_bov_c3", CategoryId = "bovinos_carne", Purpose = "Engorde", MinAgeWeeks = 37, MaxAgeWeeks = 1000, FeedType = "Engorde Bovino / Pastoreo", DailyAmountPerAnimal = 8.0, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 14%, FND: 40%. Conversión en canal.", RecommendedWaterLiters = 50.0, Alternatives = "Purina Engorde Bovino / Cargill Finalizador Carne" },
                
                new() { Id = "fc_bov_c_prod1", CategoryId = "bovinos_carne", Purpose = "Producción de carne", MinAgeWeeks = 0, MaxAgeWeeks = 36, FeedType = "Pastoreo + Suplementación", DailyAmountPerAnimal = 4.0, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 15%. Crecimiento de ganado de carne a pasto.", RecommendedWaterLiters = 30.0, Alternatives = "Corina Engorde / Solla Carne Pasto" },
                new() { Id = "fc_bov_c_rep1", CategoryId = "bovinos_carne", Purpose = "Reproducción", MinAgeWeeks = 53, MaxAgeWeeks = 1000, FeedType = "Pastoreo + Sales Proteicas (Madres)", DailyAmountPerAnimal = 10.0, FrequencyPerDay = 2, NutritionalInfo = "Materia seca. Nutrición base fibra + sales minerales con fósforo para cría.", RecommendedWaterLiters = 55.0, Alternatives = "Purina Cría / Cargill Vacas con Fósforo" },

                // Ovinos Carne
                new() { Id = "fc_ovi_1", CategoryId = "ovinos", Purpose = "Carne", MinAgeWeeks = 0, MaxAgeWeeks = 8, FeedType = "Pre-iniciador Cordero", DailyAmountPerAnimal = 0.2, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 20%, Grasa: 5%. Nutrición de apoyo al destete.", RecommendedWaterLiters = 1.2, Alternatives = "Purina Corderos / Solla Cordero Pre-destete" },
                new() { Id = "fc_ovi_2", CategoryId = "ovinos", Purpose = "Carne", MinAgeWeeks = 9, MaxAgeWeeks = 24, FeedType = "Crecimiento / Pastoreo", DailyAmountPerAnimal = 0.8, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%, Fibra: 12%. Desarrollo de fibra y carne.", RecommendedWaterLiters = 3.5, Alternatives = "Cargill Borregos / Corina Crecimiento Ovinos" },
                new() { Id = "fc_ovi_3", CategoryId = "ovinos", Purpose = "Carne", MinAgeWeeks = 25, MaxAgeWeeks = 1000, FeedType = "Engorde Ovino", DailyAmountPerAnimal = 1.5, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 14%, Calcio: 0.6%. Engorde rápido en establo.", RecommendedWaterLiters = 5.0, Alternatives = "Solla Engorde Ovinos / Purina Borregos Final" },

                // Ovinos Reproducción
                new() { Id = "fc_ovi_rep1", CategoryId = "ovinos", Purpose = "Reproducción", MinAgeWeeks = 0, MaxAgeWeeks = 24, FeedType = "Desarrollo Ovinos", DailyAmountPerAnimal = 0.6, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%. Desarrollo de corderas de reemplazo.", RecommendedWaterLiters = 3.0, Alternatives = "Purina Borregas Reemplazo / Solla Ovinos Cría" },
                new() { Id = "fc_ovi_rep2", CategoryId = "ovinos", Purpose = "Reproducción", MinAgeWeeks = 25, MaxAgeWeeks = 1000, FeedType = "Gestación/Lactancia Ovina", DailyAmountPerAnimal = 1.6, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 15%, Fibra: 15%. Para ovejas gestantes y paridas.", RecommendedWaterLiters = 6.0, Alternatives = "Cargill Gestación Ovinos / Champion Lactancia Ovinos" },

                // Caprinos Leche
                new() { Id = "fc_cap_1", CategoryId = "caprinos", Purpose = "Leche", MinAgeWeeks = 0, MaxAgeWeeks = 8, FeedType = "Pre-iniciador Cabrito", DailyAmountPerAnimal = 0.15, FrequencyPerDay = 4, NutritionalInfo = "Proteína: 22%. Sustituto lácteo y fibra tierna.", RecommendedWaterLiters = 1.0, Alternatives = "Purina Cabritos / Cargill Cabras Inicial" },
                new() { Id = "fc_cap_2", CategoryId = "caprinos", Purpose = "Leche", MinAgeWeeks = 9, MaxAgeWeeks = 24, FeedType = "Crecimiento Cabras", DailyAmountPerAnimal = 0.6, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 17%, Fibra: 14%. Desarrollo de alzada caprina.", RecommendedWaterLiters = 3.0, Alternatives = "Solla Crecimiento Cabras / Corina Cabras" },
                new() { Id = "fc_cap_3", CategoryId = "caprinos", Purpose = "Leche", MinAgeWeeks = 25, MaxAgeWeeks = 1000, FeedType = "Concentrado Lactancia Caprina", DailyAmountPerAnimal = 1.2, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 18%, Fósforo: 0.5%. Sostenimiento lácteo.", RecommendedWaterLiters = 6.0, Alternatives = "Purina Cabras Lecheras / Cargill Lactancia Caprinos" },

                // Caprinos Reproducción
                new() { Id = "fc_cap_rep1", CategoryId = "caprinos", Purpose = "Reproducción", MinAgeWeeks = 25, MaxAgeWeeks = 1000, FeedType = "Lactancia/Gestación Caprina", DailyAmountPerAnimal = 1.4, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%. Suplemento para cabras gestantes y lactantes.", RecommendedWaterLiters = 7.0, Alternatives = "Solla Cabras Gestación / Champion Cabritos" },

                // Cunicultura Engorde
                new() { Id = "fc_cuni_1", CategoryId = "cunicultura", Purpose = "Engorde", MinAgeWeeks = 0, MaxAgeWeeks = 4, FeedType = "Pre-iniciador Gazapo", DailyAmountPerAnimal = 0.05, FrequencyPerDay = 3, NutritionalInfo = "Proteína: 20%, Fibra: 16%. Prevención de enteropatías mucoides.", RecommendedWaterLiters = 0.12, Alternatives = "Purina Conejos Gazapos / Solla Conejos Lactancia" },
                new() { Id = "fc_cuni_2", CategoryId = "cunicultura", Purpose = "Engorde", MinAgeWeeks = 5, MaxAgeWeeks = 8, FeedType = "Crecimiento Conejo", DailyAmountPerAnimal = 0.12, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 17%, Fibra: 18%. Desarrollo digestivo estable.", RecommendedWaterLiters = 0.25, Alternatives = "Cargill Crecimiento Conejos / Corina Conejos" },
                new() { Id = "fc_cuni_3", CategoryId = "cunicultura", Purpose = "Engorde", MinAgeWeeks = 9, MaxAgeWeeks = 1000, FeedType = "Engorde Conejo", DailyAmountPerAnimal = 0.18, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 16%, Almidón: 12%. Finalización y engorde comercial.", RecommendedWaterLiters = 0.35, Alternatives = "Purina Engorde Conejo / Solla Conejos Final" },

                // Cunicultura Reproducción
                new() { Id = "fc_cuni_rep1", CategoryId = "cunicultura", Purpose = "Reproducción", MinAgeWeeks = 20, MaxAgeWeeks = 1000, FeedType = "Lactancia Coneja Madre", DailyAmountPerAnimal = 0.35, FrequencyPerDay = 2, NutritionalInfo = "Proteína: 18%, Fibra: 15%. Alimento de alta energía para lactancia intensiva.", RecommendedWaterLiters = 0.60, Alternatives = "Cargill Coneja Madre / Champion Reproductoras Conejo" }
            };
            await SaveListAsync(_feedingConfigPath, feedingConfigs);

            // 3. Lotes de Prueba (Seed Batches)
            var b1Id = Guid.NewGuid().ToString();
            var b2Id = Guid.NewGuid().ToString();
            var b3Id = Guid.NewGuid().ToString();

            var batches = new List<Batch>
            {
                new()
                {
                    Id = b1Id,
                    Name = "Lote Cerdos Engorde A",
                    CategoryId = "porcinos",
                    CategoryName = "Porcinos",
                    Breed = "Duroc x Landrace",
                    InitialQuantity = 50,
                    Quantity = 48,
                    MortalityCount = 2,
                    BirthDate = DateTime.Today.AddDays(-60), // 8 semanas y media
                    Gender = "Mixto",
                    InitialWeight = 12.5,
                    CurrentWeight = 36.5,
                    SanitaryStatus = "Excelente",
                    Purpose = "Engorde",
                    Notes = "Ingresados en galpón 2. Muy buen desarrollo y conversión alimenticia.",
                    QRCode = $"GALPONAPP-LOTE-{b1Id[..8]}",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-60)
                },
                new()
                {
                    Id = b2Id,
                    Name = "Gallinas Ponedoras Paddock B",
                    CategoryId = "avicolas_postura",
                    CategoryName = "Avícola (Postura)",
                    Breed = "Rhode Island Red",
                    InitialQuantity = 150,
                    Quantity = 149,
                    MortalityCount = 1,
                    BirthDate = DateTime.Today.AddDays(-140), // 20 semanas
                    Gender = "Hembra",
                    InitialWeight = 0.08,
                    CurrentWeight = 1.65,
                    SanitaryStatus = "Excelente",
                    Purpose = "Producción de huevos",
                    Notes = "Comenzando etapa de postura activa (10% de puesta diaria actual).",
                    QRCode = $"GALPONAPP-LOTE-{b2Id[..8]}",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-140)
                },
                new()
                {
                    Id = b3Id,
                    Name = "Bovinos Lactancia 1",
                    CategoryId = "bovinos_leche",
                    CategoryName = "Bovinos (Lechero)",
                    Breed = "Holstein",
                    InitialQuantity = 12,
                    Quantity = 12,
                    MortalityCount = 0,
                    BirthDate = DateTime.Today.AddDays(-1000), // ~2.7 años
                    Gender = "Hembra",
                    InitialWeight = 40.0,
                    CurrentWeight = 540.0,
                    SanitaryStatus = "Regular",
                    Purpose = "Producción lechera",
                    Notes = "Grupo de ordeño mecánico mañana/tarde. Una vaca aislada por mastitis.",
                    QRCode = $"GALPONAPP-LOTE-{b3Id[..8]}",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-300)
                }
            };
            await SaveListAsync(_batchesPath, batches);

            // 4. Registros de Pesos Históricos (Weight Logs)
            var weightLogs = new List<WeightLog>
            {
                // Cerdos Engorde A
                new() { Id = Guid.NewGuid().ToString(), BatchId = b1Id, Date = DateTime.Today.AddDays(-60), AverageWeight = 12.5, AverageSize = 30, MortalityCount = 0, Notes = "Peso inicial al destete" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b1Id, Date = DateTime.Today.AddDays(-45), AverageWeight = 17.2, AverageSize = 38, MortalityCount = 0, Notes = "Primer control quincenal" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b1Id, Date = DateTime.Today.AddDays(-30), AverageWeight = 23.0, AverageSize = 46, MortalityCount = 1, Notes = "Segundo control. Se registra 1 baja por asfixia." },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b1Id, Date = DateTime.Today.AddDays(-15), AverageWeight = 29.8, AverageSize = 55, MortalityCount = 1, Notes = "Tercer control quincenal. 1 baja por hernia." },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b1Id, Date = DateTime.Today.AddDays(0), AverageWeight = 36.5, AverageSize = 65, MortalityCount = 0, Notes = "Pesaje actual. Óptimo crecimiento." },

                // Gallinas Ponedoras B
                new() { Id = Guid.NewGuid().ToString(), BatchId = b2Id, Date = DateTime.Today.AddDays(-140), AverageWeight = 0.08, AverageSize = 5, MortalityCount = 0, Notes = "Llegada de pollitas de 1 día" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b2Id, Date = DateTime.Today.AddDays(-100), AverageWeight = 0.52, AverageSize = 12, MortalityCount = 1, Notes = "Control a las 5 semanas" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b2Id, Date = DateTime.Today.AddDays(-50), AverageWeight = 1.15, AverageSize = 20, MortalityCount = 0, Notes = "Control a las 12 semanas" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b2Id, Date = DateTime.Today.AddDays(0), AverageWeight = 1.65, AverageSize = 25, MortalityCount = 0, Notes = "Pesaje al inicio de postura (semana 20)" },

                // Bovinos Leche 1
                new() { Id = Guid.NewGuid().ToString(), BatchId = b3Id, Date = DateTime.Today.AddDays(-90), AverageWeight = 525.0, AverageSize = 135, MortalityCount = 0, Notes = "Inicio de periodo de lactancia" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b3Id, Date = DateTime.Today.AddDays(-30), AverageWeight = 534.0, AverageSize = 136, MortalityCount = 0, Notes = "Control de pesaje y condición corporal" },
                new() { Id = Guid.NewGuid().ToString(), BatchId = b3Id, Date = DateTime.Today.AddDays(0), AverageWeight = 540.0, AverageSize = 136, MortalityCount = 0, Notes = "Pesaje actual" }
            };
            await SaveListAsync(_weightLogsPath, weightLogs);

            // 5. Calendario de Vacunación (Vaccinations Seed)
            var vaccinations = new List<Vaccination>
            {
                // Cerdos Engorde A
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b1Id, BatchName = "Lote Cerdos Engorde A",
                    Name = "Vacuna contra la Peste Porcina Clásica (Cólera Porcino)", Type = "Vacuna", Description = "Inmunización obligatoria oficial contra el Cólera Porcino (Cepa China). Vía Intramuscular (IM).",
                    Dose = 2.0, DoseUnit = "ml", ScheduledDate = DateTime.Today.AddDays(-45), AppliedDate = DateTime.Today.AddDays(-45),
                    Status = "Aplicada", Notes = "Aplicación exitosa por el veterinario."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b1Id, BatchName = "Lote Cerdos Engorde A",
                    Name = "Vacuna contra Mycoplasma hyopneumoniae", Type = "Vacuna", Description = "Prevención de la neumonía enzoótica (Dosis 1). Vía Intramuscular (IM).",
                    Dose = 2.0, DoseUnit = "ml", ScheduledDate = DateTime.Today.AddDays(-30), AppliedDate = DateTime.Today.AddDays(-30),
                    Status = "Aplicada", Notes = "Primera dosis."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b1Id, BatchName = "Lote Cerdos Engorde A",
                    Name = "Refuerzo contra Mycoplasma hyopneumoniae", Type = "Refuerzo", Description = "Segunda dosis para inmunidad protectora pulmonar. Vía Intramuscular (IM).",
                    Dose = 2.0, DoseUnit = "ml", ScheduledDate = DateTime.Today.AddDays(-9), AppliedDate = null,
                    Status = "Atrasada", Notes = "Se postergó por falta de stock. Aplicar urgente."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b1Id, BatchName = "Lote Cerdos Engorde A",
                    Name = "Desparasitante (Ivermectina 1%)", Type = "Desparasitación", Description = "Control de parásitos gastrointestinales, pulmonares, sarna y piojos. Vía Subcutánea (SC).",
                    Dose = 1.0, DoseUnit = "ml/33kg", ScheduledDate = DateTime.Today.AddDays(15), AppliedDate = null,
                    Status = "Pendiente", Notes = "Subcutánea."
                },

                // Gallinas B
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b2Id, BatchName = "Gallinas Ponedoras Paddock B",
                    Name = "Vacuna contra Newcastle + Bronquitis", Type = "Vacuna", Description = "Dosis inicial protectora ocular/agua contra Newcastle y Bronquitis.",
                    Dose = 0.03, DoseUnit = "ml (1 gota)", ScheduledDate = DateTime.Today.AddDays(-133), AppliedDate = DateTime.Today.AddDays(-133),
                    Status = "Aplicada", Notes = "Aplicado en criadero de origen."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b2Id, BatchName = "Gallinas Ponedoras Paddock B",
                    Name = "Vacuna contra Viruela Aviar", Type = "Vacuna", Description = "Prevención de la Viruela Aviar. Vía Punción alar.",
                    Dose = 1.0, DoseUnit = "dosis/ave", ScheduledDate = DateTime.Today.AddDays(-105), AppliedDate = DateTime.Today.AddDays(-105),
                    Status = "Aplicada", Notes = "Revisión exitosa de costra alar a los 7 días."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b2Id, BatchName = "Gallinas Ponedoras Paddock B",
                    Name = "Multivitamínico AD3E + Calcio", Type = "Vitamina", Description = "Soporte vitamínico para el inicio del ciclo de puesta activa.",
                    Dose = 2.0, DoseUnit = "g/L", ScheduledDate = DateTime.Today.AddDays(-3), AppliedDate = DateTime.Today.AddDays(-3),
                    Status = "Aplicada", Notes = "Suministrado vía agua de bebida durante 3 días."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b2Id, BatchName = "Gallinas Ponedoras Paddock B",
                    Name = "Desparasitación Coccidiostato", Type = "Desparasitación", Description = "Prevención de coccidiosis intestinal en etapa de huevo",
                    Dose = 0.5, DoseUnit = "ml/L", ScheduledDate = DateTime.Today.AddDays(30), AppliedDate = null,
                    Status = "Pendiente", Notes = "Periodo de retiro de 0 días para el huevo."
                },

                // Bovinos 1
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b3Id, BatchName = "Bovinos Lactancia 1",
                    Name = "Vacuna contra la Fiebre Aftosa", Type = "Vacuna", Description = "Campaña obligatoria de vacunación contra la fiebre aftosa. Vía Subcutánea (SC) o Intramuscular (IM).",
                    Dose = 2.0, DoseUnit = "ml", ScheduledDate = DateTime.Today.AddDays(-15), AppliedDate = DateTime.Today.AddDays(-15),
                    Status = "Aplicada", Notes = "Aplicado bajo supervisión de ente oficial sanitario."
                },
                new() {
                    Id = Guid.NewGuid().ToString(), BatchId = b3Id, BatchName = "Bovinos Lactancia 1",
                    Name = "Refuerzo Triple Clostridial", Type = "Refuerzo", Description = "Prevención de carbón sintomático, edema maligno y enterotoxemia. Vía Subcutánea (SC) en la tabla del cuello.",
                    Dose = 5.0, DoseUnit = "ml", ScheduledDate = DateTime.Today.AddDays(10), AppliedDate = null,
                    Status = "Pendiente", Notes = "Vía subcutánea."
                }
            };
            await SaveListAsync(_vaccinationsPath, vaccinations);

            // 6. Registro Sanitario (Sanitary Seeds)
            var sanitaryRecords = new List<SanitaryRecord>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(), BatchId = b1Id, BatchName = "Lote Cerdos Engorde A",
                    Diagnosis = "Diarrea Neonatal (E. Coli)", AffectedCount = 4,
                    Treatment = "Hidratación oral y tratamiento antibiótico inyectable",
                    Medication = "Enrofloxacina 10% + Electrolitos", Dose = "1 ml por cada 10 kg de peso por 3 días",
                    StartDate = DateTime.Today.AddDays(-40), EndDate = DateTime.Today.AddDays(-36),
                    IsIsolated = true, Cost = 75.0, Status = "Recuperados",
                    Notes = "Todos los lechones respondieron favorablemente al tercer día. Retornados al lote."
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(), BatchId = b3Id, BatchName = "Bovinos Lactancia 1",
                    Diagnosis = "Mastitis subclínica (Cuarto Trasero Derecho)", AffectedCount = 1,
                    Treatment = "Ordeño a fondo del cuarto afectado y aplicación de jeringa intramamaria",
                    Medication = "Cefapirina Sódica (Today)", Dose = "1 jeringa intramamaria cada 12 horas por 3 dosis",
                    StartDate = DateTime.Today.AddDays(-3), EndDate = null,
                    IsIsolated = true, Cost = 90.0, Status = "Bajo Tratamiento",
                    Notes = "Vaca aislada en corral de enfermería. Leche descartada del tanque común. Progreso favorable."
                }
            };
            await SaveListAsync(_sanitaryPath, sanitaryRecords);
        }
        #endregion

        #region Animals
        public async Task<List<Animal>> GetAnimalsAsync()
        {
            await InitializeAsync();
            return await LoadListAsync<Animal>(_animalsPath);
        }

        public async Task<List<Animal>> GetAnimalsForBatchAsync(string batchId, int quantity, double currentWeight)
        {
            var list = await GetAnimalsAsync();
            var batchAnimals = list.Where(a => a.BatchId == batchId).ToList();

            if (batchAnimals.Count == 0 && quantity > 0)
            {
                // Auto-generate animals
                var random = new Random(batchId.GetHashCode()); // Seed with hash code to keep it deterministic for a given batch ID
                for (int i = 1; i <= quantity; i++)
                {
                    // Generate a spread of weights around currentWeight (e.g. ±10%)
                    double weightSpread = currentWeight * 0.1;
                    double randomOffset = (random.NextDouble() - 0.5) * 2 * weightSpread;
                    double weight = Math.Max(0.1, currentWeight + randomOffset);

                    var animal = new Animal
                    {
                        Id = Guid.NewGuid().ToString(),
                        BatchId = batchId,
                        Name = $"Animal #{i:D3}",
                        Weight = weight,
                        Status = "Saludable"
                    };
                    batchAnimals.Add(animal);
                    list.Add(animal);
                }
                await SaveListAsync(_animalsPath, list);
            }
            return batchAnimals.OrderBy(a => a.Name).ToList();
        }

        public async Task SaveAnimalAsync(Animal animal)
        {
            var list = await GetAnimalsAsync();
            var index = list.FindIndex(a => a.Id == animal.Id);
            if (index >= 0)
            {
                list[index] = animal;
            }
            else
            {
                list.Add(animal);
            }
            await SaveListAsync(_animalsPath, list);
        }
        #endregion

        #region Vaccinations Auto-Generation
        public async Task AutoGenerateVaccinationsAsync(Batch batch)
        {
            var vacs = new List<Vaccination>();
            DateTime birth = batch.BirthDate;
            string categoryId = batch.CategoryId?.ToLower()?.Trim() ?? string.Empty;
            string purpose = batch.Purpose?.Trim() ?? string.Empty;

            if (categoryId == "porcinos")
            {
                // Auto-generate vaccination scheme for pigs
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Vacuna contra la Peste Porcina Clásica (Cólera Porcino)",
                    Type = "Vacuna",
                    Description = "Inmunización obligatoria oficial contra el Cólera Porcino (Cepa China). Vía Intramuscular (IM).",
                    Dose = 2.0,
                    DoseUnit = "ml",
                    ScheduledDate = birth.AddDays(45),
                    Status = "Pendiente",
                    Alternatives = "Pest-Vac / Cólera Porcino"
                });
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Vacuna contra Mycoplasma hyopneumoniae",
                    Type = "Vacuna",
                    Description = "Prevención de la neumonía enzoótica (Dosis 1). Vía Intramuscular (IM).",
                    Dose = 2.0,
                    DoseUnit = "ml",
                    ScheduledDate = birth.AddDays(21),
                    Status = "Pendiente",
                    Alternatives = "RespiSure / M+Pac"
                });
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Refuerzo contra Mycoplasma hyopneumoniae",
                    Type = "Refuerzo",
                    Description = "Segunda dosis para inmunidad protectora pulmonar. Vía Intramuscular (IM).",
                    Dose = 2.0,
                    DoseUnit = "ml",
                    ScheduledDate = birth.AddDays(42),
                    Status = "Pendiente",
                    Alternatives = "RespiSure / M+Pac"
                });
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Desparasitante (Ivermectina 1%)",
                    Type = "Desparasitación",
                    Description = "Control de parásitos gastrointestinales, pulmonares, sarna y piojos. Vía Subcutánea (SC).",
                    Dose = 1.0,
                    DoseUnit = "ml/33kg",
                    ScheduledDate = birth.AddDays(60),
                    Status = "Pendiente",
                    Alternatives = "Dectomax / Ivomec"
                });
            }
            else if (categoryId == "avicolas_engorde" || categoryId == "avicolas_postura")
            {
                // Avian scheme
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Vacuna contra Newcastle + Bronquitis",
                    Type = "Vacuna",
                    Description = "Dosis inicial protectora ocular/agua contra Newcastle y Bronquitis.",
                    Dose = 0.03,
                    DoseUnit = "ml",
                    ScheduledDate = birth.AddDays(7),
                    Status = "Pendiente",
                    Alternatives = "Nobilis / Poulvac"
                });
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Vacuna contra Viruela Aviar",
                    Type = "Vacuna",
                    Description = "Prevención de la Viruela Aviar. Vía Punción alar.",
                    Dose = 1.0,
                    DoseUnit = "dosis/ave",
                    ScheduledDate = birth.AddDays(35),
                    Status = "Pendiente",
                    Alternatives = "Poulvac Fowl Pox"
                });
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Multivitamínico AD3E + Calcio",
                    Type = "Vitamina",
                    Description = "Soporte vitamínico para el inicio del ciclo de puesta activa.",
                    Dose = 2.0,
                    DoseUnit = "g/L",
                    ScheduledDate = birth.AddDays(14),
                    Status = "Pendiente",
                    Alternatives = "Vitapoli / Promotor-L"
                });
            }
            else if (categoryId == "bovinos_leche" || categoryId == "bovinos_carne")
            {
                // Bovine scheme
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Vacuna contra la Fiebre Aftosa",
                    Type = "Vacuna",
                    Description = "Campaña obligatoria de vacunación contra la fiebre aftosa. Vía Subcutánea (SC) o Intramuscular (IM).",
                    Dose = 2.0,
                    DoseUnit = "ml",
                    ScheduledDate = birth.AddDays(15),
                    Status = "Pendiente",
                    Alternatives = "Aftogen / Aftosan"
                });
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Refuerzo Triple Clostridial",
                    Type = "Refuerzo",
                    Description = "Prevención de carbón sintomático, edema maligno y enterotoxemia. Vía Subcutánea (SC) en la tabla del cuello.",
                    Dose = 5.0,
                    DoseUnit = "ml",
                    ScheduledDate = birth.AddDays(45),
                    Status = "Pendiente",
                    Alternatives = "Covexin 8 / Tasvax"
                });
            }
            else
            {
                // Generic scheme
                vacs.Add(new Vaccination
                {
                    Id = Guid.NewGuid().ToString(),
                    BatchId = batch.Id,
                    BatchName = batch.Name,
                    Name = "Desparasitante de Amplio Espectro",
                    Type = "Desparasitación",
                    Description = "Tratamiento preventivo básico contra parásitos internos. Vía Oral.",
                    Dose = 1.0,
                    DoseUnit = "ml/10kg",
                    ScheduledDate = birth.AddDays(30),
                    Status = "Pendiente",
                    Alternatives = "Panacur / Albendazol"
                });
            }

            foreach (var v in vacs)
            {
                if (v.ScheduledDate < DateTime.Today)
                {
                    v.Status = "Aplicada";
                    v.AppliedDate = v.ScheduledDate;
                    v.Notes = "Aplicada automáticamente según calendario histórico.";
                }
                await SaveVaccinationAsync(v);
            }
        }
        #endregion
    }
}
