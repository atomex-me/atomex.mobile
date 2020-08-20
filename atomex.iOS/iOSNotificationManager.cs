using System;
using atomex.Models;
using atomex.Services;
using Newtonsoft.Json.Linq;
using UserNotifications;
using Xamarin.Forms;

[assembly: Dependency(typeof(atomex.iOS.iOSNotificationManager))]
namespace atomex.iOS
{
    public class iOSNotificationManager : INotificationManager
    {
        int messageId = -1;

        bool hasNotificationsPermission;

        public event EventHandler NotificationReceived;

        public void Initialize()
        {
            // request the permission to use local notifications
            UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge, (approved, err) =>
            {
                hasNotificationsPermission = approved;
            });
        }

        // emulate firebase message
        public int ScheduleNotification(string message)
        {
            // EARLY OUT: app doesn't have permissions
            if (!hasNotificationsPermission)
            {
                return -1;
            }

            messageId++;

            var content = new UNMutableNotificationContent()
            {
                Title = "Atomex",
                Subtitle = "",
                Body = message,
                Sound = UNNotificationSound.Default
            };

            // Local notifications can be time or location based
            // Create a time-based trigger, interval is in seconds and must be greater than 0

            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(5, false);

            //var request = UNNotificationRequest.FromIdentifier("swap", content, trigger);
            var request = UNNotificationRequest.FromIdentifier(messageId.ToString(), content, trigger);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
            {
                if (err != null)
                {
                    throw new Exception($"Failed to schedule notification: {err}");
                }
            });

            return messageId;
        }

        public void ReceiveNotification(string title, string message)
        {
            JObject o = JObject.Parse(message);

            var args = new NotificationEventArgs()
            {
                SwapId = long.Parse(o["swapId"].ToString()),
                Currency = o["currency"].ToString(),
                TxId = o["txId"].ToString(),
                PushType = o["type"].ToString()
            };
            NotificationReceived?.Invoke(null, args);
        }

        public void RemoveNotifications()
        {
            UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
        }
    }
}
