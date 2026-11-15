import { MonthSummaryDto, TransactionDto } from '../api/types';
import { toProductDay } from './finance.dates';
import { CATEGORIES, Category, MonthSummary, Transaction } from './finance.models';

export function toTransaction(dto: TransactionDto): Transaction {
  return {
    id: dto.id,
    date: toProductDay(dto.occurredAt),
    description: dto.description,
    category: toCategory(dto.category),
    origin: dto.origin,
    bank: dto.bank,
    amount: dto.amount,
    pending: dto.isPending,
    installment:
      dto.installmentNumber && dto.installmentTotal
        ? { current: dto.installmentNumber, total: dto.installmentTotal }
        : null,
    note: null,
  };
}

export function toMonthSummary(dto: MonthSummaryDto): MonthSummary {
  const byCategory = Object.fromEntries(
    CATEGORIES.map((category) => [category, dto.byCategory[category] ?? 0]),
  ) as Record<Category, number>;
  return { month: dto.month, total: dto.total, byCategory };
}

function toCategory(value: string): Category {
  return (CATEGORIES as readonly string[]).includes(value) ? (value as Category) : 'other';
}
