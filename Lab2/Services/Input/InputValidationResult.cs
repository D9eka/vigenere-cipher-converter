using Lab2.Services.Message;

namespace Lab2.Services.Input;

public class InputValidationResult
{
    public bool IsValid => Type != MessageType.Error;
    public MessageType Type { get; }
    public string Message { get; }

    private InputValidationResult(MessageType type, string message)
    {
        Type = type;
        Message = message;
    }

    public static InputValidationResult Success() => new(MessageType.None, string.Empty);
    public static InputValidationResult Warning(string msg) => new(MessageType.Warning, msg);
    public static InputValidationResult Error(string msg) => new(MessageType.Error, msg);
}
