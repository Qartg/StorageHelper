using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Models.Bases;
using StorageHelper.Services;
using System.Collections.ObjectModel;

namespace StorageHelper.ViewModels
{
    public partial class ReviewViewModel : DialogViewModelBase
    {
        [ObservableProperty] private ObservableCollection<ReviewLine> _lines;

        public decimal? Total { get; set; }

        public ReviewViewModel(IPricingService pricingService, IEnumerable<Item> items)
        {
            (IEnumerable<ReviewLine> enumLines, Total) = pricingService.BuildReview(items);
            Lines = new(enumLines);
        }

        [RelayCommand]
        private void Accept() => Close(true);

        [RelayCommand]
        private void Cancel() => Close(false);  
    }
}
