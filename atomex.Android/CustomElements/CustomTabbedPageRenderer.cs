using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;
using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.Threading.Tasks;
using Google.Android.Material.Navigation;

[assembly: ExportRenderer(typeof(CustomTabbedPage), typeof(CustomTabbedPageRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomTabbedPageRenderer : TabbedPageRenderer, NavigationBarView.IOnItemSelectedListener, NavigationBarView.IOnItemReselectedListener
    {
        public CustomTabbedPageRenderer(Context context)
            : base(context)
        {
        }

        private TabbedPage tabbedPage;

        protected override void OnElementChanged(ElementChangedEventArgs<TabbedPage> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
                tabbedPage = (TabbedPage)e.NewElement;
            else
                tabbedPage = (TabbedPage)e.OldElement;

            if (e.OldElement == null && e.NewElement != null)
            {
                for (int i = 0; i <= this.ViewGroup.ChildCount - 1; i++)
                {
                    var childView = this.ViewGroup.GetChildAt(i);
                    if (childView is ViewGroup viewGroup)
                    {
                        for (int j = 0; j <= viewGroup.ChildCount - 1; j++)
                        {
                            var childRelativeLayoutView = viewGroup.GetChildAt(j);
                            if (childRelativeLayoutView is NavigationBarView bottomNavigationView)
                                bottomNavigationView.SetOnItemReselectedListener(this);
                        }
                    }
                }
            }
        }

        public void OnNavigationItemReselected(IMenuItem item)
        {
            if (tabbedPage?.CurrentPage.Navigation.NavigationStack.Count > 1)
                _ = PopToRoot();
        }

        private async Task PopToRoot()
        {
            await tabbedPage.CurrentPage.Navigation.PopToRootAsync();
        }
    }
}