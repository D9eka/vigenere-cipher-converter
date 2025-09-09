namespace Lab2.Services.Message
{
    public class MessageService : IMessageService
    {
        public string Message { get; private set; } = string.Empty;
        public MessageType Type { get; private set; } = MessageType.None;
        public bool HasMessage => Type != MessageType.None && !string.IsNullOrWhiteSpace(Message);

        public void ShowError(string message)
        {
            Message = message;
            Type = MessageType.Error;
        }

        public void ShowWarning(string message)
        {
            Message = message;
            Type = MessageType.Warning;
        }

        public void Clear()
        {
            Message = string.Empty;
            Type = MessageType.None;
        }
    }
}
