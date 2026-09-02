/** Pure helpers for Overview metrics. Counts come from unfiltered API totalCount queries. */
export function computeOpenCount(total: number, closed: number): number {
  return Math.max(0, total - closed);
}

export interface StatusShare {
  status: 'New' | 'InProgress' | 'Resolved' | 'Closed';
  count: number;
  percent: number;
}

/** Build status share rows for the overview distribution display. */
export function buildStatusShares(
  counts: Record<'New' | 'InProgress' | 'Resolved' | 'Closed', number>,
): StatusShare[] {
  const order: Array<StatusShare['status']> = ['New', 'InProgress', 'Resolved', 'Closed'];
  const total = order.reduce((sum, key) => sum + Math.max(0, counts[key] ?? 0), 0);
  return order.map((status) => {
    const count = Math.max(0, counts[status] ?? 0);
    const percent = total === 0 ? 0 : Math.round((count / total) * 100);
    return { status, count, percent };
  });
}
