using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GalponApp.Domain.Models;

namespace GalponApp.Infrastructure.Services
{
    public class ReportService
    {
        // Genera un archivo CSV con todo el historial del lote y retorna la ruta completa del archivo
        public async Task<string> GenerateBatchReportCsvAsync(Batch batch, List<WeightLog> weights, List<Vaccination> vaccinations, List<SanitaryRecord> sanitary)
        {
            var sb = new StringBuilder();

            // 1. Cabecera e Información General del Lote
            sb.AppendLine("REPORTE DE LOTE DE ANIMALES - GALPONAPP");
            sb.AppendLine($"Fecha de Generación: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine("INFORMACION GENERAL");
            sb.AppendLine("ID Lote,Nombre,Categoría,Raza,Propósito,Cantidad Inicial,Cantidad Actual,Mortalidad,Tasa Mortalidad,Fecha Nacimiento,Edad (Semanas),Peso Inicial (kg),Peso Actual (kg),Estado Sanitario");
            
            double mortalityRate = batch.InitialQuantity > 0 ? ((double)batch.MortalityCount / batch.InitialQuantity) * 100 : 0;
            sb.AppendLine($"\"{batch.Id}\",\"{batch.Name}\",\"{batch.CategoryName}\",\"{batch.Breed}\",\"{batch.Purpose}\",{batch.InitialQuantity},{batch.Quantity},{batch.MortalityCount},{mortalityRate:F1}%,{batch.BirthDate:dd/MM/yyyy},{batch.AgeInWeeks},{batch.InitialWeight:F2},{batch.CurrentWeight:F2},\"{batch.SanitaryStatus}\"");
            
            sb.AppendLine();
            sb.AppendLine("HISTORIAL DE PESOS Y CRECIMIENTO");
            sb.AppendLine("Fecha,Peso Promedio (kg),Ganancia de Peso (kg),Tamaño Promedio (cm),Bajas en Periodo,Notas");
            
            double lastWeight = batch.InitialWeight;
            foreach (var w in weights)
            {
                double gain = w.AverageWeight - lastWeight;
                sb.AppendLine($"{w.Date:dd/MM/yyyy},{w.AverageWeight:F2},{gain:F2},{w.AverageSize:F1},{w.MortalityCount},\"{w.Notes.Replace("\"", "\"\"")}\"");
                lastWeight = w.AverageWeight;
            }

            sb.AppendLine();
            sb.AppendLine("CALENDARIO SANITARIO Y VACUNAS");
            sb.AppendLine("Fecha Programada,Fecha Aplicada,Nombre,Tipo,Dosis,Estado,Notas");
            foreach (var v in vaccinations)
            {
                string appliedDateStr = v.AppliedDate.HasValue ? v.AppliedDate.Value.ToString("dd/MM/yyyy") : "-";
                string name = v.HasCustomAppliedDose ? $"{v.Name} (Alternativo: {v.SavedCustomMedicationName})" : v.Name;
                string dose = v.HasCustomAppliedDose ? v.SavedCustomDoseAmount : $"{v.Dose} {v.DoseUnit}";
                sb.AppendLine($"{v.ScheduledDate:dd/MM/yyyy},{appliedDateStr},\"{name}\",\"{v.Type}\",\"{dose}\",\"{v.Status}\",\"{v.Notes.Replace("\"", "\"\"")}\"");
            }

            sb.AppendLine();
            sb.AppendLine("CONTROLES SANITARIOS Y ENFERMEDADES");
            sb.AppendLine("Fecha Inicio,Fecha Fin,Diagnóstico,Afectados,Tratamiento,Medicamento,Dosis,Costo (USD),Estado,Notas");
            foreach (var s in sanitary)
            {
                string endDateStr = s.EndDate.HasValue ? s.EndDate.Value.ToString("dd/MM/yyyy") : "-";
                sb.AppendLine($"{s.StartDate:dd/MM/yyyy},{endDateStr},\"{s.Diagnosis}\",{s.AffectedCount},\"{s.Treatment}\",\"{s.Medication}\",\"{s.Dose}\",{s.Cost:F2},\"{s.Status}\",\"{s.Notes.Replace("\"", "\"\"")}\"");
            }

            string fileName = $"Reporte_Lote_{batch.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }

        // Genera un archivo HTML estéticamente impecable que sirve como reporte imprimible (PDF)
        public async Task<string> GenerateBatchReportHtmlAsync(Batch batch, List<WeightLog> weights, List<Vaccination> vaccinations, List<SanitaryRecord> sanitary)
        {
            var sb = new StringBuilder();
            
            double mortalityRate = batch.InitialQuantity > 0 ? ((double)batch.MortalityCount / batch.InitialQuantity) * 100 : 0;
            double weightGain = batch.CurrentWeight - batch.InitialWeight;

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine($"<title>Reporte de Lote - {batch.Name}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; color: #333333; margin: 40px; line-height: 1.5; background-color: #fafafa; }");
            sb.AppendLine(".container { max-width: 900px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); }");
            sb.AppendLine(".header-table { width: 100%; border-collapse: collapse; margin-bottom: 30px; }");
            sb.AppendLine(".header-title { font-size: 28px; font-weight: bold; color: #2e7d32; margin: 0; }");
            sb.AppendLine(".header-meta { text-align: right; color: #666666; font-size: 14px; }");
            sb.AppendLine("h2 { color: #8d6e63; border-bottom: 2px solid #e0e0e0; padding-bottom: 8px; margin-top: 40px; font-size: 18px; text-transform: uppercase; letter-spacing: 0.5px; }");
            
            // Grid layout
            sb.AppendLine(".grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }");
            sb.AppendLine(".card { background-color: #f5f5f5; padding: 15px; border-radius: 8px; border-left: 4px solid #2e7d32; }");
            sb.AppendLine(".card h3 { margin: 0 0 10px 0; color: #333; font-size: 15px; }");
            sb.AppendLine(".card-item { margin-bottom: 8px; font-size: 14px; }");
            sb.AppendLine(".card-item span { font-weight: bold; color: #555; }");
            
            // Table styles
            sb.AppendLine("table.data-table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 14px; }");
            sb.AppendLine("table.data-table th { background-color: #e8f5e9; color: #2e7d32; text-align: left; padding: 12px 10px; font-weight: 600; border-bottom: 2px solid #c8e6c9; }");
            sb.AppendLine("table.data-table td { padding: 10px; border-bottom: 1px solid #eeeeee; }");
            sb.AppendLine("table.data-table tr:nth-child(even) { background-color: #fbfbfb; }");
            
            // Badges
            sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: bold; text-transform: uppercase; }");
            sb.AppendLine(".badge-applied { background-color: #d4edda; color: #155724; }");
            sb.AppendLine(".badge-pending { background-color: #fff3cd; color: #856404; }");
            sb.AppendLine(".badge-overdue { background-color: #f8d7da; color: #721c24; }");
            
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");
            
            // Header
            sb.AppendLine("<table class='header-table'>");
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td><div class='header-title'>GalponApp</div><div style='color:#777; font-size:14px;'>Sistema de Control Agropecuario Inteligente</div></td>");
            sb.AppendLine($"<td class='header-meta'><strong>REPORTE DETALLADO DE LOTE</strong><br>Fecha: {DateTime.Now:dd/MM/yyyy}<br>ID: {batch.Id[..8].ToUpper()}</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");
            
            // General info section
            sb.AppendLine("<h2>Información General del Lote</h2>");
            sb.AppendLine("<div class='grid-2'>");
            
            // Card 1
            sb.AppendLine("<div class='card'>");
            sb.AppendLine($"<h3>{batch.Name}</h3>");
            sb.AppendLine($"<div class='card-item'><span>Especie:</span> {batch.CategoryName}</div>");
            sb.AppendLine($"<div class='card-item'><span>Raza:</span> {batch.Breed}</div>");
            sb.AppendLine($"<div class='card-item'><span>Propósito Comercial:</span> {batch.Purpose}</div>");
            sb.AppendLine($"<div class='card-item'><span>Fecha Nacimiento:</span> {batch.BirthDate:dd/MM/yyyy} ({batch.AgeInWeeks} semanas)</div>");
            sb.AppendLine("</div>");

            // Card 2
            sb.AppendLine("<div class='card' style='border-left-color: #8d6e63;'>");
            sb.AppendLine("<h3>Población y Crecimiento</h3>");
            sb.AppendLine($"<div class='card-item'><span>Cantidad Inicial:</span> {batch.InitialQuantity} animales</div>");
            sb.AppendLine($"<div class='card-item'><span>Cantidad Actual:</span> {batch.Quantity} animales</div>");
            sb.AppendLine($"<div class='card-item'><span>Mortalidad:</span> {batch.MortalityCount} ({mortalityRate:F1}%)</div>");
            sb.AppendLine($"<div class='card-item'><span>Rendimiento Peso:</span> Inicial {batch.InitialWeight}kg | Actual {batch.CurrentWeight}kg (+{weightGain:F1}kg)</div>");
            sb.AppendLine($"<div class='card-item'><span>Código QR:</span> {batch.QRCode}</div>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("</div>");

            // Weights
            sb.AppendLine("<h2>Historial de Pesos y Ganancia de Masa</h2>");
            if (weights.Count == 0)
            {
                sb.AppendLine("<p style='font-style: italic; color:#777;'>No se han registrado controles de peso para este lote.</p>");
            }
            else
            {
                sb.AppendLine("<table class='data-table'>");
                sb.AppendLine("<thead><tr><th>Fecha</th><th>Edad (Semanas)</th><th>Peso Promedio (kg)</th><th>Ganancia del Periodo</th><th>Tamaño Promedio</th><th>Bajas</th><th>Observaciones</th></tr></thead>");
                sb.AppendLine("<tbody>");
                double prevWeight = batch.InitialWeight;
                foreach (var w in weights)
                {
                    int ageWeeks = (w.Date.Date - batch.BirthDate.Date).Days / 7;
                    double gain = w.AverageWeight - prevWeight;
                    string gainStr = gain >= 0 ? $"+{gain:F2} kg" : $"{gain:F2} kg";
                    sb.AppendLine($"<tr><td>{w.Date:dd/MM/yyyy}</td><td>Semana {ageWeeks}</td><td>{w.AverageWeight:F2} kg</td><td>{gainStr}</td><td>{(w.AverageSize > 0 ? w.AverageSize.ToString("F1") + " cm" : "-")}</td><td>{w.MortalityCount}</td><td>{w.Notes}</td></tr>");
                    prevWeight = w.AverageWeight;
                }
                sb.AppendLine("</tbody></table>");
            }

            // Vaccinations
            sb.AppendLine("<h2>Calendario Sanitario y Vacunas</h2>");
            if (vaccinations.Count == 0)
            {
                sb.AppendLine("<p style='font-style: italic; color:#777;'>No hay vacunas programadas en el lote.</p>");
            }
            else
            {
                sb.AppendLine("<table class='data-table'>");
                sb.AppendLine("<thead><tr><th>Nombre</th><th>Tipo</th><th>Dosis</th><th>Fecha Programada</th><th>Fecha Aplicada</th><th>Estado</th><th>Notas</th></tr></thead>");
                sb.AppendLine("<tbody>");
                foreach (var v in vaccinations)
                {
                    string appliedStr = v.AppliedDate.HasValue ? v.AppliedDate.Value.ToString("dd/MM/yyyy") : "-";
                    string statusClass = "badge-pending";
                    if (v.Status == "Aplicada") statusClass = "badge-applied";
                    else if (v.Status == "Atrasada") statusClass = "badge-overdue";
                    
                    string name = v.HasCustomAppliedDose ? $"{v.Name} (Alternativo: {v.SavedCustomMedicationName})" : v.Name;
                    string dose = v.HasCustomAppliedDose ? v.SavedCustomDoseAmount : $"{v.Dose} {v.DoseUnit}";
                    
                    sb.AppendLine($"<tr><td><strong>{name}</strong></td><td>{v.Type}</td><td>{dose}</td><td>{v.ScheduledDate:dd/MM/yyyy}</td><td>{appliedStr}</td><td><span class='badge {statusClass}'>{v.Status}</span></td><td>{v.Notes}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            // Sanitary records
            sb.AppendLine("<h2>Controles Médicos e Historial Clínico</h2>");
            if (sanitary.Count == 0)
            {
                sb.AppendLine("<p style='font-style: italic; color:#777;'>No se registran eventos de enfermedades o tratamientos sanitarios.</p>");
            }
            else
            {
                sb.AppendLine("<table class='data-table'>");
                sb.AppendLine("<thead><tr><th>Diagnóstico</th><th>Cantidad Afectados</th><th>Fecha Inicio</th><th>Tratamiento</th><th>Medicamento / Dosis</th><th>Costo</th><th>Aislamiento</th><th>Estado</th></tr></thead>");
                sb.AppendLine("<tbody>");
                foreach (var s in sanitary)
                {
                    sb.AppendLine($"<tr><td><strong>{s.Diagnosis}</strong></td><td>{s.AffectedCount} cabezas</td><td>{s.StartDate:dd/MM/yyyy}</td><td>{s.Treatment}</td><td>{s.Medication}<br><small style='color:#777;'>{s.Dose}</small></td><td>${s.Cost:F2} USD</td><td>{(s.IsIsolated ? "Sí (Quarentena)" : "No")}</td><td>{s.Status}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            string fileName = $"Reporte_Lote_{batch.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }
    }
}
