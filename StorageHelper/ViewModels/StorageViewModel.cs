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
            //TODO: remove
            try
            {
                await _dataBase.AddItem(new() { Name = "Item 1", CurrentOnStorage = 23, Description = "Desc 1", ParLevel = 30, IsActive = true });
                await _dataBase.AddItem(new() { Name = "Item 2", CurrentOnStorage = 15, Description = "Desc 2", ParLevel = 23, IsActive = true });
                await _dataBase.AddItem(new() { Name = "Item 3", CurrentOnStorage = 122, Description = "Desc 3", ParLevel = 500, IsActive = true });

                var loadedItems = await _dataBase.GetItemsList();
                foreach (var item in loadedItems)
                    _storageItems.Add(_loadCard(item));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }
        }
    }
}
