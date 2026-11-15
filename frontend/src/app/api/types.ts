export interface ApiEnvelope<T> {
  isSuccess: boolean;
  code: string;
  message: string;
  data: T | null;
}

export interface TokensDto {
  token: string;
  refreshToken: string;
}

export interface LoginResponseDto {
  tokens: TokensDto;
  isNeededChangePassword: boolean;
}

export interface ProfileDto {
  id: number;
  name: string;
  email: string;
  role: string | null;
  isNeededChangePassword: boolean;
  createdAt: string;
}

export interface RegistrationStatusDto {
  enabled: boolean;
}

export interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export type BankConnectionStatusDto = 'ok' | 'updating' | 'needsReconnect' | 'failed';

export type FinancialAccountTypeDto = 'checking' | 'savings' | 'creditCard';

export interface FinancialAccountDto {
  id: number;
  type: FinancialAccountTypeDto;
  name: string;
  number: string;
  balance: number;
  currencyCode: string;
  balanceUpdatedAt: string;
  cardBrand: string | null;
  creditLimit: number | null;
  availableCreditLimit: number | null;
  billClosesOn: string | null;
  billDueOn: string | null;
  previousBillAmount: number | null;
}

export interface BankConnectionDto {
  id: number;
  institutionName: string;
  bankCode: string | null;
  institutionImageUrl: string | null;
  status: BankConnectionStatusDto;
  providerUpdatedAt: string | null;
  lastSyncedAt: string | null;
  createdAt: string;
  accounts: FinancialAccountDto[];
}

export interface ConnectTokenDto {
  accessToken: string;
  itemId: string | null;
}

export type TransactionOriginDto = 'card' | 'account';

export interface TransactionDto {
  id: number;
  occurredAt: string;
  description: string;
  displayName: string | null;
  category: string;
  origin: TransactionOriginDto;
  bank: string | null;
  amount: number;
  isPending: boolean;
  isIgnored: boolean;
  installmentNumber: number | null;
  installmentTotal: number | null;
}

export interface CategoryRuleResultDto {
  description: string;
  category: string;
  isIgnored: boolean;
  updatedCount: number;
}

export interface MonthSummaryDto {
  month: string;
  total: number;
  byCategory: Record<string, number>;
}

export interface DisplayNameResultDto {
  description: string;
  displayName: string | null;
  affectedCount: number;
}

export interface CustomCategoryDto {
  id: number;
  key: string;
  name: string;
}
