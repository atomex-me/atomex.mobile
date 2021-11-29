using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;

namespace atomex.ViewModel
{
    public enum PopupType
    {
        [Description ("Success")]
        Success,
        [Description ("Error")]
        Error
    }

    public class PopupViewModel
    {
        public PopupType Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ButtonText { get; set; }

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new Command(async () => await ClosePopup());

        private async Task ClosePopup()
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}