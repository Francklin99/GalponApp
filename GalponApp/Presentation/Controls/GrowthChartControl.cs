using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using GalponApp.Domain.Models;

namespace GalponApp.Presentation.Controls
{
    public class GrowthChartControl : GraphicsView, IDrawable
    {
        public static readonly BindableProperty WeightLogsProperty =
            BindableProperty.Create(nameof(WeightLogs), typeof(IList<WeightLog>), typeof(GrowthChartControl), null,
                propertyChanged: (bindable, oldValue, newValue) => ((GrowthChartControl)bindable).Invalidate());

        public IList<WeightLog> WeightLogs
        {
            get => (IList<WeightLog>)GetValue(WeightLogsProperty);
            set => SetValue(WeightLogsProperty, value);
        }

        public static readonly BindableProperty AccentColorProperty =
            BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(GrowthChartControl), Colors.Green);

        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        public GrowthChartControl()
        {
            Drawable = this;
            HeightRequest = 180;
            HorizontalOptions = LayoutOptions.Fill;
            BackgroundColor = Colors.Transparent;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Antialias = true;

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            float paddingLeft = 40f;
            float paddingRight = 20f;
            float paddingTop = 20f;
            float paddingBottom = 30f;

            float chartWidth = width - paddingLeft - paddingRight;
            float chartHeight = height - paddingTop - paddingBottom;

            // Dibujar fondo limpio y sutil
            canvas.FillColor = Color.FromArgb("#F9FBF9"); // Soft background tint
            canvas.FillRoundedRectangle(dirtyRect, 8);

            if (WeightLogs == null || WeightLogs.Count == 0)
            {
                canvas.FontColor = Colors.Gray;
                canvas.FontSize = 14;
                canvas.DrawString("No hay registros de peso disponibles", 0, height / 2 - 15, width, 30, HorizontalAlignment.Center, VerticalAlignment.Center);
                return;
            }

            var sortedLogs = WeightLogs.OrderBy(w => w.Date).ToList();
            
            // Buscar mínimos y máximos para el escalado
            double minWeight = sortedLogs.Min(w => w.AverageWeight);
            double maxWeight = sortedLogs.Max(w => w.AverageWeight);
            
            // Margen para no tocar los bordes del gráfico
            minWeight = Math.Max(0, minWeight - (maxWeight - minWeight) * 0.15);
            maxWeight = maxWeight + (maxWeight - minWeight) * 0.15;
            
            if (Math.Abs(maxWeight - minWeight) < 0.001)
            {
                maxWeight += 10;
                minWeight = Math.Max(0, minWeight - 10);
            }

            DateTime minDate = sortedLogs.Min(w => w.Date);
            DateTime maxDate = sortedLogs.Max(w => w.Date);
            double totalDays = (maxDate - minDate).TotalDays;
            if (totalDays < 1) totalDays = 1;

            // 1. Dibujar líneas de cuadrícula horizontales
            canvas.StrokeColor = Color.FromArgb("#E2E8F0");
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 };
            int gridLines = 4;
            for (int i = 0; i <= gridLines; i++)
            {
                float ratio = (float)i / gridLines;
                float y = paddingTop + chartHeight * (1 - ratio);
                canvas.DrawLine(paddingLeft, y, width - paddingRight, y);

                // Etiquetas del eje Y
                double weightValue = minWeight + (maxWeight - minWeight) * ratio;
                canvas.FontColor = Color.FromArgb("#64748B");
                canvas.FontSize = 10;
                canvas.DrawString($"{weightValue:F0}kg", 0, y - 10, paddingLeft - 5, 20, HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // Quitar el patrón de guiones para el resto del dibujo
            canvas.StrokeDashPattern = null;

            // 2. Mapear puntos a coordenadas en pantalla
            var points = new List<PointF>();
            foreach (var log in sortedLogs)
            {
                double daysFromMin = (log.Date - minDate).TotalDays;
                float x = paddingLeft + (float)(daysFromMin / totalDays * chartWidth);
                float y = paddingTop + (float)((1 - (log.AverageWeight - minWeight) / (maxWeight - minWeight)) * chartHeight);
                points.Add(new PointF(x, y));
            }

            // 3. Rellenar área debajo de la curva (Gradiente sutil)
            if (points.Count > 1)
            {
                var fillPath = new PathF();
                fillPath.MoveTo(paddingLeft, paddingTop + chartHeight); // Esquina inferior izquierda
                fillPath.LineTo(points[0].X, points[0].Y);
                for (int i = 1; i < points.Count; i++)
                {
                    fillPath.LineTo(points[i].X, points[i].Y);
                }
                fillPath.LineTo(points[^1].X, paddingTop + chartHeight); // Esquina inferior derecha
                fillPath.Close();

                canvas.SaveState();
                // Simulación de degradado pintando un color transparente
                canvas.FillColor = Color.FromRgba(AccentColor.Red, AccentColor.Green, AccentColor.Blue, 0.12f);
                canvas.FillPath(fillPath);
                canvas.RestoreState();
            }

            // 4. Dibujar la línea principal de la curva de crecimiento
            canvas.StrokeColor = AccentColor;
            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y);
            }

            // 5. Dibujar los puntos (círculos) e indicadores de fechas
            canvas.FillColor = Colors.White;
            canvas.StrokeSize = 2;
            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                var log = sortedLogs[i];

                // Círculo del punto
                canvas.StrokeColor = AccentColor;
                canvas.FillCircle(pt.X, pt.Y, 4);
                canvas.DrawCircle(pt.X, pt.Y, 4);

                // Etiquetas del eje X (Fechas)
                // Para evitar superposiciones, mostramos el primero, el del medio (opcional) y el último
                if (i == 0 || i == points.Count - 1 || (points.Count > 2 && i == points.Count / 2))
                {
                    string dateStr = log.Date.ToString("dd MMM");
                    canvas.FontColor = Color.FromArgb("#475569");
                    canvas.FontSize = 9;
                    canvas.DrawString(dateStr, pt.X - 50, height - 25, 100, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
                }

                // Mostrar el peso encima del último punto
                if (i == points.Count - 1)
                {
                    canvas.FontColor = AccentColor;
                    canvas.FontSize = 11;
                    canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                    canvas.DrawString($"{log.AverageWeight:F1} kg", pt.X - 50, pt.Y - 25, 100, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
        }
    }
}
