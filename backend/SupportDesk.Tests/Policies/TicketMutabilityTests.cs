using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;

namespace SupportDesk.Tests.Policies;

public class TicketMutabilityTests
{
    [Theory]
    [InlineData(Status.New, true)]
    [InlineData(Status.InProgress, true)]
    [InlineData(Status.Resolved, false)]
    [InlineData(Status.Closed, false)]
    public void Open_status_definition(Status status, bool expectedOpen)
    {
        Assert.Equal(expectedOpen, TicketMutability.IsOpen(status));
        Assert.Equal(expectedOpen, TicketMutability.CanEditFields(status));
    }

    [Fact]
    public void Closed_ticket_cannot_mutate()
    {
        Assert.False(TicketMutability.CanMutate(Status.Closed));

        var decision = TicketMutability.EnsureNotClosed(Status.Closed);

        Assert.False(decision.IsAllowed);
        Assert.Equal(DomainErrorCodes.TicketClosed, decision.ErrorCode);
    }

    [Theory]
    [InlineData(Status.New)]
    [InlineData(Status.InProgress)]
    [InlineData(Status.Resolved)]
    public void Non_closed_tickets_can_mutate(Status status)
    {
        Assert.True(TicketMutability.CanMutate(status));
        Assert.True(TicketMutability.EnsureNotClosed(status).IsAllowed);
    }

    [Fact]
    public void Resolved_ticket_fields_are_not_editable()
    {
        var decision = TicketMutability.EnsureCanEditFields(Status.Resolved);

        Assert.False(decision.IsAllowed);
        Assert.Equal(DomainErrorCodes.TicketNotEditable, decision.ErrorCode);
    }

    [Fact]
    public void Closed_ticket_field_edit_uses_closed_error()
    {
        var decision = TicketMutability.EnsureCanEditFields(Status.Closed);

        Assert.False(decision.IsAllowed);
        Assert.Equal(DomainErrorCodes.TicketClosed, decision.ErrorCode);
    }
}
