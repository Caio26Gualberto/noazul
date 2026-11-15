import { CATEGORIES, Category, Origin, Transaction } from './finance.models';

export function roundMoney(value: number): number {
  return Math.round(value * 100) / 100;
}

export function sumAmounts(transactions: readonly Transaction[]): number {
  return roundMoney(
    transactions.reduce(
      (total, transaction) => (transaction.ignored ? total : total + transaction.amount),
      0,
    ),
  );
}

export function totalsByOrigin(transactions: readonly Transaction[]): Record<Origin, number> {
  return {
    card: sumAmounts(transactions.filter((transaction) => transaction.origin === 'card')),
    account: sumAmounts(transactions.filter((transaction) => transaction.origin === 'account')),
  };
}

export function totalsByCategory(
  transactions: readonly Transaction[],
): Partial<Record<Category, number>> {
  const totals: Partial<Record<Category, number>> = Object.fromEntries(
    CATEGORIES.map((category) => [category, 0]),
  );
  for (const transaction of transactions) {
    if (transaction.ignored) continue;
    totals[transaction.category] = roundMoney(
      (totals[transaction.category] ?? 0) + transaction.amount,
    );
  }
  return totals;
}
