using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class BatchDetailPage : ContentPage
    {
        public BatchDetailViewModel ViewModel { get; }

        public BatchDetailPage(BatchDetailViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = ViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadDetailsCommand.Execute(null);
        }
    }
}
