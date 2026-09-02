namespace SupportDesk.Domain.Policies;

public sealed class PolicyDecision
{
    public bool IsAllowed { get; }

    public string? ErrorCode { get; }

    public string? Reason { get; }

    private PolicyDecision(bool isAllowed, string? errorCode, string? reason)
    {
        IsAllowed = isAllowed;
        ErrorCode = errorCode;
        Reason = reason;
    }

    public static PolicyDecision Allow() => new(true, null, null);

    public static PolicyDecision Reject(string errorCode, string reason) =>
        new(false, errorCode, reason);
}
