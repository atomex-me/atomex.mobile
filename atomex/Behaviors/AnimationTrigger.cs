using Xamarin.Forms;

namespace atomex.Behaviors
{
    public class AnimationTrigger : TriggerAction<Image>
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
                {
                    StartAnimation(sender);
                }
                else if (Action == AnimationAction.Stop)
                {
                    FinishAnimation(sender);
                    CancelAnimation(sender);
                }
            }
        }

        private void StartAnimation(Image myElement)
        {   
            new Animation
            {
                //{ 0, 0.5, new Animation (v => myElement.Scale = v, 1, 2) },
                { 0, 1, new Animation (v => myElement.Rotation = v, myElement.Rotation, -180)},
            }.Commit(myElement, "ChildAnimations", 16, 1000, Easing.Linear, null, () => true);
        }

        private void FinishAnimation(Image myElement)
        {
            new Animation {
                { 0, 1, new Animation (v =>
                    {
                        if (myElement.Rotation == 0)
                            return;

                        myElement.Rotation = v;
                    },
                    myElement.Rotation, -180)
                }
            }.Commit(myElement, "FinishAnimations", 16, 1000, Easing.Linear, (v, s) =>
                {
                    myElement.Rotation = 0;
                },
                () => false);
        }

        private void CancelAnimation(Image myElement)
        {
            //ViewExtensions.CancelAnimations(myElement);
            myElement.AbortAnimation("ChildAnimations");
        }
    }
}
