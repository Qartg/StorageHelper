using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StorageHelper.Models.Bases;
using StorageHelper.Services;

namespace StorageHelper.ViewModels
{
    public partial class LoginViewModel : DialogViewModelBase
    {
        private IAuthService _authService;

        [ObservableProperty]
        private string? _errorText;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task SubmitAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorText = "Введите пароль";
                return;
            }

            if (await _authService.TryEnterManagerModeAsync(password))
                Close(true);
            else
                ErrorText = "Неправильный пароль";
        }

        [RelayCommand]
        private void Cancel() => Close(false);
    }
}
