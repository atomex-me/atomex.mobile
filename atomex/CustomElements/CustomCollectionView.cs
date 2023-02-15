using System;
using System.Linq;
using Serilog;
using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomCollectionView : CollectionView
    {
        private ScrollView _scrollView;
        private double _previousScrollViewPosition = 0;
        private int _columns;
        private double ThresholdSpace => RowHeight * 2;
        private int BindingItemCount => Convert.ToInt32(ItemsSource?.Cast<object>().ToList().Count);

        public static readonly BindableProperty RowHeightProperty =
            BindableProperty.CreateAttached("RowHeight",
                typeof(int),
                typeof(CustomCollectionView),
                24);

        public int RowHeight
        {
            get => (int) GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        public static readonly BindableProperty GroupHeaderHeightProperty =
            BindableProperty.CreateAttached("GroupHeaderHeight",
                typeof(int),
                typeof(CustomCollectionView),
                12);

        public int GroupHeaderHeight
        {
            get => (int) GetValue(GroupHeaderHeightProperty);
            set => SetValue(GroupHeaderHeightProperty, value);
        }

        public static readonly BindableProperty RowCountProperty =
            BindableProperty.CreateAttached("RowCount",
                typeof(int),
                typeof(CustomCollectionView),
                0,
                propertyChanged: (bo, o1, o2) =>
                {
                    var collectionView = bo as CustomCollectionView;
                    collectionView?.UpdateHeight();
                });

        public int RowCount
        {
            get => IsGrouped 
                ? (int) GetValue(RowCountProperty)
                : Convert.ToInt32(ItemsSource.Cast<object>().ToList().Count) ;
            set => SetValue(RowCountProperty, 
                IsGrouped 
                    ? value 
                    : Convert.ToInt32(ItemsSource.Cast<object>().ToList().Count));
        }

        [TypeConverter(typeof(ReferenceTypeConverter))]
        public ScrollView ScrollView
        {
            set
            {
                _scrollView = value;
                _scrollView.Scrolled += _scrollView_Scrolled;
            }
        }

        private void _scrollView_Scrolled(object sender, ScrolledEventArgs e)
        {
            try
            {
                if (!IsVisible) return;
                
                if (_previousScrollViewPosition < e.ScrollY)
                {
                    //scrolled down
                    var scrollingSpace = _scrollView.ContentSize.Height - _scrollView.Height - ThresholdSpace;
                    if (scrollingSpace <= e.ScrollY)
                        RemainingItemsThresholdReachedCommand?.Execute(
                            RemainingItemsThresholdReachedCommandParameter); // Touched bottom view
                }
                
                _previousScrollViewPosition = e.ScrollY;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Custom collection view scrolled error");
            }
        }

        private void UpdateHeight()
        {
            try
            {
                if (_columns == 0)
                {
                    _columns = ItemsLayout is GridItemsLayout layout
                        ? layout.Span
                        : 1;
                }

                var header = Header as VisualElement;
                var footer = Footer as VisualElement;

                var headerHeight = header?.IsVisible ?? false
                    ? header.HeightRequest
                    : 0;
                
                var footerHeight = footer?.IsVisible ?? false
                    ? footer.HeightRequest
                    : 0;

                if (footerHeight < 0) footerHeight = 0;
                if (headerHeight < 0) headerHeight = 0;

                HeightRequest = IsGrouped
                    ? RowCount * RowHeight + (BindingItemCount == 0 ? RowCount : BindingItemCount) * GroupHeaderHeight + footerHeight + headerHeight
                    : RowCount * RowHeight / _columns + footerHeight + headerHeight;
            }
            catch (Exception e)
            {
                Log.Error(e, "Update height of custom collection view error");
            }
        }
    }
}