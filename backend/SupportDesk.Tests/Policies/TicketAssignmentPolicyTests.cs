using SupportDesk.Domain.Policies;

namespace SupportDesk.Tests.Policies;

public class TicketAssignmentPolicyTests
{
    [Fact]
    public void Active_agent_is_assignable()
    {
        var decision = TicketAssignmentPolicy.EvaluateAssign(agentIsActive: true);

        Assert.True(decision.IsAllowed);
        Assert.Null(decision.ErrorCode);
    }

    [Fact]
    public void Inactive_agent_is_rejected()
    {
        var decision = TicketAssignmentPolicy.EvaluateAssign(agentIsActive: false);

        Assert.False(decision.IsAllowed);
        Assert.Equal(DomainErrorCodes.AgentInactive, decision.ErrorCode);
    }
}
