---
paths:
  - "**/NoAzul.Application/BankConnections/**"
  - "**/NoAzul.Application/Interfaces/Pluggy/**"
  - "**/NoAzul.Infra.Data/Pluggy/**"
  - "**/NoAzul.Api/Controllers/BankConnectionsController.cs"
  - "**/NoAzul.Workers/**/BankConnections/**"
---

# Decisões — Bancos e Pluggy

O NoAzul lê cartões e contas pela Pluggy, na modalidade gratuita de uso pessoal (Meu Pluggy). Não movimenta dinheiro: só `GET` de dados, mais o `connect_token` que abre a janela de conexão.

## Como o banco entra: janela oficial da Pluggy, não Item ID colado

Decidido pelo dono em 2026-11-15. O botão "Conectar banco" pede um connect token à API (`POST /api/bank-connections/connect-token`), abre o Pluggy Connect (`pluggy-connect-sdk`) e, no `onSuccess`, manda o `item.id` para `POST /api/bank-connections`. Alternativa descartada: a pessoa copiar o Item ID no meu.pluggy.ai e colar num campo (mais simples, mas manual e técnico demais para o pai do dono).

Por que o NoAzul guarda o item: no plano de uso pessoal a Pluggy **não deixa listar items** (`GET /items` desligado). Item que não está na tabela `BankConnections` não existe para o NoAzul.

**Cada pessoa usa o próprio Meu Pluggy** (decidido em 2026-11-15). A Pluggy diz que juntar titulares diferentes numa conta Meu Pluggy está em revisão. O `client_id`/`client_secret` é um só, da aplicação do dono no dashboard da Pluggy; o connect token leva `clientUserId` = `UserId` do NoAzul.

## A tela lê do banco do NoAzul, nunca da Pluggy

A Pluggy limita leituras por mês (Open Finance: contas cerca de 4/mês, faturas cerca de 30/mês). Por isso:

- `GET /api/bank-connections` só lê `BankConnections` + `FinancialAccounts`.
- Quem fala com a Pluggy é a sincronização: uma vez por dia (`bank-connections:sync`, 08:00 de São Paulo, depois da atualização noturna do Meu Pluggy) e uma vez logo depois de conectar (job enfileirado).
- Não existe botão "atualizar agora". Se entrar, precisa de limite próprio por conexão, não só rate limit por usuário.

## O que é guardado

| Tabela | Uma linha por | Chave da Pluggy |
|---|---|---|
| `BankConnections` | conexão (item), dona = `UserId` | `PluggyItemId`, única filtrada |
| `FinancialAccounts` | conta ou cartão do item | `PluggyAccountId`, única filtrada |

- Subtipos guardados: `CHECKING_ACCOUNT`, `SAVINGS_ACCOUNT`, `CREDIT_CARD`. Outros são ignorados.
- Cartão: `Balance` é a fatura aberta; limite, fechamento e vencimento vêm de `creditData`; `PreviousBillAmount` é a fatura de `GET /bills` com vencimento mais recente até hoje.
- Conta que some da Pluggy é `SoftDelete`; se voltar, a linha revive.
- `FinancialAccount` está em `AuditPolicy.Ignored`: é cópia de dado do provedor reescrita todo dia, não ação de alguém.
- Status da conexão a partir do `item.status`: `UPDATED` → `Ok`, `UPDATING` → `Updating`, `LOGIN_ERROR`/`WAITING_USER_INPUT` → `NeedsReconnect`, o resto → `Failed`. Item que a Pluggy não acha mais → `NeedsReconnect`.

## Conectar de novo o mesmo banco não é erro

O connect token pede `avoidDuplicates`, então a Pluggy pode devolver um item que já está guardado. Se ele é da mesma pessoa, a resposta é `BANK_CONNECTION_REFRESHED` e a sincronização é enfileirada. Se é de outra pessoa, `BANK_CONNECTION_TAKEN` (409).

## Credencial ausente degrada, não derruba

Sem `Pluggy:ClientId`/`Pluggy:ClientSecret`, o boot loga `PLUGGY_NOT_CONFIGURED`, os endpoints que falam com a Pluggy devolvem `PLUGGY_NOT_CONFIGURED`, e a sincronização diária loga o aviso e não faz nada. A API key da Pluggy (vale 2h) fica no `HybridCache` por 100 min.

## Codes

| Code | Quando |
|---|---|
| `BANK_CONNECTIONS_LOADED` | lista |
| `BANK_CONNECT_TOKEN_CREATED` | token criado |
| `BANK_CONNECTION_CREATED` | conexão nova guardada |
| `BANK_CONNECTION_REFRESHED` | item já era desta pessoa |
| `BANK_CONNECTION_ITEM_ID_INVALID` (400) | `itemId` vazio ou com mais de 64 caracteres |
| `BANK_CONNECTION_ITEM_NOT_FOUND` (400) | a Pluggy não conhece o item |
| `BANK_CONNECTION_TAKEN` (409) | item de outra pessoa |
| `PLUGGY_NOT_CONFIGURED` (500) | credencial ausente |
| `PLUGGY_UNAVAILABLE` (500) | a Pluggy falhou ou não respondeu |

## Eventos de log

| Evento | Nível | Quando |
|---|---|---|
| `PLUGGY_NOT_CONFIGURED` | Warning | boot ou sincronização diária sem credencial |
| `PLUGGY_CONNECT_TOKEN_FAILED` | Error | falha ao criar connect token |
| `PLUGGY_ITEM_LOOKUP_FAILED` | Error | falha ao ler o item no cadastro |
| `BANK_CONNECTION_CREATED` | Info | conexão nova |
| `BANK_CONNECTION_SYNC_NOT_ENQUEUED` | Warning | conexão salva, primeira sincronização não enfileirada |
| `BANK_CONNECTION_ITEM_GONE` | Warning | a Pluggy não acha mais o item |
| `BANK_CONNECTION_SYNC_FAILED` | Error | uma conexão falhou na sincronização diária (as outras seguem) |
| `BANK_CONNECTIONS_SYNCED` | Info | fim da sincronização diária, com total e sucesso |

Nada da Pluggy entra no log além de ids e nome da instituição: nem API key, nem connect token, nem payload.

## Roteiro de verificação

Sem credencial:

1. Subir a API sem `Pluggy:*` → linha `PLUGGY_NOT_CONFIGURED` no boot.
2. `GET /api/bank-connections` logado → 200 `BANK_CONNECTIONS_LOADED`, `items: []`.
3. `POST /api/bank-connections/connect-token` → 500 `PLUGGY_NOT_CONFIGURED`.
4. `POST /api/bank-connections` com `{"itemId":""}` → 400 `BANK_CONNECTION_ITEM_ID_INVALID`.
5. Sem token → 401.

Com credencial (no `appsettings.Development.json`):

6. Tela Cartões e contas → "Conectar banco" abre a janela da Pluggy. Em desenvolvimento ela mostra também os bancos de teste (sandbox, ex.: "Pluggy Bank", usuário `user-ok`, senha `password-ok`).
7. Concluir → aviso `BANK_CONNECTION_CREATED`, o banco aparece "Buscando os dados no banco" e, em alguns segundos, com cartões e contas. Log `BANK_CONNECTION_CREATED`; no `/hangfire`, o job "Bancos: sincronizar conexão #N" com sucesso.
8. Conectar o mesmo banco de novo → `BANK_CONNECTION_REFRESHED`, sem linha nova.
9. `/hangfire` → recorrente `bank-connections:sync` → "Trigger now" → `BANK_CONNECTIONS_SYNCED` com `Synced` igual ao total.

## O nome do banco sai do código do banco, não do conector

Pelo conector MeuPluggy, `item.connector.name` é sempre "MeuPluggy" e o `imageUrl` é o logo do Meu Pluggy (visto na primeira conexão real, Itaú, em 2026-11-15). Nenhum campo do item traz o banco. O que traz é a conta corrente: `bankData.transferNumber` começa pelo código COMPE (`341/agência/conta`). `BankCodes` traduz o código dos bancos mais comuns, e a sincronização grava o código em `BankConnection.BankCode` e troca o nome da conexão pelo nome do banco. O `bankCode` vai no DTO porque o frontend escolhe a cor do banco por ele, não pelo nome.

- Cartão não tem `transferNumber`. Conexão só com cartão, ou com banco fora da tabela, fica com o nome do conector até alguém acrescentar o código em `BankCodes`.
- Descartado: usar o `name` da conta. Ele vem inconsistente no mesmo banco ("itau", "ITAU VISA PLATINUM").
- O logo do MeuPluggy não é guardado (`InstitutionImageUrl` fica nulo), para não aparecer como se fosse o do banco.

## Em aberto

- Reconectar (Pluggy Connect com `updateItem`) e desconectar ainda não existem; os botões ficam desligados na tela.
- ~~Lançamentos ainda não são sincronizados.~~ Sincronizados desde 2026-11-15, ver `lancamentos.md`.
