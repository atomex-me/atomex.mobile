using System.Windows.Input;
using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomWebView : WebView
    {
        public static readonly BindableProperty NavigatingCommandProperty =
            BindableProperty.Create(nameof(NavigatingCommand), typeof(ICommand), typeof(CustomWebView), null);

        public ICommand NavigatingCommand
        {
            get { return (ICommand)GetValue(NavigatingCommandProperty); }
            set { SetValue(NavigatingCommandProperty, value); }
        }

        public CustomWebView()
        {
            Navigating += (s, e) =>
            {
                WebViewClass _WebViewClass = new WebViewClass();
                _WebViewClass.sender = s;
                _WebViewClass.e = e;

                //if (NavigatingCommand?.CanExecute(e) ?? false)
                NavigatingCommand?.Execute(e);
            };
        }
    }

    public class WebViewClass
    {
        public object sender { get; set; }
        public WebNavigatingEventArgs e { get; set; }
    }
}

