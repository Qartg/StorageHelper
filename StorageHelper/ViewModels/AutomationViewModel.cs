using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StorageHelper.Models;
using StorageHelper.Services;
using StorageHelper.Services.Automation;
using StorageHelper.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace StorageHelper.ViewModels
{
    public partial class AutomationViewModel : DialogViewModelBase
    {
        [ObservableProperty] private ObservableCollection<AutomationLineViewModel> _log = new();
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(Processed))] private int _succeededCount;
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(Processed))] private int _failedCount;
        
        [ObservableProperty] 
        [NotifyCanExecuteChangedFor(nameof(RunCommand))] 
        [NotifyCanExecuteChangedFor(nameof(StopCommand))] 
        private bool _isRunning;

        private IEnumerable<ReviewLine> _orderLines;
        private CancellationTokenSource _cts = new();

        private IVendorAutomation _automation;
        private IPricingService _pricingService;
        private ILogger<AutomationViewModel> _logger;

        public int Total { get; set; }
        public int Processed => SucceededCount + FailedCount;

        public AutomationViewModel(IVendorAutomation automation, IPricingService pricingService, ILogger<AutomationViewModel> logger, IEnumerable<ReviewLine> lines)
        {
            _automation = automation;
            _orderLines = lines;
            _pricingService = pricingService;
            _logger = logger;

            Total = lines.Count();
        }

        [RelayCommand]
        private void Cancel()
        {
            Stop();
            Close(false);
        }

        [RelayCommand(CanExecute =nameof(CanStop))]
        private void Stop()
        {
            _cts.Cancel();
        }

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task Run()
        {
            try
            {
                IsRunning = true;

                _cts = new();
                Log.Clear();
                SucceededCount = 0;
                FailedCount = 0;

                foreach (var item in _orderLines)
                {
                    AutomationLineViewModel curLine = new(AutomationStatus.Running, item.Name, item.Sku, item.CurrentPrice, "Начинаю работу");
                    Log.Add(curLine);
                    try
                    {
                        if (_cts.IsCancellationRequested) break;

                        var actualInfo = await _automation.GetItemInfo(item.Sku, _cts.Token);
                        curLine.UpdateFieldsByGetInfo(actualInfo);

                        if (curLine.Status == AutomationStatus.Failed)
                        {
                            FailedCount++;
                            continue;
                        }

                        var cartResult = await _automation.AddItemToCart(item.Sku,
                            item.Quantity,
                            _cts.Token);

                        curLine.UpdateFieldsByAddCart(cartResult);

                        if (cartResult.Success)
                            SucceededCount++;
                        else
                            FailedCount++;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        curLine.Status = AutomationStatus.Failed;
                        curLine.Message = "Произошла ошибка";

                        FailedCount++;
                        _logger.LogError(ex, "Error in Automation RunCommand, {Sku}", item.Sku);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                IsRunning = false;
            }
        }

        private bool CanRun() => !IsRunning;
        private bool CanStop() => IsRunning;
    }
}
