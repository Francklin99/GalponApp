using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class ClassifyBatchPage : ContentPage
    {
        public ClassifyBatchPage(ClassifyBatchViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
