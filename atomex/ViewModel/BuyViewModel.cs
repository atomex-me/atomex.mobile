using System;
using System.Windows.Input;
using Atomex;
using Atomex.Core;
using Xamarin.Forms;


namespace atomex.ViewModel
{
    public class BuyViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        private Network _network;
        public Network Network
        {
            get => _network;
            set
            {
                _network = value;

                if (_network == Network.MainNet)
                    Url = $"https://widget.wert.io?partner_id=atomex&theme={_appTheme}";
                else
                    Url = $"https://sandbox.wert.io?partner_id=01F298K3HP4DY326AH1NS3MM3M&theme={_appTheme}";

                OnPropertyChanged(nameof(Network));
            }
        }

        private string _url;
        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(nameof(Url)); }
        }

        private string _appTheme;

        public BuyViewModel(IAtomexApp app, string appTheme)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(AtomexApp));
            IsLoading = true;
            _appTheme = appTheme;
            Network = app.Account.Network;
        }

        private ICommand _canExecuteCommand;
        public ICommand CanExecuteCommand => _canExecuteCommand ??= new Command(() => CanExecute());

        private void CanExecute()
        {
            IsLoading = false;
        }
    }
}

