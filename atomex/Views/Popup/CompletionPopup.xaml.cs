using atomex.ViewModel;
using Rg.Plugins.Popup.Pages;

namespace atomex.Views.Popup
{
    public partial class CompletionPopup : PopupPage
    {
        public CompletionPopup(PopupViewModel popupViewModel)
        {
            InitializeComponent();
            BindingContext = popupViewModel;
        }
    }
}
