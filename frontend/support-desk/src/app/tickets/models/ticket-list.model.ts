import { Priority, Status } from '../../shared/models/enums';

export interface TicketListItem {
  id: string;
  reference: string;
  title: string;
  customerName: string;
  priority: Priority;
  status: Status;
  assignedAgentId: string | null;
  assignedAgentName: string | null;
  createdDate: string;
  dueDate: string;
  lastModifiedDate: string;
  isOverdue: boolean;
}

export interface TicketListQuery {
  page?: number;
  pageSize?: number;
  search?: string | null;
  status?: Status | null;
  priority?: Priority | null;
  assignedAgentId?: string | null;
  overdueOnly?: boolean;
}
