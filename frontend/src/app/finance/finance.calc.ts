import { CATEGORIES, Category, Origin, Transaction } from './finance.models';

export function roundMoney(value: number): number {
  return Math.round(value * 100) / 100;
}

export function sumAmounts(transactions: readonly Transaction[]): number {
  return roundMoney(transactions.reduce((total, transaction) => total + transaction.amount, 0));
}

export function totalsByOrigin(transactions: readonly Transaction[]): Record<Origin, number> {
  return {
    card: sumAmounts(transactions.filter((transaction) => transaction.origin === 'card')),
    account: sumAmounts(transactions.filter((transaction) => transaction.origin === 'account')),
    pix: sumAmounts(transactions.filter((transaction) => transaction.origin === 'pix')),
  };
}

export function totalsByCategory(transactions: readonly Transaction[]): Record<Category, number> {
  const totals = Object.fromEntries(CATEGORIES.map((category) => [category, 0])) as Record<
    Category,
    number
  >;
  for (const transaction of transactions) {
    totals[transaction.category] = roundMoney(totals[transaction.category] + transaction.amount);
  }
  return totals;
}
