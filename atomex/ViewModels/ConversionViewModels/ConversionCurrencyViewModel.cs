using System;
using System.Globalization;
using System.Windows.Input;
using atomex.ViewModels.CurrencyViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xamarin.Forms;

namespace atomex.ViewModels.ConversionViewModels
{
    public class ConversionCurrencyViewModel : BaseViewModel
    {
        public Action MaxClicked { get; set; }
        public Action SelectCurrencyClicked { get; set; }
        public Action GotInputFocus { get; set; }

        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public string Address { get; set; }
        [Reactive] public decimal Amount { get; set; }
        public string AmountString
        {
            get => Amount.ToString();
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                    s: temp,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var amount))
                {
                    Amount = 0;
                }
                else
                {
                    Amount = amount;

                    if (Amount > long.MaxValue)
                        Amount = long.MaxValue;
                }

                this.RaisePropertyChanged(nameof(Amount));
            }
        }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString)
            {
                this.RaisePropertyChanged(nameof(AmountString));
                return;
            }

            string temp = value.Replace(",", ".");
            if (!decimal.TryParse(
                s: temp,
                style: NumberStyles.AllowDecimalPoint,
                provider: CultureInfo.InvariantCulture,
                result: out var amount))
            {
                AmountString = "0";
            }
            else
            {
                if (amount > long.MaxValue)
                    AmountString = long.MaxValue.ToString();
                else
                    AmountString = value;
            }

            this.RaisePropertyChanged(nameof(AmountString));
        }

        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public bool IsAmountValid { get; set; }

        private ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(() => MaxClicked?.Invoke());

        private ICommand _selectCurrencyCommand;
        public ICommand SelectCurrencyCommand => _selectCurrencyCommand ??= ReactiveCommand.Create(() => SelectCurrencyClicked?.Invoke());

        private ICommand _raiseGotInputFocusCommand;
        public ICommand RaiseGotInputFocusCommand => _raiseGotInputFocusCommand ??= new Command(() => GotInputFocus?.Invoke());

        public ConversionCurrencyViewModel()
        {
            IsAmountValid = true;
        }

        public void RaiseGotInputFocus()
        {
            GotInputFocus?.Invoke();
        }
    }
}
