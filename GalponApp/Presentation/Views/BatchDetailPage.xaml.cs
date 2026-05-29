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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadDetailsCommand.Execute(null);

            // Staggered entry animation for elements in the detail page
            if (MainLayout != null)
            {
                foreach (var child in MainLayout.Children)
                {
                    if (child is VisualElement view)
                    {
                        view.Opacity = 0;
                        view.TranslationY = 15;
                    }
                }

                await Task.Delay(80);

                foreach (var child in MainLayout.Children)
                {
                    if (child is VisualElement view)
                    {
                        _ = view.FadeToAsync(1, 250, Easing.CubicOut);
                        _ = view.TranslateToAsync(0, 0, 250, Easing.CubicOut);
                        await Task.Delay(50); // Breve retardo staggered
                    }
                }
            }
        }
    }
}
