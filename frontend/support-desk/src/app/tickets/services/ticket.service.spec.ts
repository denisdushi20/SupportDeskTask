import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { extractApiError } from '../../core/errors/api-error.util';
import { TicketService } from './ticket.service';

describe('TicketService', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('list serializes query parameters correctly', () => {
    service
      .list({
        page: 2,
        pageSize: 20,
        search: 'printer',
        status: 'InProgress',
        priority: 'High',
        assignedAgentId: '11111111-1111-1111-1111-111111111111',
        overdueOnly: true,
      })
      .subscribe((result) => {
        expect(result.items.length).toBe(1);
        expect(result.totalCount).toBe(1);
      });

    const req = httpMock.expectOne(
      (r) => r.url === '/api/tickets' && r.method === 'GET',
    );
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.get('search')).toBe('printer');
    expect(req.request.params.get('status')).toBe('InProgress');
    expect(req.request.params.get('priority')).toBe('High');
    expect(req.request.params.get('assignedAgentId')).toBe(
      '11111111-1111-1111-1111-111111111111',
    );
    expect(req.request.params.get('overdueOnly')).toBe('true');

    req.flush({
      items: [
        {
          id: '22222222-2222-2222-2222-222222222222',
          reference: 'TKT-0001',
          title: 'Printer jam',
          customerName: 'Ada',
          priority: 'High',
          status: 'InProgress',
          assignedAgentId: '11111111-1111-1111-1111-111111111111',
          assignedAgentName: 'Agent One',
          createdDate: '2026-01-01T00:00:00+00:00',
          dueDate: '2026-01-02T00:00:00+00:00',
          lastModifiedDate: '2026-01-01T00:00:00+00:00',
          isOverdue: false,
        },
      ],
      page: 2,
      pageSize: 20,
      totalCount: 1,
    });
  });

  it('list omits null and undefined query parameters', () => {
    service.list({ page: 1, pageSize: 20, search: null, status: null }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/tickets');
    expect(req.request.params.has('search')).toBeFalse();
    expect(req.request.params.has('status')).toBeFalse();
    expect(req.request.params.has('overdueOnly')).toBeFalse();
    req.flush({ items: [], page: 1, pageSize: 20, totalCount: 0 });
  });

  it('maps ProblemDetails 409 into a readable ApiError', () => {
    let caught: unknown;
    service.delete('33333333-3333-3333-3333-333333333333').subscribe({
      error: (err) => {
        caught = err;
      },
    });

    const req = httpMock.expectOne('/api/tickets/33333333-3333-3333-3333-333333333333');
    req.flush(
      {
        type: 'https://httpstatuses.com/409',
        title: 'TICKET_CLOSED',
        status: 409,
        detail: 'Closed tickets are read-only and cannot be modified.',
        code: 'TICKET_CLOSED',
      },
      { status: 409, statusText: 'Conflict' },
    );

    const apiError = extractApiError(caught);
    expect(apiError.status).toBe(409);
    expect(apiError.code).toBe('TICKET_CLOSED');
    expect(apiError.detail).toContain('Closed tickets are read-only');
  });
});
