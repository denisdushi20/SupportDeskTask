import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiError } from '../../core/errors/api-error.model';
import { apiErrorMessage, extractApiError } from '../../core/errors/api-error.util';
import { departmentLabel } from '../../shared/models/display';
import { Agent } from '../models/agent.model';
import { AgentService } from '../services/agent.service';

@Component({
  selector: 'app-agent-list-page',
  templateUrl: './agent-list.page.html',
  styleUrl: './agent-list.page.scss',
})
export class AgentListPage implements OnInit {
  private readonly agentsApi = inject(AgentService);
  private readonly destroyRef = inject(DestroyRef);

  readonly departmentLabel = departmentLabel;
  readonly agents = signal<Agent[]>([]);
  readonly loading = signal(true);
  readonly error = signal<ApiError | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.agentsApi
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.loading.set(false);
          this.agents.set(list);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(extractApiError(err));
          this.agents.set([]);
        },
      });
  }

  errorMessage(): string {
    const err = this.error();
    return err ? apiErrorMessage(err) : '';
  }
}
