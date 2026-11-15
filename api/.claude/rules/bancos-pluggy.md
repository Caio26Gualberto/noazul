---
paths:
  - "**/NoAzul.Application/BankConnections/**"
  - "**/NoAzul.Application/Interfaces/Pluggy/**"
  - "**/NoAzul.Infra.Data/Pluggy/**"
  - "**/NoAzul.Api/Controllers/BankConnectionsController.cs"
  - "**/NoAzul.Workers/**/BankConnections/**"
---

# Decisões — Bancos e Pluggy

O NoAzul lê cartões e contas pela Pluggy, na modalidade gratuita de uso pessoal (Meu Pluggy). Não movimenta dinheiro: só `GET` de dados, mais o `connect_token` que abre a janela de conexão e o `DELETE /items/{id}` de quando a pessoa desconecta um banco.

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

## Antes de conectar: o guia do Meu Pluggy (decidido pelo dono em 2026-11-15)

Quem nunca usou a Pluggy não sabe que precisa conectar o banco no Meu Pluggy antes. Por isso a tela Cartões e contas sem banco mostra um passo a passo (criar conta em meu.pluggy.ai, conectar os bancos lá, voltar e escolher "Meu Pluggy" na janela), com um link para o Meu Pluggy. E "Conectar banco" abre antes uma janela com os mesmos passos e "Já fiz isso, continuar"; só depois vem a janela da Pluggy. Reconectar não passa pela janela. Os passos seguem o README oficial (`github.com/pluggyai/meu-pluggy`): a autorização é uma vez por banco conectado no Meu Pluggy.

## Conectar de novo o mesmo banco não é erro

O connect token pede `avoidDuplicates`, então a Pluggy pode devolver um item que já está guardado. Se ele é da mesma pessoa, a resposta é `BANK_CONNECTION_REFRESHED` e a sincronização é enfileirada. Se é de outra pessoa, `BANK_CONNECTION_TAKEN` (409). Nos dois casos de "mesma pessoa" a conexão passa para `Updating` até a sincronização terminar.

## Reconectar e desconectar (decidido pelo dono em 2026-11-15)

Os dois ficam no menu "Gerenciar" de cada banco em Cartões e contas; o aviso de conexão expirada tem os dois botões também.

**Reconectar**: `POST /api/bank-connections/{id}/reconnect-token` cria um connect token com o `itemId` da conexão e devolve o token e o `itemId`. O frontend abre o Pluggy Connect com `updateItem`, a pessoa refaz o login no banco e o `onSuccess` manda o item para `POST /api/bank-connections`, que cai no caminho "mesma pessoa" acima: `BANK_CONNECTION_REFRESHED` e sincronização enfileirada. Não existe endpoint de reconexão separado porque o registro já faz o que precisa.

**Desconectar apaga tudo**, e a tela avisa isso na confirmação. `DELETE /api/bank-connections/{id}`:

1. Apaga o item na Pluggy (`DELETE /items/{id}`). Se a Pluggy falha, nada muda no NoAzul e a resposta é `PLUGGY_UNAVAILABLE`; 404 da Pluggy (item já apagado) segue em frente.
2. Numa transação do `IUnitOfWork`: `SoftDelete` dos lançamentos de todas as contas da conexão (`ITransactionAppService.RemoveForAccounts`, um `ExecuteUpdateAsync` que grava `DeletedAt`/`DeletedBy` à mão porque não passa pelo `ApplyAuditing`), das contas e da conexão.

Os totais do Início e das Métricas deixam de contar esse banco. As regras de categoria da pessoa (`CategoryRules`) ficam, porque valem por descrição e não por banco. Conectar o mesmo banco de novo gera um item novo na Pluggy, então contas e lançamentos entram como novos, com 12 meses de histórico.

Alternativas que o dono descartou: guardar o histórico e só parar de sincronizar; apagar só no NoAzul e manter o item no Meu Pluggy.

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
| `BANK_CONNECTION_NOT_FOUND` (404) | reconectar ou desconectar uma conexão que não existe ou é de outra pessoa |
| `BANK_CONNECTION_DISCONNECTED` | conexão desconectada e dados apagados |
| `PLUGGY_NOT_CONFIGURED` (500) | credencial ausente |
| `PLUGGY_UNAVAILABLE` (500) | a Pluggy falhou ou não respondeu |

## Eventos de log

| Evento | Nível | Quando |
|---|---|---|
| `PLUGGY_NOT_CONFIGURED` | Warning | boot ou sincronização diária sem credencial |
| `PLUGGY_CONNECT_TOKEN_FAILED` | Error | falha ao criar connect token |
| `PLUGGY_ITEM_LOOKUP_FAILED` | Error | falha ao ler o item no cadastro |
| `PLUGGY_ITEM_DELETE_FAILED` | Error | falha ao apagar o item na Pluggy ao desconectar (a conexão continua) |
| `BANK_CONNECTION_DISCONNECTED` | Info | conexão desconectada, com quantas contas e lançamentos saíram e se o item foi apagado na Pluggy |
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
10. `DELETE /api/bank-connections/999999` e `POST /api/bank-connections/999999/reconnect-token` → 404 `BANK_CONNECTION_NOT_FOUND`; sem token → 401.
11. "Gerenciar" → "Reconectar" → a janela da Pluggy abre já no banco; concluir → aviso `BANK_CONNECTION_REFRESHED`, o banco mostra "Buscando os dados no banco" e volta atualizado.
12. "Gerenciar" → "Desconectar" → a confirmação avisa que tudo será apagado; "Cancelar" não muda nada. Confirmar → aviso `BANK_CONNECTION_DISCONNECTED`, o banco some da tela, Início/Lançamentos/Métricas perdem os gastos dele, log `BANK_CONNECTION_DISCONNECTED` com `DeletedAtPluggy=True`. No banco: `IsDeleted = true` na conexão, nas contas e nos lançamentos.

## O nome do banco sai do código do banco, não do conector

Pelo conector MeuPluggy, `item.connector.name` é sempre "MeuPluggy" e o `imageUrl` é o logo do Meu Pluggy (visto na primeira conexão real, Itaú, em 2026-11-15). Nenhum campo do item traz o banco. O que traz é a conta corrente: `bankData.transferNumber` começa pelo código COMPE (`341/agência/conta`). `BankCodes` traduz o código dos bancos mais comuns, e a sincronização grava o código em `BankConnection.BankCode` e troca o nome da conexão pelo nome do banco. O `bankCode` vai no DTO porque o frontend escolhe a cor do banco por ele, não pelo nome.

- Cartão não tem `transferNumber`. Conexão só com cartão, ou com banco fora da tabela, fica com o nome do conector até alguém acrescentar o código em `BankCodes`.
- Descartado: usar o `name` da conta. Ele vem inconsistente no mesmo banco ("itau", "ITAU VISA PLATINUM").
- O logo do MeuPluggy não é guardado (`InstitutionImageUrl` fica nulo), para não aparecer como se fosse o do banco.

## Em aberto

- ~~Reconectar (Pluggy Connect com `updateItem`) e desconectar ainda não existem; os botões ficam desligados na tela.~~ Feitos em 2026-11-15, ver "Reconectar e desconectar".
- Reconectar um item que a Pluggy já apagou (`BANK_CONNECTION_ITEM_GONE`) falha na criação do token (`PLUGGY_UNAVAILABLE`). O caminho é desconectar e conectar de novo.
- ~~Lançamentos ainda não são sincronizados.~~ Sincronizados desde 2026-11-15, ver `lancamentos.md`.
