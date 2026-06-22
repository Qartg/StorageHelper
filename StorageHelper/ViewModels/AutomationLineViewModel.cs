using CommunityToolkit.Mvvm.ComponentModel;
using StorageHelper.Models;

namespace StorageHelper.ViewModels
{
    public enum AutomationStatus { Pending, Running, Success, Failed }

    public partial class AutomationLineViewModel : ObservableObject
    {
        [ObservableProperty] private AutomationStatus _status;
        [ObservableProperty] private string? _title;
        [ObservableProperty] private string _sku = null!;
        [ObservableProperty] private decimal? _capturedPrice;
        [ObservableProperty] private string? _message;

        public AutomationLineViewModel(AutomationStatus status, string? title, string sku, decimal? price, string? message)
        {
            Status = status;
            Title = title;
            Sku = sku;
            CapturedPrice = price;
            Message = message;
        }

        public AutomationLineViewModel(GetItemInfoResult result)
        {
            Status = result.Success && result.IsOrderable == true ? AutomationStatus.Running : AutomationStatus.Failed;
            Title = result.Name;
            Sku = result.Sku;
            CapturedPrice = result.Price;
            Message = result.Message;
        }

        public void UpdateFieldsByAddCart(AddToCartResult result)
        {
            Status = result.Success ? AutomationStatus.Success : AutomationStatus.Failed;

            Title = result.ProductTitle ?? Title;
            Sku = result.Sku;
            CapturedPrice = result.CapturedPrice ?? CapturedPrice;
            Message = result.Message;
        }
    }
}
