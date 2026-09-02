import { Department } from '../../shared/models/enums';

export interface Agent {
  id: string;
  fullName: string;
  email: string;
  department: Department;
  active: boolean;
}

export interface AgentSummary {
  id: string;
  fullName: string;
  email: string;
  department: Department;
  active: boolean;
}

export interface AgentListQuery {
  search?: string | null;
}
