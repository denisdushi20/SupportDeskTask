using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;

namespace SupportDesk.Tests.Policies;

public class TicketTransitionPolicyTests
{
    [Theory]
    [InlineData(Status.New, Status.InProgress)]
    [InlineData(Status.InProgress, Status.Resolved)]
    [InlineData(Status.Resolved, Status.Closed)]
    [InlineData(Status.Resolved, Status.InProgress)]
    public void Allowed_transitions_are_accepted(Status current, Status requested)
    {
        var decision = TicketTransitionPolicy.Evaluate(current, requested);

        Assert.True(decision.IsAllowed);
        Assert.Null(decision.ErrorCode);
    }

    [Theory]
    [InlineData(Status.New, Status.Resolved)]
    [InlineData(Status.New, Status.Closed)]
    [InlineData(Status.InProgress, Status.Closed)]
    [InlineData(Status.Resolved, Status.New)]
    [InlineData(Status.InProgress, Status.New)]
    [InlineData(Status.New, Status.New)]
    [InlineData(Status.InProgress, Status.InProgress)]
    [InlineData(Status.Resolved, Status.Resolved)]
    public void Invalid_transitions_are_rejected(Status current, Status requested)
    {
        var decision = TicketTransitionPolicy.Evaluate(current, requested);

        Assert.False(decision.IsAllowed);
        Assert.False(string.IsNullOrWhiteSpace(decision.ErrorCode));
        Assert.False(string.IsNullOrWhiteSpace(decision.Reason));
    }

    [Theory]
    [InlineData(Status.New)]
    [InlineData(Status.InProgress)]
    [InlineData(Status.Resolved)]
    [InlineData(Status.Closed)]
    public void Closed_transitions_are_rejected_as_ticket_closed(Status requested)
    {
        var decision = TicketTransitionPolicy.Evaluate(Status.Closed, requested);

        Assert.False(decision.IsAllowed);
        Assert.Equal(DomainErrorCodes.TicketClosed, decision.ErrorCode);
    }

    [Fact]
    public void New_to_Resolved_is_rejected_as_invalid_transition()
    {
        var decision = TicketTransitionPolicy.Evaluate(Status.New, Status.Resolved);

        Assert.False(decision.IsAllowed);
        Assert.Equal(DomainErrorCodes.InvalidStatusTransition, decision.ErrorCode);
    }

    [Fact]
    public void Structural_policy_does_not_require_assignment_context()
    {
        // InProgress → Resolved is structurally allowed; active-assignee checks belong to the application service.
        var decision = TicketTransitionPolicy.Evaluate(Status.InProgress, Status.Resolved);

        Assert.True(decision.IsAllowed);
    }
}
