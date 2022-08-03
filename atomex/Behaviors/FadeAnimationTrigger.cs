using Xamarin.Forms;

namespace atomex.Behaviors
{
    public class FadeAnimationTrigger : TriggerAction<Image>
    {
        public AnimationAction Action { get; set; }

        public enum AnimationAction
        {
            Start,
            Stop
        }

        protected override void Invoke(Image sender)
        {
            if (sender != null)
            {
                if (Action == AnimationAction.Start)
                    StartAnimation(sender);
                else if (Action == AnimationAction.Stop)
                    CancelAnimation(sender);
                
            }
        }

        private void StartAnimation(Image myElement)
        {
            var animation = new Animation();
            var anim1 = new Animation(v => myElement.Opacity = v, 0.2, 0.8);
            var anim2 = new Animation(v => myElement.Opacity = v, 0.8, 0.2);
            animation.Add(0,0.5, anim1);
            animation.Insert(0.5, 1, anim2);
            animation.Commit(myElement, "FadeAnimation", length: 1500, easing: Easing.Linear, repeat:() => true);
        }

        private void CancelAnimation(Image myElement)
        {
            myElement.Opacity = 1;
            myElement.AbortAnimation("FadeAnimation");
        }
    }
}