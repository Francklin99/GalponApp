using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class ReportsPage : ContentPage
    {
        public ReportsViewModel ViewModel { get; }

        public ReportsPage(ReportsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = ViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadDataCommand.Execute(null);
        }
    }
}
