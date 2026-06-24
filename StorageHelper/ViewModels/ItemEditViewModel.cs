using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.Services.Automation;
using StorageHelper.ViewModels.Bases;

namespace StorageHelper.ViewModels
{
    public partial class ItemEditViewModel : DialogViewModelBase
    {
        private Item? _currentItem;
        private decimal _initPrice = 0;
        private bool _fetching;
        private IDataBaseService _dataBase;
        private IVendorAutomation _automation;

        public bool IsCanSave => CanSave();
        public bool IsCanFetch => !string.IsNullOrWhiteSpace(Sku) && !_fetching;
        public bool Editing => _currentItem != null;
        public string Title => !Editing ? "Новый предмет" : "Редактирование";

        [ObservableProperty] private string? _errorText;
        [ObservableProperty] private string? _name;
        [ObservableProperty] private string? _sku;
        [ObservableProperty] private string? _notes;
        [ObservableProperty] private string? _vendor;
        [ObservableProperty] private int _parLevel = 0;
        [ObservableProperty] private int _currentOnStorage = 0;
        [ObservableProperty] private decimal _price = 0;
        [ObservableProperty] string? _description;
        [ObservableProperty] string? _imageUrl;
        [ObservableProperty] bool _isOrderable = true;
        [ObservableProperty] private bool _isActive = true;
        [ObservableProperty] private bool _isAwaitingLogin;
        public ItemEditViewModel(IDataBaseService dataBase, Item? localItem, IVendorAutomation automation, IPricingService pricing)
        {
            _dataBase = dataBase;
            _automation = automation;

            if(localItem != null)
            {
                _currentItem = localItem;  
                Name = localItem.Name;
                Sku = localItem.Sku;
                Notes = localItem.Notes;
                Vendor = localItem.Vendor;
                ParLevel = localItem.ParLevel;
                CurrentOnStorage = localItem.CurrentOnStorage;
                Description = localItem.Description;
                ImageUrl = localItem.ImageURL;
                IsOrderable = localItem.IsOrderable;
                IsActive = localItem.IsActive;

                Price = pricing.CalculateStats(localItem.PriceRecords).Current ?? 0;
                _initPrice = Price;
            }
        }

        partial void OnNameChanged(string? value) => SaveCommand.NotifyCanExecuteChanged();
        partial void OnSkuChanged(string? value)
        {
            SaveCommand.NotifyCanExecuteChanged();
            FetchFromOzonCommand.NotifyCanExecuteChanged();
        }

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
                _currentItem.Description = Description;
                _currentItem.ImageURL = ImageUrl;
                _currentItem.ParLevel = ParLevel;
                _currentItem.IsActive = IsActive;
                _currentItem.IsOrderable = IsOrderable;
                result = await _dataBase.UpdateItem(_currentItem);
                if(result && Price != 0 && _initPrice != Price)
                    await _dataBase.AddPriceRecord(_currentItem.Id, Price, DateTime.Now);
            }
            else
            {
                Item item = new Item() { Name = Name!, Sku = Sku! };
                item.CurrentOnStorage = CurrentOnStorage;
                item.Notes = Notes;
                item.Vendor = Vendor;
                item.IsActive = IsActive;
                item.ParLevel = ParLevel;
                item.ImageURL = ImageUrl;
                item.IsOrderable = IsOrderable;
                item.Description = Description;
                if (Price != 0 && _initPrice != Price)
                    item.PriceRecords.Add(new() { Price = Price, CapturedAt = DateTime.Now });

                result = await _dataBase.AddItem(item);
            }

            if (!result)
                ErrorText = "Такой Sku уже существует";
            else
                Close(true);
        }

        [RelayCommand]
        private void Cancel() => Close(false);

        [RelayCommand(CanExecute = nameof(Editing))]
        private async Task DeleteAsync()
        {
            if (await _dataBase.DeleteItem(_currentItem!.Id))
                Close(true);
            else
                ErrorText = "Не удалось удалить";
        }

        [RelayCommand(CanExecute = nameof(IsCanFetch))]
        private async Task FetchFromOzonAsync()
        {
            try
            {
                _fetching = true;

                var progress = new Progress<AuthPhase>(phase => IsAwaitingLogin = phase == AuthPhase.AwaitingLogin);
                if (!await _automation.ConnectAsync(progress))
                {
                    ErrorText = "Не получилось открыть браузер";
                    return;
                }

                var actualInfo = await _automation.GetItemInfo(Sku!);

                if (actualInfo.Success)
                {
                    Name = actualInfo.Name;
                    Sku = actualInfo.Sku;
                    Vendor = actualInfo.Vendor;
                    Description = actualInfo.Description;
                    ImageUrl = actualInfo.ImageURL;
                    Price = actualInfo.Price ?? 0;
                    IsOrderable = actualInfo.IsOrderable == true;
                }
                else
                    ErrorText = "Не удалось получить данные";
            }
            finally
            {
                _fetching = false;
            }
        }

        private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Sku);
    }
}
