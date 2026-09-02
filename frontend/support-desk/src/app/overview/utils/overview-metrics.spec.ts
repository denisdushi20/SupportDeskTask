import { computeOpenCount } from './overview-metrics';

describe('overview metrics', () => {
  it('computes Open as Total minus Closed', () => {
    expect(computeOpenCount(20, 5)).toBe(15);
    expect(computeOpenCount(3, 3)).toBe(0);
    expect(computeOpenCount(2, 5)).toBe(0);
  });
});
