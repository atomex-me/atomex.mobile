using System;
using System.Collections.Generic;

using Xamarin.Forms;
using ZXing;
using ZXing.Net.Mobile.Forms;

namespace atomex
{
    public partial class ScanningQrPage : ZXingScannerPage
    {
        public Action<string> OnQrScanned;

        public ScanningQrPage(Action<string> onQrScanned)
        {
            InitializeComponent();
            OnQrScanned = onQrScanned;
        }
        public void OnScanResultHandle(Result result)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                int indexOfChar = result.Text.IndexOf(':');
                if (indexOfChar == -1)
                {
                    OnQrScanned(result.Text);
                }
                else
                {
                    OnQrScanned(result.Text.Substring(indexOfChar + 1));
                }
                await Navigation.PopAsync();
            });
        }
    }
}
