using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class AlertsPage : ContentPage
    {
        public AlertsViewModel ViewModel { get; }

        public AlertsPage(AlertsViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = ViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadVaccinationsCommand.Execute(null);
        }

        private void CollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            ViewModel.ApplyFilterCommand.Execute(null);
        }
    }
}
