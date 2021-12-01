using System;
using System.Threading.Tasks;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex.Views
{
    public partial class OperationRequestEditPage : ContentPage
    {
        public OperationRequestEditPage(EditOperationViewModel editOperationViewModel)
        {
            InitializeComponent();
            BindingContext = editOperationViewModel;
        }

        private void EntryFocusedFee(object sender, FocusEventArgs args)
        {
            FrameFee.HasShadow = args.IsFocused;
            
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, EntryFee.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnEntryTappedFee(object sender, EventArgs args)
        {
            EntryFee.Focus();
        }

        private async void OnTextChangedFee(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHintFee.IsVisible)
                {
                    NameHintFee.IsVisible = true;
                    NameHintFee.Text = EntryFee.Placeholder;

                    _ = NameHintFee.FadeTo(1, 500, Easing.Linear);
                    _ = EntryFee.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHintFee.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHintFee.FadeTo(0, 500, Easing.Linear),
                    EntryFee.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHintFee.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
                NameHintFee.IsVisible = false;
            }
        }

        private void EntryFocusedSource(object sender, FocusEventArgs args)
        {
            FrameSource.HasShadow = args.IsFocused;
            
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, EntrySource.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnEntryTappedSource(object sender, EventArgs args)
        {
            EntrySource.Focus();
        }

        private async void OnTextChangedSource(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHintSource.IsVisible)
                {
                    NameHintSource.IsVisible = true;
                    NameHintSource.Text = EntrySource.Placeholder;

                    _ = NameHintSource.FadeTo(1, 500, Easing.Linear);
                    _ = EntrySource.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHintSource.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHintFee.FadeTo(0, 500, Easing.Linear),
                    EntryFee.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHintFee.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
                NameHintFee.IsVisible = false;
            }
        }
        private void EntryFocusedAmount(object sender, FocusEventArgs args)
        {
            FrameAmount.HasShadow = args.IsFocused;
            
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, EntryAmount.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnEntryTappedAmount(object sender, EventArgs args)
        {
            EntryAmount.Focus();
        }

        private async void OnTextChangedAmount(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHintAmount.IsVisible)
                {
                    NameHintAmount.IsVisible = true;
                    NameHintAmount.Text = EntryAmount.Placeholder;

                    _ = NameHintAmount.FadeTo(1, 500, Easing.Linear);
                    _ = EntryAmount.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHintAmount.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHintAmount.FadeTo(0, 500, Easing.Linear),
                    EntryAmount.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHintAmount.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
                NameHintAmount.IsVisible = false;
            }
        }
        
        private void EntryFocusedGasLimit(object sender, FocusEventArgs args)
        {
            FrameGasLimit.HasShadow = args.IsFocused;
            
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, EntryGasLimit.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnEntryTappedGasLimit(object sender, EventArgs args)
        {
            EntryGasLimit.Focus();
        }

        private async void OnTextChangedGasLimit(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHintGasLimit.IsVisible)
                {
                    NameHintGasLimit.IsVisible = true;
                    NameHintGasLimit.Text = EntryGasLimit.Placeholder;

                    _ = NameHintGasLimit.FadeTo(1, 500, Easing.Linear);
                    _ = EntryGasLimit.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHintGasLimit.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHintGasLimit.FadeTo(0, 500, Easing.Linear),
                    EntryGasLimit.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHintGasLimit.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
                NameHintGasLimit.IsVisible = false;
            }
        }
        
        private void EntryFocusedStorageLimit(object sender, FocusEventArgs args)
        {
            FrameStorageLimit.HasShadow = args.IsFocused;
            
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, EntryStorageLimit.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnEntryTappedStorageLimit(object sender, EventArgs args)
        {
            EntryStorageLimit.Focus();
        }

        private async void OnTextChangedStorageLimit(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHintStorageLimit.IsVisible)
                {
                    NameHintStorageLimit.IsVisible = true;
                    NameHintStorageLimit.Text = EntryStorageLimit.Placeholder;

                    _ = NameHintStorageLimit.FadeTo(1, 500, Easing.Linear);
                    _ = EntryStorageLimit.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHintStorageLimit.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHintStorageLimit.FadeTo(0, 500, Easing.Linear),
                    EntryStorageLimit.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHintStorageLimit.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
                NameHintStorageLimit.IsVisible = false;
            }
        }
        
        private void EntryFocusedDestination(object sender, FocusEventArgs args)
        {
            FrameDestination.HasShadow = args.IsFocused;
            
            if (args.IsFocused)
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, EntryDestination.Height, true);
                    return false;
                });
            }
            else
            {
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnEntryTappedDestination(object sender, EventArgs args)
        {
            EntryDestination.Focus();
        }

        private async void OnTextChangedDestination(object sender, TextChangedEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.NewTextValue))
            {
                if (!NameHintDestination.IsVisible)
                {
                    NameHintDestination.IsVisible = true;
                    NameHintDestination.Text = EntryDestination.Placeholder;

                    _ = NameHintDestination.FadeTo(1, 500, Easing.Linear);
                    _ = EntryDestination.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = NameHintDestination.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    NameHintDestination.FadeTo(0, 500, Easing.Linear),
                    EntryDestination.TranslateTo(0, 0, 500, Easing.CubicOut),
                    NameHintDestination.TranslateTo(0, -10, 500, Easing.CubicOut)
                );
                
                NameHintDestination.IsVisible = false;
            }
        }
        
    }
}