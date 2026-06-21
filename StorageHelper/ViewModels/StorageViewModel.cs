using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace StorageHelper.ViewModels
{
    public partial class StorageViewModel : ObservableObject
    {
        private IDataBaseService _dataBase;
        private IDialogService _dialogService;

        private Func<Item, ItemCardViewModel> _loadCardFactory;
        private Func<Item?, ItemEditViewModel> _editCardFactory;
        private Func<LoginViewModel> _loginFactory;
        private Func<IEnumerable<Item>, ReviewViewModel> _reviewFactory;

        private ICollectionView _view;

        [ObservableProperty] private ObservableCollection<ItemCardViewModel> _storageItems = new();
        [ObservableProperty] private string? _searchText;
        [ObservableProperty] private bool _isManagerMode;
        [ObservableProperty] private bool _showInactive;

        public StorageViewModel(IDataBaseService dataBase, 
            Func<Item, ItemCardViewModel> loadCardFactory,
            Func<Item?, ItemEditViewModel> editCardFactory,
            IDialogService dialogService,
            Func<LoginViewModel> loginFactory,
            Func<IEnumerable<Item>, ReviewViewModel> reviewFactory)
        {
            _dataBase = dataBase;
            _loadCardFactory = loadCardFactory;
            _editCardFactory = editCardFactory;
            _dialogService = dialogService;
            _loginFactory = loginFactory;
            _reviewFactory = reviewFactory;

            _view = CollectionViewSource.GetDefaultView(StorageItems);
            AddFilter();
        }

        partial void OnSearchTextChanged(string? value) => _view.Refresh();

        partial void OnShowInactiveChanged(bool value)
        {
            AddFilter(value);
            _view.Refresh();
        }

        [RelayCommand]
        private async Task LoadedAsync()
        {
            var loadedItems = await _dataBase.GetItemsList();
            foreach (var item in loadedItems)
                StorageItems.Add(_loadCardFactory(item));
        }

        [RelayCommand]
        private async Task EditCard(ItemCardViewModel card)
        {
            var vm = _editCardFactory(card.Item);
            if (_dialogService.ShowDialog(vm) == true)
            {
                StorageItems_Clear();
                await LoadedAsync();
            }
        }

        [RelayCommand]
        private async Task AddCard()
        {
            var vm = _editCardFactory(null);
            if (_dialogService.ShowDialog(vm) == true)
            {
                StorageItems_Clear();
                await LoadedAsync();
            }
        }

        [RelayCommand]
        private void Login()
        {
            var vm = _loginFactory();
            if (_dialogService.ShowDialog(vm) == true)
                IsManagerMode = true;
        }

        [RelayCommand]
        private void Logout() => IsManagerMode = false;

        [RelayCommand]
        private async Task ReviewAsync()
        {
            var vm = _reviewFactory(await _dataBase.GetItemsList());
            if (_dialogService.ShowDialog(vm) == true)
                MessageBox.Show("order");
            else
                MessageBox.Show("cancel");
        }

        private void StorageItems_Clear()
        {
            foreach (var item in StorageItems)
                item.Dispose();
            StorageItems.Clear();
        }

        private void AddFilter(bool showInactive = false)
        {
            _view.Filter = o => {
                if (o is ItemCardViewModel cardVm)
                {
                    if (!cardVm.Item.IsActive && !showInactive)
                        return false;

                    if (string.IsNullOrWhiteSpace(SearchText))
                        return true;

                    string clearSearch = SearchText.Trim().ToLower();
                    string clearName = cardVm.Name.Trim().ToLower();
                    string clearSku = cardVm.Sku?.Trim().ToLower() ?? "";

                    if (clearName.Contains(clearSearch) || clearSku.Contains(clearSearch))
                        return true;
                }
                return false;
            };
        }
    }
}
