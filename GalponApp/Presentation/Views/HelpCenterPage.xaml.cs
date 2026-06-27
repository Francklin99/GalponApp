using Microsoft.Maui.Controls;
using GalponApp.Presentation.ViewModels;
using System.Linq;

namespace GalponApp.Presentation.Views
{
    public partial class HelpCenterPage : ContentPage
    {
        public HelpCenterViewModel ViewModel { get; }

        public HelpCenterPage(HelpCenterViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            BindingContext = ViewModel;
        }

        private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is string category)
            {
                ViewModel.SelectCategoryCommand.Execute(category);
            }
        }
    }
}
