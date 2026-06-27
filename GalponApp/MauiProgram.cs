using GalponApp.Infrastructure.Services;
using GalponApp.Presentation.ViewModels;
using GalponApp.Presentation.Views;
using Microsoft.Extensions.Logging;

namespace GalponApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 1. Registro de Servicios (Capa de Infraestructura)
            builder.Services.AddSingleton<FileStorageService>();
            builder.Services.AddSingleton<FeedingCalculator>();
            builder.Services.AddSingleton<ReportService>();
            builder.Services.AddSingleton<QRCodeService>();

            // 2. Registro de Vistas y ViewModels (Capa de Presentación)
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<DashboardPage>();

            builder.Services.AddSingleton<BatchListViewModel>();
            builder.Services.AddSingleton<BatchListPage>();

            builder.Services.AddTransient<AddBatchViewModel>();
            builder.Services.AddTransient<AddBatchPage>();

            builder.Services.AddTransient<BatchDetailViewModel>();
            builder.Services.AddTransient<BatchDetailPage>();

            builder.Services.AddTransient<ClassifyBatchViewModel>();
            builder.Services.AddTransient<ClassifyBatchPage>();

            builder.Services.AddSingleton<AlertsViewModel>();
            builder.Services.AddSingleton<AlertsPage>();

            builder.Services.AddSingleton<HelpCenterViewModel>();
            builder.Services.AddSingleton<HelpCenterPage>();

            builder.Services.AddSingleton<ReportsViewModel>();
            builder.Services.AddSingleton<ReportsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
