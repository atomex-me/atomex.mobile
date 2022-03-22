using ReactiveUI;

namespace atomex.ViewModel
{
    public class BaseViewModel : ReactiveObject
    {
        protected void OnPropertyChanged(string name)
        {
            this.RaisePropertyChanged(name);
        }
    }
}