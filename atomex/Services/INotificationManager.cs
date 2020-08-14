using System;
namespace atomex.Services
{
    public interface INotificationManager
    {
        event EventHandler NotificationReceived;

        void Initialize();

        int ScheduleNotification(string title, string message);

        void ReceiveNotification(long swapId, string currency, string txId, string pushType);

        void RemoveNotifications();
    }
}
