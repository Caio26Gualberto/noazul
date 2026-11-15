import type { TuiPaymentSystem } from '@taiga-ui/addon-commerce';
import { BankConnectionDto, FinancialAccountDto } from '../api/types';
import {
  Account,
  BankConnection,
  CARD_BRANDS,
  Card,
  CardBrand,
  ConnectionStatus,
} from './finance.models';

const PAYMENT_SYSTEMS: Partial<Record<CardBrand, TuiPaymentSystem>> = {
  mastercard: 'mastercard',
  visa: 'visa',
  amex: 'amex',
};

const BANK_KEYS: Record<string, string> = {
  '001': 'banco-do-brasil',
  '033': 'santander',
  '041': 'banrisul',
  '070': 'brb',
  '077': 'inter',
  '104': 'caixa',
  '208': 'btg-pactual',
  '212': 'banco-original',
  '237': 'bradesco',
  '260': 'nubank',
  '290': 'pagbank',
  '323': 'mercado-pago',
  '336': 'c6-bank',
  '341': 'itau',
  '380': 'picpay',
  '422': 'safra',
  '655': 'votorantim',
  '735': 'neon',
  '748': 'sicredi',
  '756': 'sicoob',
};

export function toBankConnection(dto: BankConnectionDto): BankConnection {
  const status = toStatus(dto);
  return {
    id: dto.id,
    bank: dto.institutionName,
    bankKey: dto.bankCode ? (BANK_KEYS[dto.bankCode] ?? null) : null,
    imageUrl: dto.institutionImageUrl,
    status,
    lastSyncAt: dto.lastSyncedAt,
    expiredAt: status === 'expired' ? (dto.providerUpdatedAt ?? dto.lastSyncedAt) : null,
    cards: dto.accounts.filter((account) => account.type === 'creditCard').map(toCard),
    accounts: dto.accounts.filter((account) => account.type !== 'creditCard').map(toAccount),
  };
}

function toStatus(dto: BankConnectionDto): ConnectionStatus {
  if (dto.status === 'needsReconnect' || dto.status === 'failed') return 'expired';
  if (dto.status === 'updating' || dto.lastSyncedAt === null) return 'updating';
  return 'ok';
}

function toCard(dto: FinancialAccountDto): Card {
  const brand = toBrand(dto.cardBrand);
  return {
    id: dto.id,
    brand,
    paymentSystem: PAYMENT_SYSTEMS[brand] ?? null,
    last4: dto.number.slice(-4),
    limit: dto.creditLimit ?? 0,
    openBill: dto.balance,
    previousBill: dto.previousBillAmount ?? 0,
    futureInstallments: 0,
    closesOn: dto.billClosesOn,
    dueOn: dto.billDueOn,
  };
}

function toAccount(dto: FinancialAccountDto): Account {
  return {
    id: dto.id,
    kind: dto.type === 'savings' ? 'savings' : 'checking',
    number: dto.number,
    balance: dto.balance,
    balanceAt: dto.balanceUpdatedAt,
  };
}

function toBrand(value: string | null): CardBrand {
  const normalized = (value ?? '').toLowerCase();
  return CARD_BRANDS.find((brand) => brand !== 'other' && normalized.includes(brand)) ?? 'other';
}

export function bankClass(connection: BankConnection): string {
  return connection.bankKey ? `app-bank--${connection.bankKey}` : '';
}
