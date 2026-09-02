import { buildStatusShares, computeOpenCount } from './overview-metrics';

describe('overview metrics', () => {
  it('computes Open as Total minus Closed', () => {
    expect(computeOpenCount(20, 5)).toBe(15);
    expect(computeOpenCount(3, 3)).toBe(0);
    expect(computeOpenCount(2, 5)).toBe(0);
  });

  it('builds status share percents from honest counts', () => {
    const shares = buildStatusShares({
      New: 5,
      InProgress: 5,
      Resolved: 5,
      Closed: 5,
    });
    expect(shares.map((s) => s.percent)).toEqual([25, 25, 25, 25]);
    expect(shares[0].status).toBe('New');
  });
});
