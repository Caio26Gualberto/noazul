---
paths:
  - "**/NoAzul.Application/Transactions/**"
  - "**/NoAzul.Infra.Data/Repositories/Transactions/**"
  - "**/NoAzul.Api/Controllers/TransactionsController.cs"
  - "**/NoAzul.Domain/Entities/Transaction.cs"
  - "**/NoAzul.Domain/Enums/Transactions/**"
---

# Decisões — Lançamentos

Um lançamento é um gasto: uma saída do cartão, da conta ou um Pix lançado à mão. Os de cartão e conta vêm da Pluggy na mesma sincronização dos cartões e contas (`bancos-pluggy.md`); o Pix manual ainda não tem endpoint.

## O que é gasto (decidido pelo dono em 2026-11-15)

Só saída (`type = DEBIT`). Fica fora, e nem é gravado:

| Fora | Como se reconhece |
|---|---|
| Entradas: pagamento da fatura no cartão, estorno, Pix recebido, rendimento | `type = CREDIT` |
| Pagamento de fatura feito pela conta | categoria Pluggy `0510*` |
| Transferência entre contas da mesma pessoa | `04*` |
| Aplicação e resgate de investimento | `03*` |
| Renda | `01*` |

Estorno não desconta. O que a Pluggy não reconhece como conta da mesma pessoa (Pix para a própria conta em outro banco, por exemplo) **entra como gasto** em Outros; só a pessoa sabe. Visto na primeira importação do dono: os Pix enviados pela conta não batem com os pagamentos do cartão (nenhum com mesmo valor em até 3 dias), então não há contagem dupla com a fatura do Itaú.

## Onze categorias, por uma tabela fixa

`PluggyCategoryMap` traduz o `categoryId` da Pluggy (8 ou 9 dígitos, hierárquico) pelo **prefixo**, regras específicas antes das gerais:

| NoAzul | Pluggy |
|---|---|
| Mercado (`groceries`) | `10` |
| Restaurantes (`restaurants`) | `11` |
| Transporte (`transport`) | `19`, seguro de veículo `2004` |
| Saúde (`health`) | `18`, academia `0703`, seguro saúde `2003` |
| Assinaturas (`subscriptions`) | `09`, telecom `0701` |
| Casa (`home`) | `17`, seguro residencial `2002` |
| Compras (`shopping`) | `08` |
| Lazer e viagem (`leisure`) | `12`, `14`, `21`, ingressos `0704` |
| Educação (`education`) | `0702` |
| Tarifas e impostos (`fees`) | `02`, `15`, `16` |
| Outros (`other`) | o resto: `05` (Pix, TED, boleto), `07` serviços, `13` doações, sem categoria |

O `ProviderCategoryId` fica guardado em cada lançamento, então mudar a tabela não exige buscar de novo na Pluggy: basta recalcular `Category` a partir dele. A sincronização já faz isso nos últimos 30 dias, porque compara a categoria calculada com a gravada.

## Sincronização

`TransactionAppService.SyncFromProvider(conta, userId)`, chamado por `BankConnectionAppService` para cada conta, depois de atualizar as contas.

- Primeira vez da conta (nenhum lançamento dela, nem apagado): 12 meses. Depois: os últimos 30 dias.
- A janela é relida inteira: o que é novo entra, o que mudou é atualizado, e o que estava gravado na janela e a Pluggy não devolveu mais é `SoftDelete`. É assim que pendente que vira definitivo com outro id não duplica, e o que voltou revive.
- `GET /v2/transactions` (o `/transactions` antigo responde 410). A paginação é por cursor: o campo `next` já vem como query string (`?accountId=…&after=…`) e é usado como veio; montar o `after` à mão dá `INVALID_CURSOR`. Teto de 200 páginas por conta.
- Valor sempre positivo (`Math.Abs`); a data é o instante da Pluggy, gravado em UTC (`OccurredAt`).
- Parcela só é gravada quando `totalInstallments > 1`.
- `Transaction` está em `AuditPolicy.Ignored`: é cópia do provedor, reescrita todo dia.

## Leitura

- `GET /api/transactions?month=yyyy-MM&origin=&category=&search=&page=&pageSize=` — do próprio usuário, mês na zona do produto (`AppTime.MonthWindowUtc`), mais recente primeiro com desempate por `Id`. Sem `month`, o mês atual. Origem e categoria aceitam o nome do enum (`card`, `restaurants`). O frontend pede todas as páginas do mês e filtra na tela.
- `GET /api/transactions/monthly-summary?months=6` — de 1 a 24 meses terminando no atual, total e soma por categoria, um `GroupBy` por mês no `ITransactionRepository`. Mês sem dados vem zerado.
- Valor inválido de `origin`/`category` cai na validação padrão do ASP.NET (400 em ProblemDetails, fora do envelope), igual ao `status` de `Items`.

## Codes

| Code | Quando |
|---|---|
| `TRANSACTIONS_LOADED` | lista |
| `TRANSACTION_MONTH_INVALID` (400) | `month` fora de `yyyy-MM` |
| `TRANSACTION_SEARCH_TOO_LONG` (400) | busca com mais de 100 caracteres |
| `TRANSACTION_SUMMARY_LOADED` | resumo |
| `TRANSACTION_SUMMARY_MONTHS_INVALID` (400) | `months` fora de 1–24 |

## Eventos de log

| Evento | Nível | Quando |
|---|---|---|
| `TRANSACTIONS_SYNCED` | Info | uma conta ganhou ou perdeu lançamentos na sincronização (com `Received`, `Added`, `Removed`, `FirstSync`); conta sem mudança não loga |

Falha da Pluggy sobe e vira `BANK_CONNECTION_SYNC_FAILED` da conexão inteira.

## Roteiro de verificação

1. `GET /api/transactions` logado sem banco → 200, `items: []`. `?month=2026-13` → 400 `TRANSACTION_MONTH_INVALID`. `monthly-summary?months=99` → 400.
2. Com banco conectado, `/hangfire` → `bank-connections:sync` → "Trigger now" → uma linha `TRANSACTIONS_SYNCED` por conta, `FirstSync=true` na primeira vez.
3. Rodar de novo → contas sem mudança não logam; nenhuma linha duplicada (`PluggyTransactionId` único).
4. Lançamentos, Início e Métricas mostram o mês atual com os dados do banco.

## Em aberto

- A descrição é a que a Pluggy manda, crua do banco ("KEETABR*60.472.405 ELISAB…"). Limpar ou usar o nome do estabelecimento (`merchant`) é decisão do dono.
- Não dá para trocar a categoria de um lançamento nem marcá-lo como "não é gasto".
- Pix manual (`Origin = Pix`) ainda é só na memória do frontend.
