using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace GalponApp.Presentation.Controls
{
    public class CircularProgressControl : GraphicsView, IDrawable
    {
        public static readonly BindableProperty ProgressProperty =
            BindableProperty.Create(nameof(Progress), typeof(double), typeof(CircularProgressControl), 0.0,
                propertyChanged: (bindable, oldValue, newValue) => ((CircularProgressControl)bindable).Invalidate());

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, Math.Clamp(value, 0.0, 1.0));
        }

        public static readonly BindableProperty ProgressColorProperty =
            BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(CircularProgressControl), Colors.Green);

        public Color ProgressColor
        {
            get => (Color)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public static readonly BindableProperty StrokeWidthProperty =
            BindableProperty.Create(nameof(StrokeWidth), typeof(float), typeof(CircularProgressControl), 10f,
                propertyChanged: (bindable, oldValue, newValue) => ((CircularProgressControl)bindable).Invalidate());

        public float StrokeWidth
        {
            get => (float)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public static readonly BindableProperty CenterTextProperty =
            BindableProperty.Create(nameof(CenterText), typeof(string), typeof(CircularProgressControl), string.Empty,
                propertyChanged: (bindable, oldValue, newValue) => ((CircularProgressControl)bindable).Invalidate());

        public string CenterText
        {
            get => (string)GetValue(CenterTextProperty);
            set => SetValue(CenterTextProperty, value);
        }

        public static readonly BindableProperty CenterSubtextProperty =
            BindableProperty.Create(nameof(CenterSubtext), typeof(string), typeof(CircularProgressControl), string.Empty,
                propertyChanged: (bindable, oldValue, newValue) => ((CircularProgressControl)bindable).Invalidate());

        public string CenterSubtext
        {
            get => (string)GetValue(CenterSubtextProperty);
            set => SetValue(CenterSubtextProperty, value);
        }

        public CircularProgressControl()
        {
            Drawable = this;
            HeightRequest = 100;
            WidthRequest = 100;
            BackgroundColor = Colors.Transparent;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Antialias = true;

            float cx = dirtyRect.Width / 2;
            float cy = dirtyRect.Height / 2;
            float radius = Math.Min(cx, cy) - StrokeWidth / 2 - 4f;

            if (radius <= 0) return;

            // 1. Dibujar círculo de fondo (color de fondo de la barra)
            canvas.StrokeColor = Color.FromArgb("#F1F5F9"); // Slate 100
            canvas.StrokeSize = StrokeWidth;
            canvas.DrawCircle(cx, cy, radius);

            // 2. Dibujar arco de progreso
            if (Progress > 0)
            {
                canvas.StrokeColor = ProgressColor;
                canvas.StrokeSize = StrokeWidth;
                canvas.StrokeLineCap = LineCap.Round;

                // En MAUI, DrawArc funciona especificando un rectángulo delimitador.
                // startAngle de 90 es arriba (12 en punto) si se incrementa en sentido antihorario,
                // pero si es horario, restamos.
                // Para asegurarnos que se vea bien en todas las plataformas:
                // startAngle = 90 (arriba), sweepAngle = -360 * Progress (antihorario hacia la derecha)
                float startAngle = 90f;
                float sweepAngle = -360f * (float)Progress;
                
                canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, startAngle + sweepAngle, clockwise: false, closed: false);
            }

            // 3. Dibujar textos en el centro
            string mainText = string.IsNullOrEmpty(CenterText) ? $"{(Progress * 100):F0}%" : CenterText;

            canvas.FontColor = Color.FromArgb("#1E293B"); // Slate 800
            canvas.FontSize = radius * 0.45f;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(mainText, cx - radius, cy - radius + (string.IsNullOrEmpty(CenterSubtext) ? 0f : -radius * 0.15f), radius * 2, radius * 2, HorizontalAlignment.Center, VerticalAlignment.Center);

            if (!string.IsNullOrEmpty(CenterSubtext))
            {
                canvas.FontColor = Color.FromArgb("#64748B"); // Slate 500
                canvas.FontSize = radius * 0.22f;
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                canvas.DrawString(CenterSubtext, cx - radius, cy + radius * 0.2f, radius * 2, radius * 0.5f, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}
