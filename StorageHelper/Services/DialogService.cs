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
            var window = new DIalogView()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            if(viewModel is DialogViewModelBase vm)
            {
                void Handler(bool? result) => window.DialogResult = result;
                vm.CloseRequsted += Handler;
                window.Closed += (_, _) => vm.CloseRequsted -= Handler;
            }

            return window.ShowDialog();
        }
    }
}
