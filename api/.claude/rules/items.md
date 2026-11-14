---
paths:
  - "**/NoAzul.Application/Items/**"
  - "**/NoAzul.Domain/Entities/Item.cs"
  - "**/NoAzul.Domain/Enums/Items/**"
  - "**/NoAzul.Infra.Data/Repositories/Items/**"
  - "**/Controllers/App/ItemsController.cs"
  - "**/Controllers/Admin/ItemsController.cs"
  - "**/NoAzul.Workers/**/Items/**"
---

# Decisões — Items (a feature de exemplo)

`Item` existe para ser copiado. Não tem negócio: é um registro do cliente com nome, descrição, status (`Draft → Active → Archived`) e vencimento opcional. Cada decisão abaixo está aqui porque é o padrão que uma feature real vai precisar — quando a primeira feature real nascer, copie a estrutura e apague `Items` inteira.

## O usuário é dono do que é dele, e o admin vê tudo

Os dois controllers (`Controllers/ItemsController` e `Controllers/Admin/ItemsController`) apontam para o **mesmo** `IItemAppService`, com métodos separados por escopo: `GetMine`/`GetMineById`/`ArchiveMine` filtram por `UserId == _currentUser.UserId` **dentro do `WHERE`**; `GetAll`/`GetById`/`Archive`/`Delete` não filtram e exigem permissão `Items.*`. Não existe um `if (item.CustomerId != _currentUser.AccountId)` depois de carregar a linha: carregar a linha de outro para depois recusar é `NotFound` que vaza existência e um round-trip a mais.

`Delete` (hard? não — `SoftDelete`) só existe no admin. O usuário **arquiva**; excluir é operação de equipe, e `Items.Delete` é permissão separada de `Items.Edit` de propósito (ver granularidade no `CLAUDE.md`).

## Transição de estado é `UPDATE ... WHERE`, não `if` depois de `SELECT`

`Activate` e `Archive` são `IItemRepository.TryActivateAsync`/`TryArchiveAsync`: um `ExecuteUpdateAsync` com o estado esperado no `WHERE`, e "zero linhas" é a recusa (`ITEM_NOT_ACTIVATABLE`/`ITEM_NOT_ARCHIVABLE`, `Conflict`). Dois toques simultâneos em "ativar" produzem um sucesso e um `409` — nunca dois sucessos e nunca um 500.

`Update` (nome/descrição/vencimento) continua sendo carregar-e-salvar: não há transição de estado e o último a escrever vence, que é o esperado para edição de campos.

## Nome único por usuário, e o índice é a rede, não a validação

`(UserId, Name)` é único com filtro de soft delete. O AppService checa antes (`NameTakenAsync`) e devolve `ITEM_NAME_TAKEN` (`Conflict`); o índice existe para a corrida entre a checagem e o insert — quando ele pega, a resposta é 500 `UNEXPECTED_ERROR` com `caused-by=` na linha de log, e isso é aceitável porque é raro e visível. O que não é aceitável é depender do índice como caminho normal: a mensagem do banco não é contrato com o cliente.

## Vencimento é varrido por job, não checado na leitura

`ExpiresAt` vencido vira `Archived` pelo recorrente `items:archive-expired` (`Cron.Hourly`, fila `items`, `IItemRepository.ArchiveExpiredAsync` — um `UPDATE` de conjunto). A alternativa, filtrar `ExpiresAt > now` em toda leitura, foi descartada: espalha a regra por cada consulta e deixa o status da linha mentindo. O job devolve `int` (tipo cru — nenhum endpoint o chama) e loga `ITEMS_EXPIRED` só quando arquivou alguma coisa.

## O pós-criação é job enfileirado, e falhar em enfileirar não desfaz a criação

`Create` grava o item e depois chama `IEnqueuedJobDispatcher.EnqueueItemCreated(id)`. Se o enfileiramento estourar (storage do Hangfire fora), o item **já existe** e a resposta é sucesso; o `catch` loga `ITEM_CREATED_JOB_NOT_ENQUEUED` (Warning) com o id. Desfazer a criação por causa de um efeito colateral secundário seria trocar um aviso por um erro na cara do usuário. O trigger tem retry (`3 · 30, 120, 600`) porque não tem linha de status própria.

## `ItemsSnapshot` é o exemplo de job com linha de status

`POST /api/admin/jobs/items-snapshot` (`System.Jobs.Manage`) cria um `BackgroundJobRun`, enfileira e devolve o id. O handler (`ItemsSnapshotJobHandler`) conta itens por status e grava o JSON em `ResultJson`. Serve para mostrar o ciclo completo: dedup por tipo (`JOB_ALREADY_RUNNING` enquanto houver um em voo), claim atômico, `Attempts = 0`, retry pela tela. Ver `jobs-hangfire.md`.

## Rate limit no create

`item-create`: 20/h por usuário. Criar é a única escrita que gera linha nova sem limite natural, e é o exemplo de "endpoint que pode ser abusado tem política". Os outros verbos operam sobre linha existente e ficam no limite global.

## Eventos de log

| Evento | Nível | Quando |
|---|---|---|
| `ITEM_ARCHIVED` | Info | arquivamento pelo dono ou pelo admin, com `ByOwner` |
| `ITEMS_EXPIRED` | Info | varredura arquivou pelo menos um |
| `ITEM_CREATED_JOB_NOT_ENQUEUED` | Warning | item criado, pós-criação não enfileirado |
| `ITEM_CREATED_PROCESSED` | Info | trigger do pós-criação rodou |

## Roteiro de verificação

1. `POST /api/auth/register` `{name, email, password}` → `REGISTRATION_SUCCESS` já com `data.tokens.token`.
2. `POST /api/items` `{name}` → 200 `ITEM_CREATED`, status `0`; repetir o mesmo nome → 409 `ITEM_NAME_TAKEN`.
3. `POST /api/items/{id}/activate` → `ITEM_ACTIVATED`; repetir → 409 `ITEM_NOT_ACTIVATABLE`.
4. `GET /api/items?page=1&pageSize=5` → `ITEMS_LOADED` com `totalCount`.
5. Token de usuário comum em `GET /api/admin/items` → 403. Login admin (`admin@noazul.local`) → `GET /api/admin/items` → lista com `customerName`.
6. 21 `POST /api/items` na mesma hora → o 21º volta 429 `TOO_MANY_REQUESTS` em camelCase e uma linha `WARN RATE_LIMIT_REJECTED`.
7. `POST /api/admin/jobs/items-snapshot` → `JOB_TRIGGERED`; repetir → 409 `JOB_ALREADY_RUNNING`; `GET /api/admin/jobs/runs/{id}` → `status: 2` e `resultJson` com as contagens.
8. `GET /api/admin/jobs/recurring` → `items:archive-expired` na fila `items`.
