using StorageHelper.Utility;
using System.Windows;

namespace StorageHelper.Views
{
    /// <summary>
    /// Логика взаимодействия для DIalogView.xaml
    /// </summary>
    public partial class DialogView : Window
    {
        public DialogView()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DarkTitleBar.ApplyDarkBar(this);
        }
    }
}
