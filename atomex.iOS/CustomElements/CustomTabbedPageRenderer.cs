using System;
using atomex.CustomElements;
using atomex.iOS.CustomElements;
using CoreGraphics;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CustomTabbedPage), typeof(CustomTabbedPageRenderer))]
namespace atomex.iOS.CustomElements
{
    public class CustomTabbedPageRenderer : TabbedRenderer
    {
        public CustomTabbedPageRenderer()
        {
        }
        public override void ViewWillAppear(bool animated)
        {
            if (TabBar?.Items == null)
                return;

            var tabs = Element as TabbedPage;
            if (tabs != null)
            {
                for (int i = 0; i < TabBar.Items.Length; i++)
                {
                    UpdateTabBarItem(TabBar.Items[i], tabs.Children[i].IconImageSource);
                }
            }
            base.ViewWillAppear(animated);
        }

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            TabBar.TintColor = new UIColor(red: 0.23f, green: 0.56f, blue: 0.20f, alpha: 1.0f);
            TabBar.UnselectedItemTintColor = new UIColor(red: 0.34f, green: 0.34f, blue: 0.34f, alpha: 1.0f);
        }

        private void UpdateTabBarItem(UITabBarItem item, ImageSource icon)
        {
            if (item == null || icon == null)
                return;

            TabBar.SelectedImageTintColor = new UIColor(red: 0.37f, green: 0.61f, blue: 0.62f, alpha: 1.0f);
            //foreach (var uiTabBarItem in TabBar.Items)
            //{
            //    //var fontSize = new UITextAttributes() { Font = UIFont.SystemFontOfSize(13) };
            //    //uiTabBarItem.SetTitleTextAttributes(fontSize, UIControlState.Normal);
            //    //uiTabBarItem.TitlePositionAdjustment = new UIOffset(0, 1);
 
            //    uiTabBarItem.ImageInsets = new UIEdgeInsets(0, 0, -20, 0);
            //}

            // Set the font for the title.
            //item.SetTitleTextAttributes(new UITextAttributes() { Font = UIFont.FromName("SourceSansPro-Regular", 20), TextColor = Color.FromHex("#757575").ToUIColor() }, UIControlState.Normal);
            //item.SetTitleTextAttributes(new UITextAttributes() { Font = UIFont.FromName("SourceSansPro-Regular", 20), TextColor = Color.FromHex("#3C9BDF").ToUIColor() }, UIControlState.Selected);
        }
    }

    // ANDROID:

    //public class ExtendedTabbedPageRenderer : TabbedPageRenderer
    //{
    //    Xamarin.Forms.TabbedPage tabbedPage;
    //    BottomNavigationView bottomNavigationView;
    //    Android.Views.IMenuItem lastItemSelected;
    //    int lastItemId = -1;

    //    protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.TabbedPage> e)
    //    {
    //        base.OnElementChanged(e);

    //        if (e.NewElement != null)
    //        {
    //            tabbedPage = e.NewElement as ExtendedTabbedPage;
    //            bottomNavigationView = (GetChildAt(0) as Android.Widget.RelativeLayout).GetChildAt(1) as BottomNavigationView;
    //            bottomNavigationView.NavigationItemSelected += BottomNavigationView_NavigationItemSelected;

    //            //Call to change the font
    //            ChangeFont();
    //        }

    //        if (e.OldElement != null)
    //        {
    //            bottomNavigationView.NavigationItemSelected -= BottomNavigationView_NavigationItemSelected;
    //        }
    //    }

    //    //Change Tab font
    //    void ChangeFont()
    //    {
    //        var fontFace = Typeface.CreateFromAsset(Context.Assets, "gilsansultrabold.ttf");
    //        var bottomNavMenuView = bottomNavigationView.GetChildAt(0) as BottomNavigationMenuView;

    //        for (int i = 0; i < bottomNavMenuView.ChildCount; i++)
    //        {
    //            var item = bottomNavMenuView.GetChildAt(i) as BottomNavigationItemView;
    //            var itemTitle = item.GetChildAt(1);

    //            var smallTextView = ((TextView)((BaselineLayout)itemTitle).GetChildAt(0));
    //            var largeTextView = ((TextView)((BaselineLayout)itemTitle).GetChildAt(1));

    //            lastItemId = bottomNavMenuView.SelectedItemId;

    //            smallTextView.SetTypeface(fontFace, TypefaceStyle.Bold);
    //            largeTextView.SetTypeface(fontFace, TypefaceStyle.Bold);

    //            //Set text color
    //            var textColor = (item.Id == bottomNavMenuView.SelectedItemId) ? tabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().GetBarSelectedItemColor().ToAndroid() : tabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().GetBarItemColor().ToAndroid();
    //            smallTextView.SetTextColor(textColor);
    //            largeTextView.SetTextColor(textColor);
    //        }
    //    }

    //    void BottomNavigationView_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
    //    {
    //        var normalColor = tabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().GetBarItemColor().ToAndroid();
    //        var selectedColor = tabbedPage.On<Xamarin.Forms.PlatformConfiguration.Android>().GetBarSelectedItemColor().ToAndroid();

    //        if (lastItemId != -1)
    //        {
    //            SetTabItemTextColor(bottomNavMenuView.GetChildAt(lastItemId) as BottomNavigationItemView, normalColor);
    //        }

    //        SetTabItemTextColor(bottomNavMenuView.GetChildAt(e.Item.ItemId) as BottomNavigationItemView, selectedColor);

    //        this.OnNavigationItemSelected(e.Item);
    //        lastItemId = e.Item.ItemId;
    //    }

    //    void SetTabItemTextColor(BottomNavigationItemView bottomNavigationItemView, Android.Graphics.Color textColor)
    //    {
    //        var itemTitle = bottomNavigationItemView.GetChildAt(1);
    //        var smallTextView = ((TextView)((BaselineLayout)itemTitle).GetChildAt(0));
    //        var largeTextView = ((TextView)((BaselineLayout)itemTitle).GetChildAt(1));

    //        smallTextView.SetTextColor(textColor);
    //        largeTextView.SetTextColor(textColor);
    //    }
    //}
}
