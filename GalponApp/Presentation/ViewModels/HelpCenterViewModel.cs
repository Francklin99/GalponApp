using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalponApp.Domain.Models;

namespace GalponApp.Presentation.ViewModels
{
    public partial class HelpCenterViewModel : BaseViewModel
    {
        private readonly List<Disease> _allDiseases = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        [ObservableProperty]
        private string selectedCategory = "Todos";

        partial void OnSelectedCategoryChanged(string value)
        {
            ApplyFilters();
        }

        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<Disease> FilteredDiseases { get; } = new();

        public HelpCenterViewModel()
        {
            Title = "Centro de Ayuda Sanitaria";

            Categories.Add("Todos");
            Categories.Add("Porcinos");
            Categories.Add("Bovinos");
            Categories.Add("Aves");
            Categories.Add("Ovinos / Caprinos");
            Categories.Add("Cunicultura");

            SeedDiseases();
            ApplyFilters();
        }

        private void SeedDiseases()
        {
            // 🐷 PORCINOS
            _allDiseases.Add(new Disease
            {
                Name = "Neumonía Enzoótica (Micoplasmosis Porcina)",
                Category = "Porcinos",
                Symptoms = "Tos seca y persistente (especialmente al moverse), respiración acelerada y dificultosa, retraso severo en el crecimiento y pérdida de condición corporal.",
                WhatToDo = "Aislar inmediatamente a los animales enfermos en un galpón bien ventilado pero libre de corrientes de aire frío. Mantener la cama limpia y seca. Evitar el hacinamiento.",
                Treatment = "Tilosina (ampollas de Tylan 200: 1 ml por cada 20 kg de peso vía intramuscular durante 3 días) o Enrofloxacina al 10% (1 ml por cada 40 kg de peso).",
                AlternativeTreatment = "Florfenicol (1 ml por cada 15 kg vía intramuscular en dos dosis separadas por 48 horas) o Espiramicina.",
                FeedingGuidance = "Suministrar papilla templada fácil de digerir. Incrementar las vitaminas en el agua de bebida (Complejo B y Vitamina C) para ayudar a la recuperación pulmonar.",
                Icon = "🐷"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Peste Porcina Clásica (Cólera Porcino)",
                Category = "Porcinos",
                Symptoms = "Fiebre muy alta (41-42°C), decaimiento extremo, amontonamiento de animales, manchas moradas/rojas en la piel (abdomen, orejas), diarrea y conjuntivitis con secreción.",
                WhatToDo = "Alerta Sanitaria. Aislar y poner bajo estricta cuarentena. Notificar de inmediato a las autoridades veterinarias oficiales. Es altamente contagioso y mortal.",
                Treatment = "No existe tratamiento curativo efectivo (es viral). El control se basa puramente en vacunación preventiva obligatoria. Aplicar antipiréticos y suero oral para confort.",
                AlternativeTreatment = "Complejos vitamínicos inyectables y antibióticos de amplio espectro únicamente para prevenir infecciones bacterianas secundarias en animales expuestos.",
                FeedingGuidance = "Suministrar agua fresca con electrolitos. Si los animales intentan comer, ofrecer raciones húmedas ligeras enriquecidas con aminoácidos y alta energía.",
                Icon = "🐷"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Colibacilosis (Diarrea por E. Coli)",
                Category = "Porcinos",
                Symptoms = "Diarrea acuosa de color amarillo-cremoso o blanquecino en lechones, deshidratación rápida (ojos hundidos, piel reseca), debilidad generalizada y letargo.",
                WhatToDo = "Mantener una fuente de calor constante para los lechones (foco/calefactor). Proporcionar rehidratación oral constante con suero. Desinfectar todo el corral.",
                Treatment = "Colistina en solución oral o Gentamicina inyectable (1 ml por cada 10 kg de peso vía intramuscular durante 3 a 5 días).",
                AlternativeTreatment = "Trimetoprima-Sulfametoxazol (TMP/Sulfa) por vía oral o Enrofloxacina oral para lechones destetados.",
                FeedingGuidance = "Retirar el alimento concentrado pesado por 24 horas. Ofrecer suero electrolítico tibio y luego reintroducir papillas de pre-iniciador mezcladas con probióticos.",
                Icon = "🐷"
            });

            // 🐄 BOVINOS
            _allDiseases.Add(new Disease
            {
                Name = "Mastitis Bovina (Infección de la Ubre)",
                Category = "Bovinos",
                Symptoms = "Cuarto de la ubre hinchado, enrojecido, caliente y adolorido. Leche con grumos, coágulos, aspecto acuoso o presencia de sangre. Fiebre en casos graves.",
                WhatToDo = "Realizar ordeño completo y frecuente del cuarto afectado (mínimo 3 veces al día) para eliminar toxinas. Desinfectar los pezones con sellador después de cada ordeño.",
                Treatment = "Antibióticos intramamarios (jeringas intramamarias de Cefalexina o Ampicilina + Cloxacilina) introducidas por el pezón afectado después del ordeño de escurrido.",
                AlternativeTreatment = "Terapia inyectable sistémica con Penicilina + Estreptomicina (1 ml por cada 20 kg vía intramuscular profunda) en casos de mastitis clínica grave con fiebre.",
                FeedingGuidance = "Proporcionar abundante agua fresca y limpia. Incrementar el consumo de forraje fibroso de alta calidad (heno). Disminuir la cantidad de concentrado proteico.",
                Icon = "🐄"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Fiebre Aftosa (Aftas Bucales y de Pezuña)",
                Category = "Bovinos",
                Symptoms = "Babeo constante y espumoso, ampollas (aftas) en la lengua, encías, pezones y pezuñas. Cojera severa que impide caminar. Fiebre y disminución abrupta de leche.",
                WhatToDo = "Cuarentena estricta del predio. Aislar al ganado afectado en zonas secas y limpias. Notificar obligatoriamente a la autoridad sanitaria nacional.",
                Treatment = "No existe cura viral. Tratamiento paliativo: lavado bucal con soluciones ácidas o bicarbonato al 4%, y aplicación de antisépticos (azul de metileno) y cicatrizantes en pezuñas.",
                AlternativeTreatment = "Antiinflamatorios no esteroideos inyectables (Flunixin Meglumine: 2 ml por cada 100 kg de peso) para aliviar el dolor bucal y de pezuñas, y antibióticos locales.",
                FeedingGuidance = "Suministrar forraje verde picado muy fino y tierno, o silaje suave. Evitar heno seco que lastime las llagas de la boca. Agua templada a disposición.",
                Icon = "🐄"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Timpanismo (Meteorismo o Hinchazón Ruminar)",
                Category = "Bovinos",
                Symptoms = "Abombamiento pronunciado en el flanco izquierdo (vientre inflado), dificultad respiratoria severa, inquietud (patearse el vientre), quejidos y colapso rápido.",
                WhatToDo = "Mantener al animal en movimiento (caminar). Si es asfixia inminente, usar un trócar o aguja gruesa para perforar el rumen en el flanco izquierdo. Colocar sonda esofágica.",
                Treatment = "Agentes antiespumantes vía oral (Tympanol o Bloat Guard: 100 ml disueltos en agua) o Aceite mineral (1 a 2 litros) para deshacer la espuma ruminal.",
                AlternativeTreatment = "Disolución oral de agua tibia con jabón neutro de cocina (50 gramos en un litro) o bicarbonato de sodio (100 gramos en un litro) en emergencias.",
                FeedingGuidance = "Retirar de forma inmediata del pastoreo en leguminosas tiernas (alfalfa, trébol húmedo). Ofrecer únicamente heno seco de gramíneas por las siguientes 48 horas.",
                Icon = "🐄"
            });

            // 🐔 AVES
            _allDiseases.Add(new Disease
            {
                Name = "Coccidiosis Aviar",
                Category = "Aves",
                Symptoms = "Plumas erizadas, alas caídas, palidez en la cresta y barbillas, diarrea oscura con sangre o mucosidad, deshidratación, letargo y alta tasa de mortalidad.",
                WhatToDo = "Cambiar y retirar la cama húmeda por completo. Mantener los comederos limpios y secos para romper el ciclo del parásito. Evitar la humedad ambiental excesiva.",
                Treatment = "Amprolio soluble en el agua de bebida (1.2 gramos por litro durante 5-7 días) o Toltrazuril al 2.5% (1 ml por litro de agua durante 2 días seguidos).",
                AlternativeTreatment = "Sulfametacina o Sulfaquinoxalina soluble (aplicar según indicaciones del empaque en intervalos de 3 días con agua limpia de intermedio).",
                FeedingGuidance = "Suministrar alimento con coccidiostato preventivo. Adicionar vitamina K3 (coagulante para frenar hemorragias intestinales) y vitamina A para regeneración epitelial.",
                Icon = "🐔"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Coriza Infecciosa Aviar",
                Category = "Aves",
                Symptoms = "Hinchazón de la cara y alrededor de los ojos (edema facial), secreción nasal y ocular pegajosa y con olor fétido, estornudos, ronquera y caída en la postura.",
                WhatToDo = "Aislar a las aves enfermas. Reducir las corrientes de aire directas en el galpón. Limpiar y desinfectar bebederos diariamente con cloro o yodo.",
                Treatment = "Sulfatrimetoprima soluble en el agua (1.5 ml por litro de agua durante 5 días) o Enrofloxacina soluble (1 ml al 10% por cada litro de agua).",
                AlternativeTreatment = "Oxitetraciclina soluble en agua o inyecciones individuales de Estreptomicina (100-200 mg por ave vía intramuscular en la pechuga).",
                FeedingGuidance = "Agregar complejos multivitamínicos (Vitamina A, D3, E) en el agua para estimular el sistema inmune. Ofrecer alimento molido húmedo para facilitar la deglución.",
                Icon = "🐔"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Enfermedad de Newcastle",
                Category = "Aves",
                Symptoms = "Dificultad respiratoria, tos, estornudos, diarrea verdosa acuosa, signos nerviosos (cuello torcido, caminar en círculos, parálisis de patas/alas), muerte súbita.",
                WhatToDo = "Es una enfermedad viral altamente contagiosa. Aislar el galpón. Vacunar preventivamente a los lotes sanos vecinos. Enterrar o cremar cadáveres.",
                Treatment = "No tiene cura. El tratamiento es de soporte: rehidratantes con electrolitos y vitaminas en el agua de bebida para disminuir la mortalidad y estrés.",
                AlternativeTreatment = "Antibióticos solubles de amplio espectro (como Fosfomicina o Tilvalosina) para controlar infecciones respiratorias bacterianas secundarias.",
                FeedingGuidance = "Ofrecer alimento con alta densidad calórica y probióticos para reestablecer la microbiota intestinal devastada por la diarrea.",
                Icon = "🐔"
            });

            // 🐑 OVINOS / CAPRINOS
            _allDiseases.Add(new Disease
            {
                Name = "Ectima Contagioso (Boquera)",
                Category = "Ovinos / Caprinos",
                Symptoms = "Ampollas que se convierten en costras gruesas y oscuras alrededor de los labios, nariz, párpados y, en madres lactantes, en los pezones de la ubre.",
                WhatToDo = "Zoonosis (se contagia a humanos). Usar guantes estrictamente para manipular a los animales. Separar a las crías y madres infectadas para evitar rechazo.",
                Treatment = "Limpieza local de las costras y aplicación de violeta de genciana o sprays cicatrizantes con antibiótico (Oxitetraciclina en aerosol/Terramicina spray).",
                AlternativeTreatment = "Aplicación de glicerina yodada al 10% preparada localmente sobre las lesiones labiales cada 48 horas.",
                FeedingGuidance = "Proporcionar forraje verde, pasto tierno picado o papillas suaves de afrechillo húmedo. Evitar dar tallos secos o espinosos que lastimen la boca.",
                Icon = "🐑"
            });

            _allDiseases.Add(new Disease
            {
                Name = "Fiebre de Leche (Hipocalcemia)",
                Category = "Ovinos / Caprinos",
                Symptoms = "Debilidad muscular progresiva cerca al parto, incapacidad para levantarse (postración), temblores, pupilas dilatadas, respiración lenta y cuello torcido.",
                WhatToDo = "Colocar al animal apoyado sobre su pecho para evitar timpanismo por gases. Aplicar calor corporal con mantas. Tratar de inmediato para evitar la muerte.",
                Treatment = "Inyección subcutánea de Gluconato de Calcio al 20% (50 a 100 ml por animal, calentado a temperatura corporal y repartido en 2 o 3 sitios de inyección).",
                AlternativeTreatment = "Calcio oral en pasta/gel únicamente si el animal está consciente y conserva el reflejo de deglución de forma normal.",
                FeedingGuidance = "Después de la recuperación, proveer una dieta rica en heno de buena calidad con suplemento mineral equilibrado en calcio y fósforo.",
                Icon = "🐑"
            });

            // 🐇 CUNICULTURA
            _allDiseases.Add(new Disease
            {
                Name = "Coccidiosis de los Conejos",
                Category = "Cunicultura",
                Symptoms = "Diarrea con moco o sangre, vientre hinchado y blando, deshidratación extrema, pérdida de apetito, debilidad en patas traseras y retraso en el crecimiento.",
                WhatToDo = "Limpieza y desinfección a fondo de las jaulas y comederos con vapor o desinfectantes específicos. Evitar el contacto de las heces con el alimento.",
                Treatment = "Sulfaquinoxalina soluble (1.5 ml por litro de agua durante 3 días, descansar 2 días y repetir otros 3) o Toltrazuril (0.5 ml por litro durante 2 días).",
                AlternativeTreatment = "Sulfadimetoxina soluble en el agua de bebida o medicada en el concentrado comercial.",
                FeedingGuidance = "Retirar alimento verde o verduras húmedas. Ofrecer exclusivamente heno de alfalfa o gramíneas seco y limpio, e incrementar la ventilación.",
                Icon = "🐇"
            });
        }

        [RelayCommand]
        public void SelectCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return;
            SelectedCategory = category;
            ApplyFilters();
        }

        [RelayCommand]
        public void ToggleDisease(Disease disease)
        {
            if (disease == null) return;
            disease.IsExpanded = !disease.IsExpanded;
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void ApplyFilters()
        {
            var filtered = _allDiseases.AsEnumerable();

            // Filter by Category
            if (SelectedCategory != "Todos")
            {
                string matchCat = SelectedCategory.Replace(" ", "").ToLower();
                filtered = filtered.Where(d => {
                    string dCat = d.Category.Replace(" ", "").ToLower();
                    if (matchCat == "ovinos/caprinos")
                    {
                        return dCat.Contains("ovino") || dCat.Contains("caprino");
                    }
                    return dCat == matchCat;
                });
            }

            // Filter by Search Text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string query = SearchText.ToLower().Trim();
                filtered = filtered.Where(d =>
                    d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    d.Symptoms.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    d.Treatment.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    d.AlternativeTreatment.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    d.FeedingGuidance.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    d.WhatToDo.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    d.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            FilteredDiseases.Clear();
            foreach (var d in filtered)
            {
                FilteredDiseases.Add(d);
            }
        }
    }
}
