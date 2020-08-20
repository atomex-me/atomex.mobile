using System;

namespace atomex.Services
{
    public interface INotificationManager
    {
        event EventHandler NotificationReceived;

        void Initialize();

        int ScheduleNotification(string message);

        void ReceiveNotification(string title, string message);

        void RemoveNotifications();
    }
}
