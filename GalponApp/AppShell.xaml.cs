using Microsoft.Maui.Controls;
using GalponApp.Presentation.Views;

namespace GalponApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrar rutas para navegación asíncrona
            Routing.RegisterRoute(nameof(BatchDetailPage), typeof(BatchDetailPage));
            Routing.RegisterRoute(nameof(AddBatchPage), typeof(AddBatchPage));
            Routing.RegisterRoute(nameof(ClassifyBatchPage), typeof(ClassifyBatchPage));
        }
    }
}
