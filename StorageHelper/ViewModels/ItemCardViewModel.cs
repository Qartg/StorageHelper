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

        public string? Name => _currentItem.Name;
        public string? Description => _currentItem.Description;
        public string? ImageURL => _currentItem.ImageURL;
        public int ParLevel => _currentItem.ParLevel;

        [ObservableProperty]
        private int _currentOnStorage;

        public ItemCardViewModel(IDataBaseService dataBase, Item item)
        {
            _dataBase = dataBase;
            _currentItem = item;

            _currentOnStorage = item.CurrentOnStorage;
        }

        partial void OnCurrentOnStorageChanged(int oldValue, int newValue)
        {
            _currentItem.CurrentOnStorage = newValue;
            CallUpdateDB();
        }

        [RelayCommand]
        private void PlusItemCount()
        {
            _currentItem.CurrentOnStorage++;

            CallUpdateDB();
        }

        [RelayCommand]
        private void MinusItemCount()
        {
            _currentItem.CurrentOnStorage--;

            CallUpdateDB();
        }

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
