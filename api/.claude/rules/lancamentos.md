---
paths:
  - "**/NoAzul.Application/Transactions/**"
  - "**/NoAzul.Infra.Data/Repositories/Transactions/**"
  - "**/NoAzul.Api/Controllers/TransactionsController.cs"
  - "**/NoAzul.Domain/Entities/Transaction.cs"
  - "**/NoAzul.Domain/Enums/Transactions/**"
  - "**/NoAzul.Application/CustomCategories/**"
  - "**/NoAzul.Api/Controllers/CustomCategoriesController.cs"
  - "**/NoAzul.Domain/Entities/CustomCategory.cs"
---

# Decisões — Lançamentos

Um lançamento é um gasto: uma saída do cartão ou da conta, sempre vinda da Pluggy, na mesma sincronização dos cartões e contas (`bancos-pluggy.md`). Não existe lançamento manual: o "Lançar Pix" foi removido pelo dono em 2026-11-15, porque os Pix das contas conectadas já chegam pela Pluggy e lançar à mão contaria duas vezes.

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

## Regra da pessoa vence a tabela (decidido pelo dono em 2026-11-15)

`PUT /api/transactions/{id}/category` com `{ category: "restaurants" }` ou `{ ignored: true }` grava uma `CategoryRule` (pessoa + descrição **exata**, única filtrada) e aplica a todos os lançamentos da pessoa com a mesma descrição, num `ExecuteUpdateAsync` do `ITransactionRepository`. A sincronização carrega as regras da pessoa e as aplica antes de comparar, então o que chega depois já entra com a escolha dela, e a releitura dos 30 dias não desfaz nada.

- "Idêntica" é igualdade de texto, sem normalizar caixa nem espaço: foi o que o dono pediu, e a Pluggy repete a descrição do mesmo estabelecimento.
- `IsIgnored` ("não é gasto") fica fora do `SumByCategoryAsync` (resumo mensal) e dos totais do frontend, mas a lista continua devolvendo o lançamento, com `isIgnored`.
- Marcar "não é gasto" guarda a categoria que o lançamento tinha; escolher uma categoria depois desfaz o "não é gasto".
- Não há "voltar à categoria automática": escolher a mesma categoria da tabela grava regra igual. Apagar a regra é só pelo banco.
- `ExecuteUpdateAsync` não passa pela auditoria; `Transaction` já é ignorada nela.

## Categorias da pessoa (decidido pelo dono em 2026-11-15)

Além das onze, cada pessoa cria as próprias categorias (`CustomCategory`: pessoa + nome) e elas funcionam como qualquer outra: entram no menu e no filtro de Lançamentos, nos totais do Início e na comparação das Métricas. Ficam presas à descrição para sempre, pela mesma `CategoryRule` das onze.

- **A chave é texto.** A API fala de categoria por uma chave única: o nome do enum em camelCase (`restaurants`) ou `custom-<id>` (`CategoryKeys`). Vale no `category` do lançamento, no filtro `?category=`, no `PUT /api/transactions/{id}/category` e nas chaves do `byCategory` do resumo. O resumo traz as onze e todas as categorias da pessoa, zeradas quando não há gasto.
- **No banco**, `Transaction.CustomCategoryId` e `CategoryRule.CustomCategoryId` (FK `Restrict`). Com ele preenchido, a categoria é a da pessoa e o `Category` (enum) é ignorado. Escolher uma das onze zera o `CustomCategoryId`.
- Na sincronização, regra com categoria da pessoa grava o `CustomCategoryId` e deixa o `Category` com o da tabela da Pluggy.
- **Criar** é pelo menu do lançamento ("Nova categoria"): cria e já aplica nele e nos idênticos. **Renomear e apagar** ficam em "Minhas categorias", no mesmo menu.
- **Apagar** (`DELETE /api/custom-categories/{id}`, numa transação do `IUnitOfWork`): `SoftDelete` das regras que apontam para ela e dos que tinham a categoria, `CustomCategoryId` zerado e `Category` recalculado pela tabela da Pluggy (`ProviderCategoryId`). Ou seja, os lançamentos voltam para a categoria automática, e os que chegarem depois também. O recálculo é linha a linha, porque a tabela é código e não SQL.
- Nome: de 1 a 40 caracteres, espaços repetidos viram um, único por pessoa sem diferenciar maiúscula. Não é comparado com o nome das onze (o nome delas só existe no i18n do frontend). Limite de 50 por pessoa.
- Rotas em `/api/custom-categories` (lista paginada, criar, renomear, apagar), com rate limit `custom-category` (60/h) nas escritas.

## Nome escolhido pela pessoa (decidido pelo dono em 2026-11-15)

A descrição do banco continua guardada e é a chave de tudo (regra de categoria, apelido). Por cima dela, a pessoa pode dar outro nome ao lançamento, e ele vale para todos com a mesma descrição, os de agora e os que chegarem depois, como a categoria. O nome aparece em Lançamentos, nos últimos lançamentos do Início e nos maiores gastos das Métricas; em Lançamentos, a descrição do banco fica embaixo, menor.

- `DescriptionAlias` (pessoa + descrição **exata**, única filtrada, nome até 60). Não é gravado no `Transaction`: o `GetMine` carrega os apelidos da pessoa e preenche `displayName` no DTO (`null` sem apelido).
- `PUT /api/transactions/{id}/display-name {"name":"..."}`: nome vazio ou igual à descrição volta ao nome do banco (`SoftDelete` do apelido). Espaços repetidos viram um. Usa o rate limit `transaction-category`.
- A busca (`?search=`) procura na descrição e no apelido.
- Descartado pelo dono: usar o nome do estabelecimento da Pluggy (`merchant`). Nos 89 gastos de 30 dias do dono, só 16 traziam `merchant`, quase sempre a razão social ("APPLE COMPUTER BRASIL LTDA"), e nenhum dos que confundem (Keeta, iFood de restaurante pequeno, posto). O `descriptionRaw` era igual à descrição.

## Sincronização

`TransactionAppService.SyncFromProvider(conta, userId)`, chamado por `BankConnectionAppService` para cada conta, depois de atualizar as contas.

- Primeira vez da conta (nenhum lançamento dela, nem apagado): 12 meses. Depois: os últimos 30 dias.
- O que a Pluggy devolve é procurado também pelo id, fora da janela: um pendente que vira definitivo pode mudar de data para antes da janela, e sem isso a sincronização tentava inserir de novo e quebrava no índice único (`IX_Transactions_PluggyTransactionId`, visto em 2026-11-15). O "sumiu da Pluggy" continua valendo só dentro da janela.
- A janela é relida inteira: o que é novo entra, o que mudou é atualizado, e o que estava gravado na janela e a Pluggy não devolveu mais é `SoftDelete`. É assim que pendente que vira definitivo com outro id não duplica, e o que voltou revive.
- `GET /v2/transactions` (o `/transactions` antigo responde 410). A paginação é por cursor: o campo `next` já vem como query string (`?accountId=…&after=…`) e é usado como veio; montar o `after` à mão dá `INVALID_CURSOR`. Teto de 200 páginas por conta.
- Valor sempre positivo (`Math.Abs`); a data é o instante da Pluggy, gravado em UTC (`OccurredAt`).
- Parcela só é gravada quando `totalInstallments > 1`.
- `Transaction` está em `AuditPolicy.Ignored`: é cópia do provedor, reescrita todo dia.

## Leitura

- `GET /api/transactions?month=yyyy-MM&origin=&category=&search=&connectionId=&page=&pageSize=` — do próprio usuário, mês na zona do produto (`AppTime.MonthWindowUtc`), mais recente primeiro com desempate por `Id`. Sem `month`, o mês atual. Origem aceita o nome do enum (`card`); categoria aceita a chave (`restaurants` ou `custom-12`); `connectionId` filtra pelo banco (ver "Filtro global por banco"). O frontend pede todas as páginas do mês e filtra na tela.
- `GET /api/transactions/monthly-summary?months=6&connectionId=` — de 1 a 24 meses terminando no atual, total e soma por categoria (onze mais as da pessoa), um `GroupBy` por mês no `ITransactionRepository`. Mês sem dados vem zerado. `connectionId` filtra pelo banco.
- `origin` inválido cai na validação padrão do ASP.NET (400 em ProblemDetails, fora do envelope). `category` inválida é `TRANSACTION_CATEGORY_INVALID`, no envelope.

## Filtro global por banco (decidido pelo dono em 2026-11-15)

Quem tem mais de um banco conectado quer ver uma conta de cada vez (o dono usa o crédito no Itaú e a conta corrente na Inter). O seletor global do site, como um "tenant", escolhe **um banco ou todas as contas**, e a escolha vale em Lançamentos, Início e Métricas. Cartões e contas sempre mostra todos os bancos (é onde se gerencia a conexão).

- O filtro é por **conexão** (`BankConnection`), não por conta individual: um banco com mais de uma conta (corrente + cartão) entra inteiro.
- Na API é o `connectionId` opcional de `GET /api/transactions` e de `GET /api/transactions/monthly-summary`. Null = todas. Ele entra no `WHERE` por `t.FinancialAccount.BankConnectionId == connectionId` (lista e `SumByCategoryAsync`).
- Não valida dono: o filtro já corre junto de `UserId == userId`, então `connectionId` de outra pessoa (ou inexistente) devolve vazio, não vaza nada. Por isso não há code próprio.
- As faturas abertas e a "última atualização" do Início saem das conexões que o frontend já tem, filtradas na tela pelo banco escolhido; não há parâmetro para elas.

## Codes

| Code | Quando |
|---|---|
| `TRANSACTIONS_LOADED` | lista |
| `TRANSACTION_MONTH_INVALID` (400) | `month` fora de `yyyy-MM` |
| `TRANSACTION_SEARCH_TOO_LONG` (400) | busca com mais de 100 caracteres |
| `TRANSACTION_SUMMARY_LOADED` | resumo |
| `TRANSACTION_SUMMARY_MONTHS_INVALID` (400) | `months` fora de 1–24 |
| `TRANSACTION_CATEGORY_UPDATED` | categoria trocada (e nos idênticos) |
| `TRANSACTION_MARKED_NOT_SPENDING` | marcado como não é gasto (e os idênticos) |
| `TRANSACTION_CATEGORY_INVALID` (400) | categoria vazia, desconhecida, ou da pessoa que não existe ou é de outra pessoa (no `PUT` sem `ignored` e no filtro) |
| `TRANSACTION_NOT_FOUND` (404) | id que não existe ou é de outra pessoa |
| `TRANSACTION_RENAMED` | nome trocado (vale para os idênticos) |
| `TRANSACTION_NAME_RESET` | voltou ao nome do banco |
| `TRANSACTION_NAME_INVALID` (400) | nome com mais de 60 caracteres |
| `CUSTOM_CATEGORIES_LOADED` | lista das categorias da pessoa |
| `CUSTOM_CATEGORY_CREATED` | categoria criada |
| `CUSTOM_CATEGORY_RENAMED` | categoria renomeada |
| `CUSTOM_CATEGORY_DELETED` | categoria apagada, lançamentos de volta à automática |
| `CUSTOM_CATEGORY_NAME_INVALID` (400) | nome vazio ou com mais de 40 caracteres |
| `CUSTOM_CATEGORY_NAME_TAKEN` (409) | a pessoa já tem uma com esse nome |
| `CUSTOM_CATEGORY_LIMIT_REACHED` (400) | já tem 50 |
| `CUSTOM_CATEGORY_NOT_FOUND` (404) | renomear ou apagar uma que não existe ou é de outra pessoa |

## Eventos de log

| Evento | Nível | Quando |
|---|---|---|
| `TRANSACTIONS_SYNCED` | Info | uma conta ganhou ou perdeu lançamentos na sincronização (com `Received`, `Added`, `Removed`, `FirstSync`); conta sem mudança não loga |
| `TRANSACTION_CATEGORY_RULE_SAVED` | Info | regra gravada, com quantos lançamentos mudaram |
| `CUSTOM_CATEGORY_CREATED` | Info | categoria da pessoa criada |
| `CUSTOM_CATEGORY_DELETED` | Info | categoria da pessoa apagada, com quantos lançamentos voltaram à automática (`Restored`) |

Falha da Pluggy sobe e vira `BANK_CONNECTION_SYNC_FAILED` da conexão inteira.

## Roteiro de verificação

1. `GET /api/transactions` logado sem banco → 200, `items: []`. `?month=2026-13` → 400 `TRANSACTION_MONTH_INVALID`. `monthly-summary?months=99` → 400.
2. Com banco conectado, `/hangfire` → `bank-connections:sync` → "Trigger now" → uma linha `TRANSACTIONS_SYNCED` por conta, `FirstSync=true` na primeira vez.
3. Rodar de novo → contas sem mudança não logam; nenhuma linha duplicada (`PluggyTransactionId` único).
4. Lançamentos, Início e Métricas mostram o mês atual com os dados do banco.
5. Em Lançamentos, trocar a categoria de um lançamento que tem idênticos → aviso `TRANSACTION_CATEGORY_UPDATED`, todos os idênticos mudam (conferir no banco por `Description`), linha em `CategoryRules`. "Não é gasto" → linha apagada, total do filtro desconta. Rodar a sincronização → a escolha continua.
6. Categorias da pessoa: `POST /api/custom-categories {"name":"  Pet  "}` → `CUSTOM_CATEGORY_CREATED` com nome `Pet` e chave `custom-N`; de novo com `pet` → 409 `CUSTOM_CATEGORY_NAME_TAKEN`; nome vazio → 400. `PUT /api/transactions/{id}/category {"category":"custom-N"}` → todos os idênticos com `CustomCategoryId = N`; `monthly-summary` traz `custom-N` com a soma; `?category=custom-N` filtra. `DELETE /api/custom-categories/N` → os lançamentos voltam para a categoria da tabela, a regra fica `IsDeleted`; apagar de novo → 404. (Visto em 2026-11-15 com dois lançamentos de teste.)
7. Na tela: menu da categoria → "Nova categoria" → nome → o lançamento e os idênticos mudam, a categoria aparece no filtro, no Início e nas Métricas. "Minhas categorias" → renomear muda o nome em todo lugar; apagar pede confirmação.
8. Nome: `PUT /api/transactions/{id}/display-name {"name":"  Keeta   Elisa "}` → `TRANSACTION_RENAMED`, `displayName` `Keeta Elisa` nos idênticos; `?search=elisa` acha; 61 caracteres → 400; `{"name":""}` → `TRANSACTION_NAME_RESET` e a busca não acha mais. Na tela: clicar na descrição → "Renomear lançamento" → o nome muda em Lançamentos, Início e Métricas; "Voltar ao nome do banco" desfaz. (Visto em 2026-11-15 com dados de teste.)
9. Filtro por banco, com dois bancos (ex.: Itaú conexão 2, soma 300; Inter conexão 3, soma 50): `monthly-summary?months=1` sem filtro → total 350; `&connectionId=2` → 300; `&connectionId=3` → 50; `&connectionId=999` → 0. `GET /api/transactions?month=atual` dá os mesmos cortes no `totalCount` (3/2/1/0). Na tela, o seletor global troca os números de Início, Lançamentos e Métricas juntos; Cartões e contas não muda. (Visto em 2026-11-15 com dois bancos de teste semeados.)

## Em aberto

- ~~A descrição é a que a Pluggy manda, crua do banco. Limpar ou usar o nome do estabelecimento é decisão do dono.~~ Decidido em 2026-11-15: fica a descrição do banco, com o nome escolhido pela pessoa por cima. Limpeza automática (tirar cidade e país colados no fim) ficou de fora.
