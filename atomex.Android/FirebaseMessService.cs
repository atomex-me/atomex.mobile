﻿using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using atomex.Services;
using Firebase.Messaging;
using Xamarin.Forms;
using Android.Support.V4.App;
using Android.Graphics;

namespace atomex.Droid
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class FirebaseMessService : FirebaseMessagingService
    {
        public FirebaseMessService()
        {
        }

        //called in foreground
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);
            var body = message.GetNotification().Body;
            SendNotification(body, message.Data);
        }

        void SendNotification(string messageBody, IDictionary<string, string> data)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            foreach (var key in data.Keys)
            {
                intent.PutExtra(key, data[key]);
            }

            var pendingIntent = PendingIntent.GetActivity(this,
                                                          AndroidNotificationManager.NOTIFICATION_ID,
                                                          intent,
                                                          PendingIntentFlags.OneShot);

            var notificationBuilder = new NotificationCompat.Builder(this, AndroidNotificationManager.CHANNEL_ID)
                                      //.SetSmallIcon(Resource.Drawable.ic_stat_ic_notification)
                                      .SetContentIntent(pendingIntent)
                                      .SetContentTitle("Atomex")
                                      .SetContentText(messageBody)
                                      .SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.ic_launcher))
                                      .SetSmallIcon(Resource.Drawable.ic_launcher)
                                      .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate);

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(AndroidNotificationManager.NOTIFICATION_ID, notificationBuilder.Build());
        }
    }
}
