import { calculateFullDayHours, datesOverlap, requestableBalance, validateRequest } from './time-off-rules';
import { TimeOffRequest } from './models';

describe('time-off rules', () => {
  it('calculates full-day hours as inclusive calendar days times 8', () => {
    expect(calculateFullDayHours('2026-10-21', '2026-10-23')).toBe(24);
    expect(calculateFullDayHours('2026-10-21', '2026-10-21')).toBe(8);
  });

  it('treats date ranges as inclusive', () => {
    expect(datesOverlap('2026-10-21', '2026-10-22', '2026-10-22', '2026-10-24')).toBeTrue();
    expect(datesOverlap('2026-10-21', '2026-10-22', '2026-10-23', '2026-10-24')).toBeFalse();
  });

  it('reserves pending hours when calculating a requestable balance', () => {
    expect(requestableBalance(40, 8, 16)).toBe(16);
  });

  it('rejects a partial-day request that is not a single date', () => {
    const message = validateRequest({
      startDate: '2026-10-21',
      endDate: '2026-10-22',
      durationType: 'PartialDay',
      requestedHours: 4,
      requestableBalance: 40,
      existing: []
    });
    expect(message).toContain('single date');
  });

  it('rejects hours above the requestable balance and ignores denied overlaps', () => {
    const denied: TimeOffRequest = {
      id: 'denied',
      agentId: 'a',
      agentName: 'Jordan',
      startDate: '2026-10-21',
      endDate: '2026-10-21',
      durationType: 'FullDay',
      requestedHours: 8,
      reason: null,
      status: 'Denied',
      createdAt: '2026-10-01',
      updatedAt: '2026-10-01',
      reviewedBy: null,
      reviewedAt: null
    };

    expect(validateRequest({
      startDate: '2026-10-21',
      endDate: '2026-10-21',
      durationType: 'FullDay',
      requestedHours: null,
      requestableBalance: 8,
      existing: [denied]
    })).toBeNull();

    expect(validateRequest({
      startDate: '2026-10-21',
      endDate: '2026-10-22',
      durationType: 'FullDay',
      requestedHours: null,
      requestableBalance: 8,
      existing: []
    })).toContain('requestable balance');
  });
});
