namespace atomex.Services
{
    public enum ToastPosition { Top, Center, Bottom }

    public interface IToastService
    {
        void Show(string message, ToastPosition toastPosition = ToastPosition.Bottom);
    }
}
