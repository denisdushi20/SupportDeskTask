import { departmentLabel, formatOverdueRelative, transitionActionLabel } from './display';

describe('display helpers', () => {
  describe('transitionActionLabel', () => {
    it('maps authorized transitions to operator actions', () => {
      expect(transitionActionLabel('New', 'InProgress')).toBe('Start work');
      expect(transitionActionLabel('InProgress', 'Resolved')).toBe('Resolve');
      expect(transitionActionLabel('Resolved', 'Closed')).toBe('Close');
      expect(transitionActionLabel('Resolved', 'InProgress')).toBe('Reopen');
    });
  });

  describe('formatOverdueRelative', () => {
    it('formats overdue duration from dueDate', () => {
      const now = new Date('2026-09-03T12:00:00.000Z');
      expect(formatOverdueRelative('2026-09-03T10:00:00.000Z', now)).toBe('2h overdue');
      expect(formatOverdueRelative('2026-09-03T11:45:00.000Z', now)).toBe('15m overdue');
    });

    it('returns null when not overdue', () => {
      const now = new Date('2026-09-03T12:00:00.000Z');
      expect(formatOverdueRelative('2026-09-03T13:00:00.000Z', now)).toBeNull();
    });
  });

  describe('departmentLabel', () => {
    it('labels departments', () => {
      expect(departmentLabel('Technical')).toBe('Technical');
    });
  });
});
