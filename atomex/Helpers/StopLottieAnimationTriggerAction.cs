using Lottie.Forms;
using Xamarin.Forms;

namespace atomex.Helpers
{
    public class StopLottieAnimationTriggerAction : TriggerAction<AnimationView>
    {
        protected override void Invoke(AnimationView sender)
        {
            sender.PauseAnimation();
        }
    }
}
