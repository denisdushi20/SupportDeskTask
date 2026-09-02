/** Pure helpers for Overview metrics. Counts come from unfiltered API totalCount queries. */
export function computeOpenCount(total: number, closed: number): number {
  return Math.max(0, total - closed);
}
