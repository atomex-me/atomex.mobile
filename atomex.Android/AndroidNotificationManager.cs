using System;
using atomex.Services;
using Android.Support.V4.App;
using Android.App;
using Android.Content;
using Android.Graphics;
using atomex.Models;
using Android.OS;
using Xamarin.Forms;

[assembly: Dependency(typeof(atomex.Droid.AndroidNotificationManager))]
namespace atomex.Droid
{
    public class AndroidNotificationManager : INotificationManager
    {
        const int pendingIntentId = 0;


        internal static readonly string CHANNEL_ID = "atomex_notifications_channel_id";
        internal static readonly string CHANNEL_NAME = "atomex_notifications_channel_name";
        internal static readonly int NOTIFICATION_ID = 100;

        public const string TitleKey = "title";
        public const string MessageKey = "message";

        bool channelInitialized = false;
        int messageId = -1;
        NotificationManager manager;

        public event EventHandler NotificationReceived;

        public void Initialize()
        {
            CreateNotificationChannel();
        }

        public int ScheduleNotification(string title, string message)
        {
            //must be called in background
            if (!channelInitialized)
            {
                CreateNotificationChannel();
            }

            messageId++;

            Intent intent = new Intent(Android.App.Application.Context, typeof(MainActivity));
            intent.PutExtra(TitleKey, title);
            intent.PutExtra(MessageKey, message);

            PendingIntent pendingIntent = PendingIntent.GetActivity(Android.App.Application.Context, pendingIntentId, intent, PendingIntentFlags.OneShot);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(Android.App.Application.Context, CHANNEL_ID)
                .SetContentIntent(pendingIntent)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_launcher))
                .SetSmallIcon(Resource.Drawable.ic_stat_send)
                .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate);

            var notification = builder.Build();
            manager.Notify(messageId, notification);

            return messageId;
        }

        public void ReceiveNotification(string title, string message)
        {
            var args = new NotificationEventArgs()
            {
                Title = title,
                Message = message,
            };
            NotificationReceived?.Invoke(null, args);
        }

        void CreateNotificationChannel()
        {
            manager = (NotificationManager)Android.App.Application.Context.GetSystemService(Android.App.Application.NotificationService);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Default);
                manager.CreateNotificationChannel(channel);
            }

            channelInitialized = true;
        }

        public void RemoveNotifications()
        {
            throw new NotImplementedException();
        }
    }
}
