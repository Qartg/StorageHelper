using CommunityToolkit.Mvvm.ComponentModel;

namespace StorageHelper.ViewModels.Bases
{
    public class DialogViewModelBase : ObservableObject
    {
        public event Action<bool?>? CloseRequested;
        protected void Close(bool? result) => CloseRequested?.Invoke(result);
    }
}
