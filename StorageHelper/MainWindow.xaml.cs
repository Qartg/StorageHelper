using StorageHelper.ViewModels;
using System.Windows;

namespace StorageHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(StorageViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            
            
        }


    }
}