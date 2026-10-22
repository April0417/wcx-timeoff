import { calculateFullDayHours, datesOverlap, requestableBalance, validateRequest } from './time-off-rules';

describe('time-off-rules', () => {
  it('uses inclusive calendar days for full-day hours', () => {
    expect(calculateFullDayHours('2026-10-21', '2026-10-23')).toBe(24);
  });

  it('treats inclusive date ranges as overlapping on the boundary', () => {
    expect(datesOverlap('2026-10-21', '2026-10-22', '2026-10-22', '2026-10-24')).toBeTrue();
  });

  it('reserves pending hours from the requestable balance', () => {
    expect(requestableBalance(40, 8, 16)).toBe(16);
  });

  it('rejects partial-day ranges and insufficient balance', () => {
    expect(validateRequest({
      startDate: '2026-10-21',
      endDate: '2026-10-22',
      durationType: 'PartialDay',
      requestedHours: 4,
      requestableBalance: 40,
      existing: []
    })).toContain('single date');

    expect(validateRequest({
      startDate: '2026-10-21',
      endDate: '2026-10-21',
      durationType: 'FullDay',
      requestedHours: null,
      requestableBalance: 4,
      existing: []
    })).toContain('requestable balance');
  });
});
