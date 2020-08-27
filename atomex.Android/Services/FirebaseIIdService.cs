using Android.App;
using Firebase.Iid;

namespace atomex.Droid.Services
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseIIdService : FirebaseInstanceIdService
    {
        const string TAG = "FirebaseIIdService";

        public override void OnTokenRefresh()
        {
            var refreshedToken = FirebaseInstanceId.Instance.Token;
            SendRegistrationTokenToServer(refreshedToken);
        }

        public void SendRegistrationTokenToServer(string token)
        {
            // send token to server
        }
    }
}
