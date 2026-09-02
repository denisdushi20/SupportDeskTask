import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { Status } from '../../shared/models/enums';
import { CreateCommentRequest, Comment } from '../models/comment.model';
import { PagedResult } from '../models/paged-result.model';
import {
  CreateTicketRequest,
  TicketDetail,
  UpdateTicketRequest,
} from '../models/ticket-detail.model';
import { TicketListItem, TicketListQuery } from '../models/ticket-list.model';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly baseUrl: string;

  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) apiBaseUrl: string,
  ) {
    this.baseUrl = `${apiBaseUrl}/api/tickets`;
  }

  list(query: TicketListQuery = {}): Observable<PagedResult<TicketListItem>> {
    let params = new HttpParams();

    if (query.page != null) {
      params = params.set('page', query.page);
    }
    if (query.pageSize != null) {
      params = params.set('pageSize', query.pageSize);
    }
    if (query.search != null && query.search !== '') {
      params = params.set('search', query.search);
    }
    if (query.status != null) {
      params = params.set('status', query.status);
    }
    if (query.priority != null) {
      params = params.set('priority', query.priority);
    }
    if (query.assignedAgentId != null && query.assignedAgentId !== '') {
      params = params.set('assignedAgentId', query.assignedAgentId);
    }
    if (query.overdueOnly === true) {
      params = params.set('overdueOnly', 'true');
    }

    return this.http.get<PagedResult<TicketListItem>>(this.baseUrl, { params });
  }

  get(id: string): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateTicketRequest): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(this.baseUrl, request);
  }

  update(id: string, request: UpdateTicketRequest): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  assign(id: string, agentId: string): Observable<TicketDetail> {
    return this.http.put<TicketDetail>(`${this.baseUrl}/${id}/assignee`, {
      agentId,
    });
  }

  unassign(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/assignee`);
  }

  changeStatus(id: string, status: Status): Observable<TicketDetail> {
    return this.http.post<TicketDetail>(`${this.baseUrl}/${id}/status`, {
      status,
    });
  }

  addComment(id: string, request: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(`${this.baseUrl}/${id}/comments`, request);
  }
}
