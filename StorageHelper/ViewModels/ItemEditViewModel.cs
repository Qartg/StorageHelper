using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.ViewModels.Bases;

namespace StorageHelper.ViewModels
{
    public partial class ItemEditViewModel : DialogViewModelBase
    {
        private Item? _currentItem;
        private IDataBaseService _dataBase;

        public bool IsCanSave => CanSave();
        public bool Editing => _currentItem != null;
        public string Title => !Editing ? "Новый предмет" : "Редактирование";

        [ObservableProperty]
        private string? _errorText;
        [ObservableProperty]
        private string? _name;
        [ObservableProperty]
        private string? _sku;
        [ObservableProperty]
        private string? _notes;
        [ObservableProperty]
        private string? _vendor;
        [ObservableProperty]
        private int _parLevel = 0;
        [ObservableProperty]
        private int _currentOnStorage = 0;
        [ObservableProperty]
        private bool _isActive;

        public ItemEditViewModel(IDataBaseService dataBase, Item? localItem)
        {
            _dataBase = dataBase;

            if(localItem != null)
            {
                _currentItem = localItem;  
                Name = localItem.Name;
                Sku = localItem.Sku;
                Notes = localItem.Notes;
                Vendor = localItem.Vendor;
                ParLevel = localItem.ParLevel;
                CurrentOnStorage = localItem.CurrentOnStorage;
                IsActive = localItem.IsActive;
            }
        }

        partial void OnNameChanged(string? value) => SaveCommand.NotifyCanExecuteChanged();
        partial void OnSkuChanged(string? value) => SaveCommand.NotifyCanExecuteChanged();

        [RelayCommand(CanExecute =nameof(IsCanSave))]
        private async Task Save()
        {
            if (!CanSave()) return;

            bool result;
            if (Editing)
            {
                _currentItem!.Name = Name!;
                _currentItem.CurrentOnStorage = CurrentOnStorage;
                _currentItem.Notes = Notes;
                _currentItem.Vendor = Vendor;
                _currentItem.IsActive = IsActive;
                _currentItem.ParLevel = ParLevel;
                result = await _dataBase.UpdateItem(_currentItem);
            }
            else
            {
                Item item = new Item() { Name = Name!, Sku = Sku! };
                item.CurrentOnStorage = CurrentOnStorage;
                item.Notes = Notes;
                item.Vendor = Vendor;
                item.IsActive = IsActive;
                item.ParLevel = ParLevel;
                result = await _dataBase.AddItem(item);
            }

            if (!result)
                ErrorText = "Такой Sku уже существует";
            else
                Close(true);
        }

        [RelayCommand]
        private void Cancel() => Close(false);

        private bool CanSave() => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Sku);
    }
}
