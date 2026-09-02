import { filterAgents, agentFiltersActive, AgentDirectoryFilters } from './agent-directory.filter';
import { Agent } from '../models/agent.model';

describe('agentDirectoryFilter', () => {
  const agents: Agent[] = [
    {
      id: '1',
      fullName: 'Alex Technical',
      email: 'alex@example.com',
      department: 'Technical',
      active: true,
    },
    {
      id: '2',
      fullName: 'Blair Billing',
      email: 'blair@example.com',
      department: 'Billing',
      active: false,
    },
  ];

  it('filters by search, department, and active state', () => {
    const filters: AgentDirectoryFilters = {
      search: 'blair',
      department: 'Billing',
      active: 'inactive',
    };
    expect(filterAgents(agents, filters).map((a) => a.id)).toEqual(['2']);
  });

  it('detects active filters', () => {
    expect(agentFiltersActive({ search: '', department: '', active: '' })).toBeFalse();
    expect(agentFiltersActive({ search: 'a', department: '', active: '' })).toBeTrue();
  });
});
