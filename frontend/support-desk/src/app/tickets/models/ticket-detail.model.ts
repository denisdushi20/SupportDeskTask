import { Priority, Status } from '../../shared/models/enums';
import { AgentSummary } from '../../agents/models/agent.model';
import { Comment } from './comment.model';

export interface TicketDetail {
  id: string;
  reference: string;
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
  status: Status;
  assignedAgentId: string | null;
  assignedAgent: AgentSummary | null;
  createdDate: string;
  lastModifiedDate: string;
  resolvedDate: string | null;
  closedDate: string | null;
  dueDate: string;
  isOverdue: boolean;
  allowedTransitions: Status[];
  canEditFields: boolean;
  canAssign: boolean;
  canUnassign: boolean;
  canAddComment: boolean;
  canDelete: boolean;
  comments: Comment[];
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
}

export interface UpdateTicketRequest {
  title: string;
  description: string;
  customerName: string;
  customerEmail: string;
  priority: Priority;
}

export interface AssignAgentRequest {
  agentId: string;
}

export interface ChangeStatusRequest {
  status: Status;
}
