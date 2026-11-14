export type Origin = 'card' | 'account' | 'pix';

export const CATEGORIES = [
  'groceries',
  'home',
  'restaurants',
  'transport',
  'health',
  'subscriptions',
  'other',
] as const;

export type Category = (typeof CATEGORIES)[number];

export interface Installment {
  current: number;
  total: number;
}

export interface Transaction {
  id: number;
  date: string;
  description: string;
  category: Category;
  origin: Origin;
  bank: string | null;
  amount: number;
  pending: boolean;
  installment: Installment | null;
  note: string | null;
}

export interface NewPix {
  date: string;
  description: string;
  category: Category;
  amount: number;
  note: string | null;
}

export type CardBrand = 'mastercard' | 'visa';

export interface Card {
  id: number;
  brand: CardBrand;
  last4: string;
  limit: number;
  openBill: number;
  previousBill: number;
  futureInstallments: number;
  closesOn: string;
  dueOn: string;
}

export interface Account {
  id: number;
  branch: string;
  balance: number;
  balanceAt: string;
}

export type ConnectionStatus = 'ok' | 'expired';

export interface BankConnection {
  id: number;
  bank: string;
  status: ConnectionStatus;
  lastSyncAt: string;
  expiredAt: string | null;
  cards: Card[];
  accounts: Account[];
}

export interface MonthSummary {
  month: string;
  total: number;
  byCategory: Record<Category, number>;
}
