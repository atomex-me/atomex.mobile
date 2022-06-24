using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class BindableToolbarItem : ToolbarItem
    {
        public static BindableProperty IsVisibleProperty = BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(BindableToolbarItem), true, BindingMode.TwoWay, HandleValidateValueDelegate, OnIsVisibleChanged);

        public BindableToolbarItem()
        {
            if (!this.IsVisible)
            {
                InitVisibility(this.IsVisible);
            }
            else
            {
                InitVisibility();
            }
        }

        private static bool HandleValidateValueDelegate(BindableObject bindable, object newValue)
        {
            return newValue is bool;
        }

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set
            {
                if (!IsVisible && !value)
                {
                    SetValue(IsVisibleProperty, value);
                    InitVisibility();
                }
                else
                {
                    SetValue(IsVisibleProperty, value);
                }
            }
        }

        private void InitVisibility()
        {
            OnIsVisibleChanged(this, false, IsVisible);
        }

        private void InitVisibility(bool value)
        {
            if (!(bool)value)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var items = ((ContentPage)this.Parent).ToolbarItems;
                    if (items.Contains(this))
                    {
                        items.Remove(this);
                    }
                });
            }
        }

        private static void OnIsVisibleChanged(
            BindableObject bindable, object oldValue, object newValue)
        {
            var item = bindable as BindableToolbarItem;

            if (item != null && item.Parent == null)
                return;

            if (item != null)
            {
                var items = ((ContentPage)item.Parent).ToolbarItems;

                if (Equals(items, null)) return;

                if ((bool)newValue && !items.Contains(item))
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        if (!items.Contains(item))
                        {
                            items.Add(item);
                        }
                    });
                }
                else if ((bool)newValue && items.Contains(item) && !(bool)oldValue)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        items.Add(item);
                    });
                }
                else if (!(bool)newValue && items.Contains(item))
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        items.Remove(item);
                    });
                }
                else if (!(bool)newValue && !items.Contains(item))
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        if (items.Contains(item))
                        {
                            items.Remove(item);
                        }
                    });
                }
            }
        }
    }
}
