namespace Lab2.Services.Message
{
    public interface IMessageService
    {
        string Message { get; }
        MessageType Type { get; }
        bool HasMessage { get; }

        void ShowError(string message);
        void ShowWarning(string message);
        void Clear();
    }
}