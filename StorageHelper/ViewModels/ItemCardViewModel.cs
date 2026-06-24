using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.Utility;

namespace StorageHelper.ViewModels
{
    public partial class ItemCardViewModel : ObservableObject, IDisposable
    {
        private Item _currentItem;
        private IDataBaseService _dataBase;
        private IPricingService _pricingService;
        private ILogger<ItemCardViewModel> _logger;

        private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(1500));

        public string Name => _currentItem.Name;
        public string? Description => _currentItem.Description;
        public string Sku => _currentItem.Sku!;
        public string? Vendor => _currentItem.Vendor;
        public string? ImageURL => _currentItem.ImageURL;
        public int ParLevel => _currentItem.ParLevel;
        public bool IsOrderable => _currentItem.IsOrderable;
        public Item Item => _currentItem;

        [ObservableProperty] private int _currentOnStorage;
        [ObservableProperty] private decimal? _currentPrice;
        [ObservableProperty] private decimal _reOrderQuantity;
        [ObservableProperty] private decimal? _priceChangePercent;

        public bool IsPriceUp => PriceChangePercent > 0;
        public bool IsPriceDown => PriceChangePercent < 0;
        public bool IsLowStock => _currentItem.CurrentOnStorage < _currentItem.ParLevel;

        public ItemCardViewModel(IDataBaseService dataBase, Item item, IPricingService pricingService, ILogger<ItemCardViewModel> logger)
        {
            _dataBase = dataBase;
            _currentItem = item;
            _pricingService = pricingService;
            _logger = logger;

            CurrentOnStorage = item.CurrentOnStorage;

            CalculatePriceFields();
        }

        partial void OnCurrentOnStorageChanged(int oldValue, int newValue)
        {
            _currentItem.CurrentOnStorage = CurrentOnStorage;

            OnPropertyChanged(nameof(IsLowStock));
            ReOrderQuantity = _pricingService.ToOrder(CurrentOnStorage, ParLevel);
            CallUpdateDB();
        }

        [RelayCommand]
        private void PlusItemCount() => CurrentOnStorage++;

        [RelayCommand]
        private void MinusItemCount() { if (CurrentOnStorage > 0) CurrentOnStorage--; }
    
        private void CallUpdateDB()
        {
            _debouncer.Run(
                async (token) => await _dataBase.UpdateItem(_currentItem),
                (ex) => _logger.LogError(ex, "Ошибка при сохранении в debouncer, {Sku}", Item.Sku)
                );
        }

        private void CalculatePriceFields()
        {
            PriceStats stats = _pricingService.CalculateStats(_currentItem.PriceRecords);

            CurrentPrice = stats.Current;
            ReOrderQuantity = _pricingService.ToOrder(CurrentOnStorage, ParLevel);
            PriceChangePercent = _pricingService.IncreaseVsPrevious(stats.Current, stats.Previous);
        }

        public void Dispose()
        {
            _debouncer.Dispose();
        }
    }
}
