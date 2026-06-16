using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Services;
using System.Collections.ObjectModel;

namespace StorageHelper.ViewModels
{
    public partial class StorageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ItemCardViewModel> _storageItems = new();

        private IDataBaseService _dataBase;
        private IDialogService _dialogService;

        private Func<Item, ItemCardViewModel> _loadCard;
        private Func<Item?, ItemEditViewModel> _editCard;

        public StorageViewModel(IDataBaseService dataBase, 
            Func<Item, ItemCardViewModel> loadCardFactory, 
            Func<Item?, ItemEditViewModel> editCardFactory,
            IDialogService dialogService)
        {
            _dataBase = dataBase;
            _loadCard = loadCardFactory;
            _editCard = editCardFactory;
            _dialogService = dialogService;
        }

        [RelayCommand]
        private async Task LoadedAsync()
        {
            var loadedItems = await _dataBase.GetItemsList();
            foreach (var item in loadedItems)
                StorageItems.Add(_loadCard(item));
        }

        [RelayCommand]
        private async Task EditCard(ItemCardViewModel card)
        {
            var vm = _editCard(card.Item);
            if (_dialogService.ShowDialog(vm) == true)
            {
                StorageItems_Clear();
                await LoadedAsync();
            }
        }

        [RelayCommand]
        private async Task AddCard()
        {
            var vm = _editCard(null);
            if (_dialogService.ShowDialog(vm) == true)
            {
                StorageItems_Clear();
                await LoadedAsync();
            }
        }

        private void StorageItems_Clear()
        {
            foreach (var item in StorageItems)
                item.Dispose();
            StorageItems.Clear();
        }
    }
}
