using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace StorageHelper.ViewModels
{
    public partial class ReviewViewModel : DialogViewModelBase
    {
        [ObservableProperty] private ObservableCollection<ReviewLine> _lines;

        public decimal? Total { get; set; }

        public ReviewViewModel(IPricingService pricingService, IEnumerable<ReviewLine> items, decimal? total)
        {
            Lines = new(items);
            Total = total;
        }

        [RelayCommand]
        private void Accept() => Close(true);

        [RelayCommand]
        private void Cancel() => Close(false);  
    }
}
