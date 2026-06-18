using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.Utility;
using System.Windows;

namespace StorageHelper.ViewModels
{
    public partial class ItemCardViewModel : ObservableObject, IDisposable
    {
        private Item _currentItem;
        private IDataBaseService _dataBase;

        private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(1500));

        public string Name => _currentItem.Name;
        public string? Description => _currentItem.Description;
        public string Sku => _currentItem.Sku!;
        public string? Vendor => _currentItem.Vendor;
        public string? ImageURL => _currentItem.ImageURL;
        public int ParLevel => _currentItem.ParLevel;
        public bool IsOrderable => _currentItem.IsOredrable;
        public Item Item => _currentItem;

        [ObservableProperty]
        private int _currentOnStorage;

        public bool IsLowStock => _currentItem.CurrentOnStorage < _currentItem.ParLevel;

        public ItemCardViewModel(IDataBaseService dataBase, Item item)
        {
            _dataBase = dataBase;
            _currentItem = item;

            CurrentOnStorage = item.CurrentOnStorage;
        }

        partial void OnCurrentOnStorageChanged(int oldValue, int newValue)
        {
            _currentItem.CurrentOnStorage = CurrentOnStorage;

            OnPropertyChanged(nameof(IsLowStock));
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
                (ex) => Application.Current.Dispatcher.Invoke(() => throw ex)
                );
        }

        public void Dispose()
        {
            _debouncer.Dispose();
        }
    }
}
