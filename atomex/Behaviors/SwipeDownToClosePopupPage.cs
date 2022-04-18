using System;
using Xamarin.Forms;

namespace atomex.Behaviors
{
    public class SwipeDownToClosePopupPage : Behavior<View>
    {
        private PanGestureRecognizer PanGestureRecognizer { get; set; }
        private DateTimeOffset? StartPanDownTime { get; set; }
        private DateTimeOffset? EndPanDownTime { get; set; }
        private double TotalY { get; set; }
        private bool ReachedEdge { get; set; }
        /// <summary>
        /// Close action, depends on your navigation mode
        /// </summary>
        public event Action CloseAction;

        public static readonly BindableProperty ClosingEdgeProperty = BindableProperty.Create(propertyName: nameof(ClosingEdge)
                                                                                         , returnType: typeof(Double)
                                                                                         , declaringType: typeof(SwipeDownToClosePopupPage)
                                                                                         , defaultValue: Convert.ToDouble(100)
                                                                                         , defaultBindingMode: BindingMode.TwoWay
                                                                                         , propertyChanged: ClosingEdgePropertyChanged);
        /// <summary>
        /// The height from the bottom that will trigger close page 
        /// </summary>
        public Double ClosingEdge
        {
            get { return (Double)GetValue(ClosingEdgeProperty); }
            set { SetValue(ClosingEdgeProperty, Convert.ToDouble(value)); }
        }

        private static void ClosingEdgePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (SwipeDownToClosePopupPage)bindable;
            if (newValue != null)
            {
                control.ClosingEdge = Convert.ToDouble(newValue);
            }
        }


        public static readonly BindableProperty ClosingTimeInMsProperty = BindableProperty.Create(propertyName: nameof(ClosingTimeInMs)
                                                                                        , returnType: typeof(Int64)
                                                                                        , declaringType: typeof(SwipeDownToClosePopupPage)
                                                                                        , defaultValue: Convert.ToInt64(500)
                                                                                        , defaultBindingMode: BindingMode.TwoWay
                                                                                        , propertyChanged: ClosingTimeInMsPropertyChanged);
        /// <summary>
        /// Scroll time less than this value will trigger close page
        /// </summary>
        public Int64 ClosingTimeInMs
        {
            get { return (Int64)GetValue(ClosingTimeInMsProperty); }
            set { SetValue(ClosingTimeInMsProperty, Convert.ToInt64(value)); }
        }

        private static void ClosingTimeInMsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (SwipeDownToClosePopupPage)bindable;
            if (newValue != null)
            {
                control.ClosingTimeInMs = Convert.ToInt64(newValue);
            }
        }


        public SwipeDownToClosePopupPage()
        {
            this.PanGestureRecognizer = new PanGestureRecognizer();
        }

        protected override void OnAttachedTo(View v)
        {
            PanGestureRecognizer.PanUpdated += Pan_PanUpdated;
            v.GestureRecognizers.Add(this.PanGestureRecognizer);
            base.OnAttachedTo(v);
        }

        protected override void OnDetachingFrom(View v)
        {
            PanGestureRecognizer.PanUpdated -= Pan_PanUpdated;
            v.GestureRecognizers.Remove(this.PanGestureRecognizer);
            base.OnDetachingFrom(v);
        }

        private void Pan_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            View v = sender as View;
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    StartPanDownTime = DateTime.Now;
                    break;

                case GestureStatus.Running:
                    TotalY = e.TotalY;
                    if (TotalY > 0)
                    {
                        if (Device.RuntimePlatform == Device.Android)
                        {
                            v.TranslateTo(0, TotalY + v.TranslationY, 20, Easing.Linear);
                            //Too close to edge?
                            ReachedEdge = TotalY + v.TranslationY > v.Height - ClosingEdge;
                        }

                        else
                        {
                            v.TranslateTo(0, TotalY, 20, Easing.Linear);
                            //Too close to edge?
                            ReachedEdge = TotalY > v.Height - ClosingEdge;
                        }
                    }
                    break;

                case GestureStatus.Completed:
                    EndPanDownTime = DateTimeOffset.Now;
                    if ((EndPanDownTime.Value.ToUnixTimeMilliseconds() - StartPanDownTime.Value.ToUnixTimeMilliseconds() < ClosingTimeInMs
                        && TotalY > 0)
                        || ReachedEdge)
                        //Swipe too fast
                        CloseAction?.Invoke();
                    else
                    {
                        v.TranslateTo(0, 0, 20, Easing.Linear);
                    }
                    break;
            }

            if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                StartPanDownTime = null;
                EndPanDownTime = null;
            }
        }
    }
}