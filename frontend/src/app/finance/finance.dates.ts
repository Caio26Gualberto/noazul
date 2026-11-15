const PRODUCT_TIME_ZONE = 'America/Sao_Paulo';

const isoDayFormat = new Intl.DateTimeFormat('en-CA', { timeZone: PRODUCT_TIME_ZONE });

export function toProductDay(instant: Date | string): string {
  return isoDayFormat.format(typeof instant === 'string' ? new Date(instant) : instant);
}

export function today(): string {
  return toProductDay(new Date());
}

export function currentMonth(): string {
  return today().slice(0, 7);
}

export function recentMonths(count: number): string[] {
  const [year, month] = currentMonth().split('-').map(Number);
  return Array.from({ length: count }, (_, offset) => {
    const date = new Date(Date.UTC(year, month - 1 - offset, 1));
    return date.toISOString().slice(0, 7);
  });
}
