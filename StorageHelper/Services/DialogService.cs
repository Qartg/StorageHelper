using StorageHelper.ViewModels.Bases;
using StorageHelper.Views;
using System.Windows;

namespace StorageHelper.Services
{
    public interface IDialogService
    {
        bool? ShowDialog(object viewModel);
    }

    public class DialogService : IDialogService
    {
        public bool? ShowDialog(object viewModel)
        {
            var window = new DialogView()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            if(viewModel is DialogViewModelBase vm)
            {
                void Handler(bool? result) => window.DialogResult = result;
                vm.CloseRequested += Handler;
                window.Closed += (_, _) => vm.CloseRequested -= Handler;
            }

            return window.ShowDialog();
        }
    }
}
