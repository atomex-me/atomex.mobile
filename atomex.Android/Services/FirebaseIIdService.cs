using Android.App;
using Firebase.Iid;

namespace atomex.Droid.Services
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseIIdService : FirebaseInstanceIdService
    {
        const string TAG = "FirebaseIIdService";

        private string DeviceToken;

        public override void OnTokenRefresh()
        {
            DeviceToken = FirebaseInstanceId.Instance.Token;

            SendRegistrationTokenToServer(DeviceToken);
        }

        public void SendRegistrationTokenToServer(string token)
        {
            // send token to server
        }
    }
}
