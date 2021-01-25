using System;
using System.Threading;
using System.Threading.Tasks;
using atomex.iOS.Services;
using atomex.Services;
using Serilog;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(ToastService))]
namespace atomex.iOS.Services
{
    public class ToastService : IToastService
    {
        UIView ToastView;
        UILabel MessageLabel;
        string initAppTheme;

        CancellationTokenSource cancelationToken;

        NSLayoutConstraint topConstraint;
        NSLayoutConstraint centerConstraint;
        NSLayoutConstraint bottomConstraint;

        public async void Show(string message, ToastPosition toastPosition, string appTheme)
        {
            UIWindow window = UIApplication.SharedApplication.KeyWindow;

            if (ToastView == null || !initAppTheme.Equals(appTheme))
            {
                ToastView = new UIView();
                ToastView.TranslatesAutoresizingMaskIntoConstraints = false;
                ToastView.Frame = window.Frame;

                initAppTheme = appTheme;

                if (appTheme == OSAppTheme.Dark.ToString())
                    ToastView.BackgroundColor = UIColor.FromRGBA(red: 202f / 255.0f,
                                                                 green: 214f / 255.0f,
                                                                 blue: 224f / 255.0f,
                                                                 alpha: 0.8f);
                else
                    ToastView.BackgroundColor = UIColor.FromRGBA(red: 31f / 255.0f,
                                                                 green: 31f / 255.0f,
                                                                 blue: 31f / 255.0f,
                                                                 alpha: 0.7f);
                ToastView.Layer.CornerRadius = 10;
                ToastView.Layer.ShadowColor = UIColor.Gray.CGColor;
                ToastView.Layer.ShadowOffset = new CoreGraphics.CGSize(5, 5);
                ToastView.Layer.ShadowOpacity = 0.7f;
                ToastView.Layer.ShadowRadius = 5;

                window.AddSubview(ToastView);

                MessageLabel = new UILabel()
                {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    Font = UIFont.SystemFontOfSize(14),
                    TextAlignment = UITextAlignment.Center,
                    Lines = 0
                };

                if (appTheme == OSAppTheme.Dark.ToString())
                    MessageLabel.TextColor = UIColor.Black;
                else
                    MessageLabel.TextColor = UIColor.White;

                ToastView.AddSubview(MessageLabel);

                MessageLabel.TopAnchor.ConstraintEqualTo(ToastView.TopAnchor, 10).Active = true;
                MessageLabel.BottomAnchor.ConstraintEqualTo(ToastView.BottomAnchor, -10).Active = true;
                MessageLabel.LeadingAnchor.ConstraintEqualTo(ToastView.LeadingAnchor, 10).Active = true;
                MessageLabel.TrailingAnchor.ConstraintEqualTo(ToastView.TrailingAnchor, -10).Active = true;

                ToastView.LeadingAnchor.ConstraintEqualTo(window.SafeAreaLayoutGuide.LeadingAnchor, 80).Active = true;
                ToastView.TrailingAnchor.ConstraintEqualTo(window.SafeAreaLayoutGuide.TrailingAnchor, -80).Active = true;

                bottomConstraint = ToastView.BottomAnchor.ConstraintEqualTo(window.SafeAreaLayoutGuide.BottomAnchor, -60);
                topConstraint = ToastView.TopAnchor.ConstraintEqualTo(window.SafeAreaLayoutGuide.TopAnchor, 60);
                centerConstraint = ToastView.CenterYAnchor.ConstraintEqualTo(window.SafeAreaLayoutGuide.CenterYAnchor, 0);
            }

            window.AddSubview(ToastView);

            if (ToastView.Alpha == 1)
            {
                cancelationToken?.Cancel();
            }

            cancelationToken = new CancellationTokenSource();

            switch (toastPosition)
            {
                case ToastPosition.Bottom:
                    bottomConstraint.Active = true;
                    topConstraint.Active = false;
                    centerConstraint.Active = false;
                    break;
                case ToastPosition.Center:
                    bottomConstraint.Active = false;
                    topConstraint.Active = false;
                    centerConstraint.Active = true;
                    break;
                case ToastPosition.Top:
                    bottomConstraint.Active = false;
                    topConstraint.Active = true;
                    centerConstraint.Active = false;
                    break;
                default:
                    bottomConstraint.Active = true;
                    topConstraint.Active = false;
                    centerConstraint.Active = false;
                    break;
            }

            MessageLabel.Text = message;

            ToastView.Alpha = 1;

            try
            {
                await Task.Delay(1000, cancelationToken.Token);
                UIView.Animate(0.5f, () =>
                {
                    ToastView.Alpha = 0;
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "ToastView animation error");
            }
            finally
            {
                cancelationToken?.Dispose();
                cancelationToken = null;
            }
        }
    }
}
