namespace SupportDesk.Api.Application.Common;

public sealed class AppError
{
    public AppError(
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        Code = code;
        Message = message;
        Context = context;
    }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, object?>? Context { get; }

    public static AppError Create(
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? context = null) =>
        new(code, message, context);
}
