using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class FeedCalculatorPage : ContentPage
    {
        public FeedCalculatorViewModel ViewModel { get; }

        public FeedCalculatorPage(FeedCalculatorViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = ViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadBatchesCommand.Execute(null);
        }
    }
}
