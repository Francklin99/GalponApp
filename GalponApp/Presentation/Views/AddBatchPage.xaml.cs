using System;
using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class AddBatchPage : ContentPage
    {
        public AddBatchViewModel ViewModel { get; }

        public AddBatchPage(AddBatchViewModel viewModel)
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

        private void Picker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ViewModel.UpdateBreedsAndPurposes();
        }
    }
}
