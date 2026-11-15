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
