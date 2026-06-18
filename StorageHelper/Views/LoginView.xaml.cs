using System.Windows;
using System.Windows.Controls;

namespace StorageHelper.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginView.xaml.
    /// PasswordBox.Password не биндится, поэтому пароль читаем здесь (в code-behind View —
    /// допустимое место) и кладём в CommandParameter кнопки. VM получает обычную строку.
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            SubmitButton.CommandParameter = ((PasswordBox)sender).Password;
        }
    }
}
