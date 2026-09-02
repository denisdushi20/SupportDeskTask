import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ApiError } from '../../core/errors/api-error.model';
import { apiErrorMessage, extractApiError } from '../../core/errors/api-error.util';
import { departmentLabel } from '../../shared/models/display';
import { Department } from '../../shared/models/enums';
import { Agent } from '../models/agent.model';
import { AgentService } from '../services/agent.service';
import {
  AgentActiveFilter,
  agentFiltersActive,
  filterAgents,
} from '../utils/agent-directory.filter';

const DEPARTMENTS: readonly Department[] = ['Technical', 'Billing', 'General'];

@Component({
  selector: 'app-agent-list-page',
  imports: [ReactiveFormsModule],
  templateUrl: './agent-list.page.html',
  styleUrl: './agent-list.page.scss',
})
export class AgentListPage implements OnInit {
  private readonly agentsApi = inject(AgentService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly departmentLabel = departmentLabel;
  readonly departments = DEPARTMENTS;

  readonly allAgents = signal<Agent[]>([]);
  readonly loading = signal(true);
  readonly error = signal<ApiError | null>(null);

  readonly filterForm = this.fb.nonNullable.group({
    search: [''],
    department: ['' as '' | Department],
    active: ['' as AgentActiveFilter],
  });

  /** Presentation filters applied in the browser over the loaded directory. */
  readonly filterState = signal({
    search: '',
    department: '' as '' | Department,
    active: '' as AgentActiveFilter,
  });

  readonly filteredAgents = computed(() => filterAgents(this.allAgents(), this.filterState()));
  readonly filtersActive = computed(() => agentFiltersActive(this.filterState()));

  ngOnInit(): void {
    this.filterForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const v = this.filterForm.getRawValue();
      this.filterState.set({
        search: v.search,
        department: v.department,
        active: v.active,
      });
    });

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
          this.allAgents.set(list);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(extractApiError(err));
          this.allAgents.set([]);
        },
      });
  }

  clearFilters(): void {
    this.filterForm.setValue({ search: '', department: '', active: '' });
  }

  errorMessage(): string {
    const err = this.error();
    return err ? apiErrorMessage(err) : '';
  }
}
