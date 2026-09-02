import { Routes } from '@angular/router';
import { OverviewPage } from './overview/pages/overview.page';
import { AgentListPage } from './agents/pages/agent-list.page';
import { TicketListPage } from './tickets/pages/ticket-list.page';
import { TicketCreatePage } from './tickets/pages/ticket-create.page';
import { TicketDetailPage } from './tickets/pages/ticket-detail.page';
import { TicketEditPage } from './tickets/pages/ticket-edit.page';
import { NotFoundPage } from './shared/pages/not-found.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'overview' },
  { path: 'overview', component: OverviewPage },
  { path: 'tickets', component: TicketListPage },
  { path: 'tickets/new', component: TicketCreatePage },
  { path: 'tickets/:id', component: TicketDetailPage },
  { path: 'tickets/:id/edit', component: TicketEditPage },
  { path: 'agents', component: AgentListPage },
  { path: '**', component: NotFoundPage },
];
