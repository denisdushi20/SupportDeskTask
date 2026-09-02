import { Agent } from '../models/agent.model';
import { Department } from '../../shared/models/enums';

export type AgentActiveFilter = '' | 'active' | 'inactive';

export interface AgentDirectoryFilters {
  search: string;
  department: '' | Department;
  active: AgentActiveFilter;
}

/** Client-side directory filter for the small agents reference list. Not server-side. */
export function filterAgents(agents: readonly Agent[], filters: AgentDirectoryFilters): Agent[] {
  const term = filters.search.trim().toLowerCase();
  return agents.filter((agent) => {
    if (term) {
      const hay = `${agent.fullName} ${agent.email}`.toLowerCase();
      if (!hay.includes(term)) {
        return false;
      }
    }
    if (filters.department && agent.department !== filters.department) {
      return false;
    }
    if (filters.active === 'active' && !agent.active) {
      return false;
    }
    if (filters.active === 'inactive' && agent.active) {
      return false;
    }
    return true;
  });
}

export function agentFiltersActive(filters: AgentDirectoryFilters): boolean {
  return (
    filters.search.trim() !== '' || filters.department !== '' || filters.active !== ''
  );
}
