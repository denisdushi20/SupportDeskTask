import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { Agent } from '../models/agent.model';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly baseUrl: string;
  private cachedList$: Observable<Agent[]> | null = null;

  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) apiBaseUrl: string,
  ) {
    this.baseUrl = `${apiBaseUrl}/api/agents`;
  }

  /** Full agent list (cached). Pass search to bypass cache for filtered lookups. */
  list(search?: string | null): Observable<Agent[]> {
    if (search != null && search !== '') {
      let params = new HttpParams().set('search', search);
      return this.http.get<Agent[]>(this.baseUrl, { params });
    }

    if (!this.cachedList$) {
      this.cachedList$ = this.http
        .get<Agent[]>(this.baseUrl)
        .pipe(shareReplay({ bufferSize: 1, refCount: true }));
    }
    return this.cachedList$;
  }

  get(id: string): Observable<Agent> {
    return this.http.get<Agent>(`${this.baseUrl}/${id}`);
  }

  /** Clears the cached unfiltered list (e.g. after rare admin changes). */
  invalidateCache(): void {
    this.cachedList$ = null;
  }
}
