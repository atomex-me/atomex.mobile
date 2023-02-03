using System;
using System.Linq;
using Serilog;
using Xamarin.Forms;

namespace atomex.CustomElements
{
    public class CustomCollectionView : CollectionView
    {
        private ScrollView _scrollView;
        private int _columns;
        private int RowCount => Convert.ToInt32(ItemsSource.Cast<object>().ToList().Count);

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

        public static readonly BindableProperty GroupedRowCountProperty =
            BindableProperty.CreateAttached("GroupedRowCount",
                typeof(int),
                typeof(CustomCollectionView),
                0);

        public int GroupedRowCount
        {
            get => (int) GetValue(GroupedRowCountProperty);
            set => SetValue(GroupedRowCountProperty, value);
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
                var scrollingSpace = _scrollView.ContentSize.Height - _scrollView.Height;
                if (scrollingSpace <= e.ScrollY)
                    RemainingItemsThresholdReachedCommand?.Execute(
                        RemainingItemsThresholdReachedCommandParameter); // Touched bottom view
            }
            catch (Exception exception)
            {
                Log.Error(exception, "CustomCollectionView scrolled error");
            }
        }

        private void UpdateHeight()
        {
            try
            {
                if (_columns == 0)
                {
                    if (ItemsLayout is GridItemsLayout layout)
                        _columns = layout.Span;
                    else
                        _columns = 1;
                }

                var footer = Footer as VisualElement;

                var footerHeight = footer?.IsVisible ?? false
                    ? footer.HeightRequest
                    : 0;

                if (footerHeight < 0) footerHeight = 0;

                HeightRequest = IsGrouped
                    ? GroupedRowCount * RowHeight + RowCount * GroupHeaderHeight + footerHeight
                    : RowHeight * RowCount / _columns + footerHeight;
            }
            catch (Exception e)
            {
                Log.Error(e, "Update height of CustomCollectionView error");
            }
        }

        protected override void OnChildAdded(Element child)
        {
            base.OnChildAdded(child);
            UpdateHeight();
        }

        protected override void OnChildRemoved(Element child, int oldLogicalIndex)
        {
            base.OnChildRemoved(child, oldLogicalIndex);
            UpdateHeight();
        }
    }
}