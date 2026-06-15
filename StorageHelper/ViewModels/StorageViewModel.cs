using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace StorageHelper.ViewModels
{
    public partial class StorageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ItemCardViewModel> _storageItems = new();

        private IDataBaseService _dataBase;

        private Func<Item, ItemCardViewModel> _loadCard;

        public StorageViewModel(IDataBaseService dataBase, Func<Item, ItemCardViewModel> loadCardFactory)
        {
            _dataBase = dataBase;
            _loadCard = loadCardFactory;
        }

        [RelayCommand]
        private async Task LoadedAsync()
        {
            var loadedItems = await _dataBase.GetItemsList();
            foreach (var item in loadedItems)
                StorageItems.Add(_loadCard(item));
        }
    }
}
