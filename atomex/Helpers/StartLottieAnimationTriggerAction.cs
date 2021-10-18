using Lottie.Forms;
using Xamarin.Forms;

namespace atomex.Helpers
{
    public class StartLottieAnimationTriggerAction : TriggerAction<AnimationView>
    {
        protected override void Invoke(AnimationView sender)
        {
            sender.PlayAnimation();
        }
    }
}
