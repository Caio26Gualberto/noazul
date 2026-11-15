# CLAUDE.md — Frontend (Angular)

## Sobre este projeto

**NoAzul** é um sistema de organização financeira pessoal: mostra os gastos de cartão e de conta vindos da Pluggy e calcula métricas do mês. É usado por duas pessoas (o dono e o pai dele), cada uma com a própria conta. Este frontend é o site que elas usam.

O que ele **não** é: não movimenta dinheiro, não é produto comercial, não cuida de investimentos e não é em tempo real.

Fase atual: autenticação pronta (login, cadastro com trava, definir senha, trocar senha, perfil). Início, Lançamentos, Cartões e contas e Métricas usam a API de verdade (dados da Pluggy). Veja "Domínio finance".

O dono está aprendendo Angular neste projeto: prefira o caminho padrão e documentado do framework ao atalho esperto.

## Stack

- Angular 22: componentes standalone, signals, controle de fluxo no template (`@if`, `@for`), rotas com carregamento sob demanda.
- Taiga UI 5 (`@taiga-ui/core`, `kit`, `layout`, `icons`, `i18n`), com o tema próprio do NoAzul por cima.
- Formulários reativos tipados (`NonNullableFormBuilder`).
- Transloco para os textos.
- `HttpClient` com interceptor funcional.
- ESLint (`angular-eslint`) e Prettier.
- Fontes auto-hospedadas: Inter (texto) e Space Grotesk (títulos), via `@fontsource-variable`.

## Comandos

| Comando | O que faz |
|---|---|
| `npm start` | sobe em `http://localhost:4200`, apontando para a API em `http://localhost:5041` |
| `npm run lint` | ESLint em `.ts` e `.html` |
| `npm run typecheck` | checagem de tipos |
| `npm run build` | build de produção em `dist/` |
| `npm run format` | Prettier em `src/` |

O CI roda `lint`, `typecheck` e `build`. Não há projeto de teste: a verificação é rodando de verdade, com a API local.

## Estrutura de pastas

```
src/
  environments/      URL da API por ambiente (o de desenvolvimento substitui o padrão no ng serve)
  styles.scss        só importa o tema
  app/
    api/             única porta de saída HTTP
    auth/            sessão, guards, permissões, rotas
    i18n/pt-BR/      textos, um arquivo por assunto
    theme/           único lugar de cor
    layout/          casca logada (barra e menu) e nav-config
    components/      componentes globais, prefixo App
    pages/           uma pasta por tela
```

## Camada de API (`app/api/`)

Toda chamada HTTP passa por aqui. Página e componente nunca injetam `HttpClient`.

- `api-client.ts` (`ApiClient`): `get`, `post`, `put`, `patch` e `delete`. Abre o envelope `{ isSuccess, code, message, data }`, devolve só o `data` e transforma qualquer falha em `ApiError(code, status)`. Mostra o erro na tela uma vez, traduzido pelo `code`; `silent: true` desliga isso, `announce: true` mostra também a mensagem de sucesso.
- `auth.interceptor.ts`: põe o `Authorization: Bearer` nas chamadas para a API. Num 401 **sem corpo** (token vencido, recusado antes de chegar ao controller), renova o token e repete a chamada. Um 401 **com envelope** (`INVALID_CREDENTIALS`, por exemplo) é resposta de negócio e segue adiante.
- `token-refresher.ts` (`TokenRefresher`): faz a renovação uma vez só, mesmo com várias chamadas falhando juntas, usando um `HttpClient` sem interceptor. Se a renovação é recusada, limpa os tokens e avisa por `sessionExpired$`.
- `token-storage.ts`: os dois tokens ficam no `localStorage` (`access_token`, `refresh_token`).
- `api-codes.ts` + `i18n/pt-BR/api.json`: espelho dos codes da API. Code novo na API entra nos dois no mesmo commit.
- `types.ts`: tipos dos DTOs, escritos à mão.
- `feedback.service.ts`: o aviso na tela (notificação do Taiga, `TuiNotificationService`). É o único lugar que abre aviso de API.

Serviço de feature novo: uma classe em `app/<feature>/` ou `app/pages/<feature>/` que injeta `ApiClient` e devolve `Observable<T>`, como `auth/auth.service.ts`.

## Domínio finance (`app/finance/`)

Gastos, cartões, contas e métricas, todos vindos da API. Não existe lançamento manual (decisão do dono, 2026-11-15).

- `finance.models.ts`: os tipos (`Transaction`, `BankConnection`, `Card`, `Account`, `MonthSummary`) e as onze categorias (`CATEGORIES`, ids em inglês, iguais ao enum da API em camelCase; o nome na tela vem de `finance.categories.<id>`).
- `finance.service.ts` (`FinanceService`): tudo pela API via `ApiClient`. `transactions(month)` pede **todas** as páginas do mês (`expand` até `hasNext` falso, 100 por página); `history()` pede `monthly-summary?months=6` e devolve só os meses fechados; `connections()`, `createConnectToken()`, `createReconnectToken(id)`, `registerConnection(itemId)` e `disconnect(id)` são de Cartões e contas.
- Reconectar e desconectar: o "Gerenciar" de cada banco é um `tuiDropdown` (signal `menuFor`, como em Lançamentos). Reconectar abre o Pluggy Connect com `updateItem`; desconectar pede confirmação pelo `TUI_CONFIRM` do Taiga (`TuiDialogService`), com o aviso de que cartões, contas e lançamentos do banco são apagados. O signal `busyId` trava os botões enquanto uma ação roda.
- `bank-connection.mapper.ts`: transforma `BankConnectionDto` no `BankConnection` da tela (cartões e contas separados pelo tipo, bandeira em `CardBrand` e `paymentSystem` do Taiga, status `needsReconnect`/`failed` viram `expired`).
- `pluggy-connect.service.ts` (`PluggyConnectService`): abre a janela da Pluggy (com `updateItem` quando é reconexão) (`pluggy-connect-sdk`, carregado só no clique, por `import()`) e devolve `connected` com o `itemId`, `closed` ou `failed`. Em desenvolvimento mostra também os bancos de teste da Pluggy (`environment.pluggyIncludeSandbox`).
- `transaction.mapper.ts`: `TransactionDto` → `Transaction` (a data vira o dia na zona do produto) e `MonthSummaryDto` → `MonthSummary`; categoria desconhecida vira `other`.
- `finance.dates.ts`: `today()`, `currentMonth()`, `recentMonths(n)` (o seletor de mês de Lançamentos oferece 12, o mesmo que a primeira sincronização traz) e `toProductDay()`, sempre em `America/Sao_Paulo`. É o único lugar do frontend que sabe que dia é hoje.
- `finance.calc.ts`: somas por origem e por categoria, usadas pelo Início e pelas Métricas. Lançamento com `ignored` ("não é gasto") não entra em soma nenhuma; Início e Métricas também o tiram da lista antes de calcular.
- Trocar categoria: em Lançamentos, a etiqueta de categoria é um botão com `tuiDropdown` (uma lista por linha, aberta pelo signal `menuFor`). `updateCategory`/`markNotSpending` chamam `PUT /api/transactions/{id}/category`, que muda também os lançamentos idênticos; a página recarrega o mês (signal `version`).
- Categorias da pessoa: `Category` é `BuiltInCategory` (as onze) ou `custom-<id>` (`CustomCategoryKey`), a mesma chave da API. `category.store.ts` (`CategoryStore`) guarda as da pessoa em signal (`custom`, `all` = onze + as dela) e fala com `/api/custom-categories`; cada página que mostra categoria chama `refresh()` no construtor. O nome na tela sai sempre pelo pipe `categoryName` (`category-name.pipe.ts`, impuro, porque lê o signal), nunca por `t('finance.categories.' + x)`. Em Lançamentos, "Nova categoria" e "Minhas categorias" abrem diálogos (`TuiDialogService` com `ng-template`); apagar confirma com `TUI_CONFIRM`.
- Nome escolhido: `Transaction.name` é o apelido da pessoa (`displayName` da API) ou a descrição do banco; `renamed` diz se há apelido. Tela mostra sempre `name`; `description` só aparece embaixo, em Lançamentos, quando há apelido. Clicar na descrição abre "Renomear lançamento" (`updateDisplayName`, `PUT /api/transactions/{id}/display-name`); nome vazio volta ao do banco. A busca olha os dois.
- Cartões e contas sem banco mostra o guia do Meu Pluggy (3 passos, `ng-template #steps`), e "Conectar banco" abre antes a janela "Antes de conectar" com os mesmos passos (`#beforeConnect`); só com "Já fiz isso, continuar" abre a Pluggy.
- Textos compartilhados (categorias, origens, "Pendente", parcelas) em `i18n/pt-BR/finance.json`.

## Autenticação (`app/auth/`)

- `auth.store.ts` (`AuthStore`): o estado da sessão em signals (`profile`, `isAuthenticated`, `needsPasswordChange`) e as ações (`login`, `register`, `logout`, `updateName`, `changePassword`, `setInitialPassword`). Componentes leem os signals e chamam as ações; ninguém mais guarda perfil ou token.
- A sessão é restaurada antes de a primeira rota abrir (`provideAppInitializer` em `app.config.ts`), então os guards sempre encontram o estado pronto.
- `auth.guard.ts`: `authGuard` manda quem não está logado para `/login` (guardando o destino em `?voltar=`) e quem tem senha temporária para `/definir-senha`; `guestGuard` tira quem já está logado do `/login`.
- `routes.ts`: os caminhos das rotas. Não escreva caminho solto em componente.
- `permissions.ts`: espelho das permissões da API, string idêntica. Hoje nenhuma tela usa permissão: qualquer conta logada entra. Esconder algo por permissão é só experiência de uso; a segurança é a policy do endpoint.
- Senha: mínimo de 6 caracteres e um número (`password-rules.ts`), igual à regra da API.
- A aba "Criar conta" só aparece se `GET /api/auth/registration` disser que o cadastro está ligado.

Recuperação de senha e verificação de e-mail não existem na API e não têm tela.

## Navegação

`layout/nav-config.ts` é a fonte única do menu lateral. Tela nova que aparece no menu: uma rota em `app.routes.ts` (filha da casca, com `loadComponent`) e um item no `nav-config`.

## Textos (i18n)

- Nenhum texto visível fica solto no template ou no código: tudo vem de `i18n/pt-BR/*.json`.
- Um arquivo por assunto (`common`, `auth`, `profile`, `api`, e um por tela ou domínio, como `finance`). A chave leva o nome do arquivo na frente: `auth.login.title`.
- Assunto novo: arquivo novo e uma linha em `i18n/index.ts`.
- No template: `*transloco="let t"` no elemento de fora e `{{ t('chave') }}`.
- Só pt-BR. A interface de data e número usa o locale `pt-BR` (`LOCALE_ID`).

## Tema e cor

- **Cor só em `theme/_colors.scss`**, em dois mapas: claro e escuro. Buscar `#` seguido de hexadecimal em `src/app` fora desse arquivo tem que voltar vazio.
- `theme/_theme.scss` liga cada cor do mapa a uma variável do Taiga (`--tui-*`) com `light-dark()`. O tema é **sempre claro** por decisão do dono: `color-scheme: light` no tema e `provideTaiga({ mode: 'light' })`. O mapa escuro fica guardado para se um dia o dono quiser o escuro de volta.
- As variáveis ficam em `:root` **e** em `[tuiTheme]`: o Taiga marca o `body` com `tuiTheme="dark"` e redefine a paleta dele ali, então só `:root` perderia para a do Taiga.
- Em componente, use os tokens do Taiga: `var(--tui-background-base)`, `var(--tui-text-secondary)`, `var(--tui-border-normal)`, `var(--tui-background-accent-1)`, e os próprios do projeto, `var(--app-color-*)`. Tipografia também por token: `font: var(--tui-typography-body-m)`; título usa `font-family: var(--tui-typography-family-display)` (Space Grotesk).
- `theme/_base.scss` tem as classes de layout compartilhadas (`app-page`, `app-card`, `app-form`, `app-title`, `app-subtitle`).
- Ícones: os do Taiga (Lucide), pelo nome: `<tui-icon icon="@tui.house" />` ou `iconStart="@tui.menu"` no botão. O `angular.json` copia os SVG para `assets/taiga-ui/icons`.
- O `App` envolve tudo em `<tui-root>`: sem ele, dropdown, notificação e dica não aparecem.
- Identidade "Azul calmo", decidida pelo dono. Mudar cor, fonte ou layout é decisão dele.
- Cor por banco (decisão do dono, 2026-11-15): `$banks` em `_colors.scss` guarda fundo e cor do texto de cada banco (texto escolhido pelo contraste, mínimo 4,5:1); o `_theme.scss` gera `.app-bank--<banco>` com `--app-bank` e `--app-on-bank`. O `bank-connection.mapper.ts` traduz o `bankCode` da API (código COMPE, ex.: `341`) na chave (`itau`) e `bankClass()` devolve a classe. Componente usa `var(--app-bank, <cor padrão>)`. Banco novo: uma linha em `$banks` e uma em `BANK_KEYS`.

## Formulários com Taiga

- Campo: `<tui-textfield>` com `<label tuiLabel for="x">` e `<input tuiInput id="x" formControlName="…">`. O `for`/`id` explícito é exigido pelo lint e liga o rótulo ao campo; use o prefixo da tela, em inglês (`login-email`, `profile-name`).
- Senha: `<tui-icon tuiPassword />` dentro do `tui-textfield` faz o mostrar/ocultar.
- Erro: `@if (campo.touched && campo.hasError('…')) { <tui-error [error]="t('auth.errors.…')" /> }`, logo abaixo do campo. O texto vem do Transloco, como todo texto.
- Botão de envio: `tuiButton` com `[loading]="submitting()"` (`TuiButtonLoading`, do `kit`).
- Alternância de abas: `tui-segmented`.

## Convenções

- Componentes globais têm prefixo `App` na classe e `app-` no seletor (`AppBrand`, `app-brand`). Páginas terminam em `Page` e ficam em `pages/<tela>/<tela>-page.ts`.
- Arquivos em kebab-case. Template e estilo em arquivos separados quando passam de poucas linhas.
- Estado em signals; `Observable` só na borda HTTP e em eventos.
- Injeção por `inject()`, não por construtor.
- Membros usados só pelo template são `protected`.
- **Nenhum comentário no código**, em `.ts`, `.html` ou `.scss`. O nome explica; o porquê mora aqui ou numa rule.
- Erro de API em ação de formulário: `try`/`catch` com `ignoreApiError(error)`. O aviso já foi mostrado pelo `ApiClient`; o `catch` só impede que um `ApiError` vire erro não tratado.

## Checklist: nova página

1. Pasta em `pages/<tela>/` com `<tela>-page.ts` (e `.html`/`.scss` se precisar).
2. Rota em `app.routes.ts` com `loadComponent`; caminho em `auth/routes.ts`.
3. Item no `layout/nav-config.ts`, se entra no menu.
4. Textos em `i18n/pt-BR/`.
5. Chamadas HTTP num serviço que usa `ApiClient`; tipos em `api/types.ts`.
6. Codes novos em `api/api-codes.ts` e `i18n/pt-BR/api.json`.
7. `npm run lint`, `npm run typecheck`, `npm run build`, e testar no navegador com a API local.

## Armadilhas conhecidas

- O 401 de token vencido chega **sem corpo**. Quem decide se renova é o interceptor, pela ausência de `code` no corpo; não tente tratar esse caso lendo o envelope.
- O logout da API exige um token válido: `AuthStore.logout` chama a API **antes** de limpar os tokens.
- `environment.ts` (produção) tem `apiBaseUrl` vazio: a URL da API de produção entra ali quando o deploy for decidido.
- O aviso de orçamento do build está em 700 kB para o pacote inicial.
- O Taiga 5 espera o idioma como **signal**: `{ provide: TUI_LANGUAGE, useValue: signal(TUI_PORTUGUESE_LANGUAGE) }`. Com `of(...)` (o jeito do Taiga 4, que ainda aparece em exemplos) os campos quebram com "language is not a function" e derrubam a tela inteira.
- O tema do Taiga é `.less`: o pacote `less` precisa estar nas devDependencies para o build.
- Select de filtro que não pode ficar vazio (Categoria e Mês em Lançamentos) leva `[tuiTextfieldCleaner]="false"` (`TuiTextfieldOptionsDirective`): o "×" do Taiga deixaria o valor `null` e a lista vazia.
- O `pluggy-connect-sdk` depende do `zoid`, que não é ESM: ele está em `allowedCommonJsDependencies` no `angular.json` para o build não avisar. Versão fixada (sem `^`) porque é a janela que mexe com login de banco.

## Como este arquivo evolui

Convenção nova ou mudada entra aqui no mesmo commit do código. Decisão com porquê longo vira uma rule em `frontend/.claude/rules/`, com `paths:`, e ganha uma linha neste arquivo.
