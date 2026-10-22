import { DurationType, TimeOffRequest } from './models';

export const HOURS_PER_FULL_DAY = 8;

export function toDateKey(value: string): string {
  return value.slice(0, 10);
}

export function inclusiveCalendarDays(startDate: string, endDate: string): number {
  const start = Date.parse(`${toDateKey(startDate)}T00:00:00Z`);
  const end = Date.parse(`${toDateKey(endDate)}T00:00:00Z`);
  return Math.round((end - start) / 86_400_000) + 1;
}

export function calculateFullDayHours(startDate: string, endDate: string): number {
  return inclusiveCalendarDays(startDate, endDate) * HOURS_PER_FULL_DAY;
}

export function datesOverlap(startA: string, endA: string, startB: string, endB: string): boolean {
  const aStart = toDateKey(startA);
  const aEnd = toDateKey(endA);
  const bStart = toDateKey(startB);
  const bEnd = toDateKey(endB);
  return aStart <= bEnd && aEnd >= bStart;
}

export function resolveRequestedHours(
  durationType: DurationType,
  startDate: string,
  endDate: string,
  partialHours: number | null
): number {
  if (durationType === 'FullDay') {
    return calculateFullDayHours(startDate, endDate);
  }
  return partialHours ?? 0;
}

export function requestableBalance(earned: number, taken: number, pendingReserved: number): number {
  return earned - taken - pendingReserved;
}

export function findOverlap(
  startDate: string,
  endDate: string,
  existing: TimeOffRequest[],
  ignoreId?: string
): TimeOffRequest | undefined {
  return existing.find((request) =>
    request.id !== ignoreId &&
    (request.status === 'Pending' || request.status === 'Approved') &&
    datesOverlap(request.startDate, request.endDate, startDate, endDate)
  );
}

export function validateRequest(input: {
  startDate: string;
  endDate: string;
  durationType: DurationType;
  requestedHours: number | null;
  requestableBalance: number;
  existing: TimeOffRequest[];
}): string | null {
  if (!input.startDate || !input.endDate) {
    return 'Start date and end date are required.';
  }
  if (input.endDate < input.startDate) {
    return 'End date cannot be earlier than start date.';
  }
  if (input.durationType === 'PartialDay') {
    if (input.startDate !== input.endDate) {
      return 'Partial-day requests are limited to a single date.';
    }
    if (input.requestedHours === null || Number.isNaN(input.requestedHours) || input.requestedHours <= 0) {
      return 'Partial-day requests require a positive number of hours.';
    }
    if (input.requestedHours > HOURS_PER_FULL_DAY) {
      return `Partial-day requests cannot exceed ${HOURS_PER_FULL_DAY} hours.`;
    }
  }

  const hours = resolveRequestedHours(input.durationType, input.startDate, input.endDate, input.requestedHours);
  if (hours <= 0) {
    return 'Requested hours must be greater than zero.';
  }
  if (hours > input.requestableBalance) {
    return `Requested hours exceed the remaining requestable balance (${formatHours(input.requestableBalance)}), which reserves pending requests.`;
  }
  if (findOverlap(input.startDate, input.endDate, input.existing)) {
    return 'This request overlaps an existing pending or approved request.';
  }
  return null;
}

export function formatHours(value: number): string {
  return Number.isInteger(value) ? value.toString() : value.toFixed(2).replace(/0$/, '').replace(/\.0$/, '');
}

export function formatDate(value: string): string {
  const key = toDateKey(value);
  const [year, month, day] = key.split('-');
  if (!year || !month || !day) {
    return value;
  }
  return `${month}/${day}/${year}`;
}
