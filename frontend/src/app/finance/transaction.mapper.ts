import { MonthSummaryDto, TransactionDto } from '../api/types';
import { toProductDay } from './finance.dates';
import {
  CATEGORIES,
  Category,
  MonthSummary,
  Transaction,
  isBuiltInCategory,
  isCustomCategoryKey,
} from './finance.models';

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
    ignored: dto.isIgnored,
    installment:
      dto.installmentNumber && dto.installmentTotal
        ? { current: dto.installmentNumber, total: dto.installmentTotal }
        : null,
    note: null,
  };
}

export function toMonthSummary(dto: MonthSummaryDto): MonthSummary {
  const byCategory: Partial<Record<Category, number>> = Object.fromEntries(
    CATEGORIES.map((category) => [category, 0]),
  );
  for (const [key, amount] of Object.entries(dto.byCategory)) {
    const category = toCategory(key);
    byCategory[category] = (byCategory[category] ?? 0) + amount;
  }
  return { month: dto.month, total: dto.total, byCategory };
}

export function toCategory(value: string): Category {
  return isBuiltInCategory(value) || isCustomCategoryKey(value) ? value : 'other';
}
