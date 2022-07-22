using ReactiveUI;

namespace atomex.ViewModels
{
    public class BaseViewModel : ReactiveObject
    {
        protected void OnPropertyChanged(string name)
        {
            this.RaisePropertyChanged(name);
        }
    }
}