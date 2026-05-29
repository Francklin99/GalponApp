using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;

namespace GalponApp.Presentation.Views
{
    public partial class BatchListPage : ContentPage
    {
        public BatchListViewModel ViewModel { get; }

        public BatchListPage(BatchListViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = ViewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadBatchesCommand.Execute(null);

            // Staggered entry animation for elements in the page
            if (MainGrid != null)
            {
                foreach (var child in MainGrid.Children)
                {
                    if (child is VisualElement view)
                    {
                        view.Opacity = 0;
                        view.TranslationY = 15;
                    }
                }

                await Task.Delay(80);

                foreach (var child in MainGrid.Children)
                {
                    if (child is VisualElement view)
                    {
                        _ = view.FadeToAsync(1, 250, Easing.CubicOut);
                        _ = view.TranslateToAsync(0, 0, 250, Easing.CubicOut);
                        await Task.Delay(50); // Breve retardo para el efecto staggered
                    }
                }
            }
        }

        private void CollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            ViewModel.ApplyFiltersCommand.Execute(null);
        }

        private void Entry_Unfocused(object? sender, FocusEventArgs e)
        {
            ViewModel.ApplyFiltersCommand.Execute(null);
        }
    }
}
