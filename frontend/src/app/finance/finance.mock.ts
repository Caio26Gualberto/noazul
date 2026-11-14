import { BankConnection, MonthSummary, Transaction } from './finance.models';

export const CURRENT_MONTH = '2026-11';
export const TODAY = '2026-11-14';

type Row = [
  date: string,
  description: string,
  category: Transaction['category'],
  origin: Transaction['origin'],
  bank: string | null,
  amount: number,
  pending?: boolean,
  installment?: [current: number, total: number],
];

const rows: Row[] = [
  ['2026-11-01', 'Seguro do carro', 'transport', 'account', 'Nubank', 390.4],
  ['2026-11-02', 'Atacadão Central', 'groceries', 'card', 'Itaú', 515.9],
  ['2026-11-03', 'Supermercado Bom Preço', 'groceries', 'card', 'Nubank', 412.8],
  ['2026-11-04', 'Academia Movimento', 'health', 'account', 'Nubank', 99.9],
  ['2026-11-04', 'Restaurante Executivo', 'restaurants', 'card', 'Nubank', 392],
  ['2026-11-05', 'Plano de celular', 'subscriptions', 'account', 'Nubank', 59.6],
  ['2026-11-05', 'Livraria Página', 'other', 'card', 'Itaú', 89.9],
  ['2026-11-06', 'Hamburgueria do Bairro', 'restaurants', 'card', 'Nubank', 78.4],
  ['2026-11-06', 'Armazenamento em nuvem', 'subscriptions', 'card', 'Itaú', 99.1],
  ['2026-11-07', 'Pix para Ana (presente)', 'other', 'pix', null, 202],
  ['2026-11-07', 'Pizzaria Forno a Lenha', 'restaurants', 'card', 'Itaú', 118],
  ['2026-11-08', 'Internet fibra', 'home', 'account', 'Nubank', 119.9],
  ['2026-11-08', 'Pet shop Amigo', 'other', 'card', 'Nubank', 153.9],
  ['2026-11-09', 'Loja Casa Bela', 'home', 'card', 'Itaú', 149.9, false, [3, 10]],
  ['2026-11-09', 'Casa & Construção', 'home', 'card', 'Nubank', 241.7],
  ['2026-11-10', 'Padaria Central', 'groceries', 'pix', null, 38],
  ['2026-11-10', 'Streaming de vídeo', 'subscriptions', 'card', 'Nubank', 55.9],
  ['2026-11-10', 'Conta de água', 'home', 'account', 'Nubank', 98.7],
  ['2026-11-11', 'Farmácia Saúde Mais', 'health', 'card', 'Itaú', 64.9],
  ['2026-11-11', 'Consulta no dentista', 'health', 'card', 'Nubank', 223.4],
  ['2026-11-11', 'Gás de cozinha', 'home', 'account', 'Nubank', 45],
  ['2026-11-12', 'Conta de luz', 'home', 'account', 'Nubank', 214.8],
  ['2026-11-12', 'Posto Avenida', 'transport', 'card', 'Nubank', 220],
  ['2026-11-12', 'Delivery japonês', 'restaurants', 'card', 'Nubank', 142],
  ['2026-11-13', 'Pix para Marcos (diarista)', 'home', 'pix', null, 180],
  ['2026-11-13', 'Restaurante Sabor Caseiro', 'restaurants', 'card', 'Itaú', 96.5],
  ['2026-11-13', 'Hortifruti Verde', 'groceries', 'card', 'Itaú', 86.45],
  ['2026-11-14', 'Supermercado Bom Preço', 'groceries', 'card', 'Nubank', 187.35, true],
  ['2026-11-14', 'Cafeteria Grão', 'restaurants', 'card', 'Nubank', 36, true],
];

export const INITIAL_TRANSACTIONS: Transaction[] = rows.map(
  ([date, description, category, origin, bank, amount, pending, installment], index) => ({
    id: index + 1,
    date,
    description,
    category,
    origin,
    bank,
    amount,
    pending: pending ?? false,
    installment: installment ? { current: installment[0], total: installment[1] } : null,
    note: null,
  }),
);

export const BANK_CONNECTIONS: BankConnection[] = [
  {
    id: 1,
    bank: 'Nubank',
    status: 'ok',
    lastSyncAt: '2026-11-14T06:12:00-03:00',
    expiredAt: null,
    cards: [
      {
        id: 1,
        brand: 'mastercard',
        last4: '4821',
        limit: 6000,
        openBill: 2184.3,
        previousBill: 2906.15,
        futureInstallments: 0,
        closesOn: '2026-12-03',
        dueOn: '2026-12-10',
      },
    ],
    accounts: [{ id: 1, branch: '0001', balance: 3912.55, balanceAt: '2026-11-14' }],
  },
  {
    id: 2,
    bank: 'Itaú',
    status: 'ok',
    lastSyncAt: '2026-11-14T06:15:00-03:00',
    expiredAt: null,
    cards: [
      {
        id: 2,
        brand: 'visa',
        last4: '1307',
        limit: 4500,
        openBill: 1179.8,
        previousBill: 1342.6,
        futureInstallments: 1049.3,
        closesOn: '2026-12-08',
        dueOn: '2026-12-15',
      },
    ],
    accounts: [],
  },
  {
    id: 3,
    bank: 'Banco Inter',
    status: 'expired',
    lastSyncAt: '2026-11-10T06:20:00-03:00',
    expiredAt: '2026-11-10',
    cards: [],
    accounts: [],
  },
];

export const MONTHLY_HISTORY: MonthSummary[] = [
  {
    month: '2026-06',
    total: 5920,
    byCategory: {
      groceries: 1610,
      home: 1480,
      restaurants: 1050,
      transport: 790,
      health: 420,
      subscriptions: 214.6,
      other: 355.4,
    },
  },
  {
    month: '2026-07',
    total: 6340,
    byCategory: {
      groceries: 1720,
      home: 1495,
      restaurants: 1240,
      transport: 860,
      health: 395,
      subscriptions: 214.6,
      other: 415.4,
    },
  },
  {
    month: '2026-08',
    total: 5410,
    byCategory: {
      groceries: 1530,
      home: 1460,
      restaurants: 880,
      transport: 700,
      health: 310,
      subscriptions: 214.6,
      other: 315.4,
    },
  },
  {
    month: '2026-09',
    total: 6780,
    byCategory: {
      groceries: 1750,
      home: 1530,
      restaurants: 1320,
      transport: 980,
      health: 640,
      subscriptions: 214.6,
      other: 345.4,
    },
  },
  {
    month: '2026-10',
    total: 6105,
    byCategory: {
      groceries: 1680,
      home: 1520,
      restaurants: 1190,
      transport: 820,
      health: 410,
      subscriptions: 214.6,
      other: 270.4,
    },
  },
];
