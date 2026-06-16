using CommunityToolkit.Mvvm.ComponentModel;

namespace StorageHelper.Models.Bases
{
    public class DialogViewModelBase : ObservableObject
    {
        public event Action<bool?>? CloseRequsted;
        protected void Close(bool? result) => CloseRequsted?.Invoke(result);
    }
}
