using Android.Content;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;

using atomex.CustomElements;
using atomex.Droid.CustomElements;
using Google.Android.Material.BottomNavigation;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.Threading.Tasks;

[assembly: ExportRenderer(typeof(CustomTabbedPage), typeof(CustomTabbedPageRenderer))]
namespace atomex.Droid.CustomElements
{
    public class CustomTabbedPageRenderer : TabbedPageRenderer, BottomNavigationView.IOnNavigationItemSelectedListener, BottomNavigationView.IOnNavigationItemReselectedListener
    {
        public CustomTabbedPageRenderer(Context context)
            : base(context)
        {
        }

        private TabbedPage _page;

        protected override void OnElementChanged(ElementChangedEventArgs<TabbedPage> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                _page = (TabbedPage)e.NewElement;
            }
            else
            {
                _page = (TabbedPage)e.OldElement;
            }

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
                            if (childRelativeLayoutView is BottomNavigationView bottomNavigationView)
                            {
                                bottomNavigationView.SetOnNavigationItemReselectedListener(this);
                            }
                        }
                    }
                }
            }
        }

        private async Task PopToRoot()
        {
            await _page.CurrentPage.Navigation.PopToRootAsync();
        }

        void BottomNavigationView.IOnNavigationItemReselectedListener.OnNavigationItemReselected(IMenuItem item)
        {
            if (_page.CurrentPage.Navigation.NavigationStack.Count > 1)
                _ = PopToRoot();
        }
    }
}