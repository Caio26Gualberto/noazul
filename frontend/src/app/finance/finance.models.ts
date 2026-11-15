import type { TuiPaymentSystem } from '@taiga-ui/addon-commerce';

export type Origin = 'card' | 'account';

export const CATEGORIES = [
  'groceries',
  'restaurants',
  'transport',
  'health',
  'subscriptions',
  'home',
  'shopping',
  'leisure',
  'education',
  'fees',
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
  ignored: boolean;
  installment: Installment | null;
  note: string | null;
}

export const CARD_BRANDS = ['mastercard', 'visa', 'elo', 'amex', 'hipercard', 'other'] as const;

export type CardBrand = (typeof CARD_BRANDS)[number];

export interface Card {
  id: number;
  brand: CardBrand;
  paymentSystem: TuiPaymentSystem | null;
  last4: string;
  limit: number;
  openBill: number;
  previousBill: number;
  futureInstallments: number;
  closesOn: string | null;
  dueOn: string | null;
}

export type AccountKind = 'checking' | 'savings';

export interface Account {
  id: number;
  kind: AccountKind;
  number: string;
  balance: number;
  balanceAt: string;
}

export type ConnectionStatus = 'ok' | 'updating' | 'expired';

export interface BankConnection {
  id: number;
  bank: string;
  bankKey: string | null;
  imageUrl: string | null;
  status: ConnectionStatus;
  lastSyncAt: string | null;
  expiredAt: string | null;
  cards: Card[];
  accounts: Account[];
}

export interface MonthSummary {
  month: string;
  total: number;
  byCategory: Record<Category, number>;
}
