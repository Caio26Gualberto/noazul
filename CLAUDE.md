# CLAUDE.md — NoAzul (monorepo)

## Sobre este projeto

**NoAzul** é um sistema de organização financeira pessoal: mostra os gastos de cartão e de conta vindos da Pluggy e calcula métricas do mês. É usado por duas pessoas (o dono e o pai dele), cada uma com a própria conta.

O que ele **não** é: não movimenta dinheiro (só lê e organiza), não é produto comercial (sem cadastro aberto ao público, sem planos), não cuida de investimentos e não é em tempo real (os dados do banco chegam uma vez por dia).

Fase atual: autenticação funcionando de ponta a ponta (cadastro com trava, login, renovação de sessão, perfil, troca de senha); Cartões e contas, Lançamentos, Início e Métricas com dados reais da Pluggy (sincronização diária). No ar desde 2026-11-15 em `https://noazul.space` (Lightsail + Amplify em São Paulo, roteiro em `DEPLOY.md`).

## Mapa

| Pasta | Projeto | Stack | Leia primeiro |
|---|---|---|---|
| `api/` | API | .NET 10 · EF Core (Postgres) · Identity + JWT · Hangfire | `api/CLAUDE.md` |
| `frontend/` | Site | Angular 22 · Taiga UI · Transloco | `frontend/CLAUDE.md` |
| `.github/workflows/` | CI (`ci.yml`, com o check `ci-ok`) | GitHub Actions | este arquivo |
| `infra/lightsail/` + `.github/workflows/deploy-api.yml` + `amplify.yml` | Produção | Docker Compose (Lightsail) · GHCR · Amplify | `DEPLOY.md` e `api/.claude/rules/hospedagem.md` |

Ordem de trabalho: **api → frontend**. O frontend espelha codes e permissões da API; a API não sabe que ele existe.

Os `CLAUDE.md` de cada projeto são **convenção** (como codar); `<projeto>/.claude/rules/` são **decisões** (o porquê, carregadas pelo `paths:` quando o agente toca os arquivos). Se o projeto tiver `docs/specs/`, elas dizem o **comportamento** de cada funcionalidade. Não confunda.

## Contratos entre projetos

- **Envelope** `{ isSuccess, code, message, data }` (camelCase). `code` é SCREAMING_SNAKE, estável e específico; é a chave de i18n no frontend. Code novo na API → espelhado em `frontend/src/app/api/api-codes.ts` + `frontend/src/app/i18n/pt-BR/api.json`, no mesmo commit.
- **Permissões** `Area.Recurso.Acao`: catálogo na API (`PermissionCatalog`), espelho em `frontend/src/app/auth/permissions.ts`, string idêntica. Gate do front é UX; segurança é a policy do endpoint.
- **DTOs**: os tipos do frontend são escritos à mão em `frontend/src/app/api/types.ts`. DTO que muda na API muda lá no mesmo commit.

## Decisões fechadas que não se reabrem sem o dono

- Nome do projeto: NoAzul (2026-11-14).
- Escopo: organização financeira pessoal para o dono e o pai; não movimenta dinheiro, não é produto comercial, não cuida de investimentos, não é em tempo real (2026-11-14).
- Templates: só API e frontend; sem desktop, mobile ou Atalog (2026-11-14).
- Frontend em Angular, escolhido para o dono aprender Angular (2026-11-14). A biblioteca de UI começou como Angular Material e foi trocada pela Taiga UI (ver abaixo).
- Idioma: só pt-BR (2026-11-14).
- Fuso do produto: `America/Sao_Paulo`; o banco fica em UTC (2026-11-14).
- Dono no GitHub: `Caio26Gualberto` (2026-11-14).
- Identidade visual "Azul calmo", escolhida pelo dono entre três mockups: claro por padrão com escuro automático, azul vivo como cor principal, fontes Space Grotesk (títulos) e Inter (2026-11-14). Atualizado no mesmo dia: o dono pediu o tema **sempre claro**, como no mock dos módulos; o modo escuro automático foi desligado (o mapa escuro continua em `_colors.scss`, sem uso) (2026-11-14).
- Cadastro com trava: `Auth:AllowRegistration` liga e desliga a criação de contas; em produção fica desligado depois que as duas contas existirem (2026-11-14).
- Banco: Postgres (2026-11-14).
- Dados bancários virão da Pluggy, pela modalidade gratuita de uso pessoal (Meu Pluggy), com atualização diária (2026-11-14).
- Tempo real reavaliado e descartado: nem o plano pago da Pluggy entrega (no máximo de hora em hora, a partir de R$ 2.500/mês); fica a atualização diária do Meu Pluggy (2026-11-14).
- Módulos, aprovados pelo dono sobre o mock "NoAzul — Módulos" (https://claude.ai/artifact/M9YeXSTjB7U4kAwaTkZqKL) (2026-11-14):
  - Menu: Início, Lançamentos, Cartões e contas, Métricas e Perfil, com "Lançar Pix" como ação de destaque. O "Lançar Pix" saiu em 2026-11-15 (ver abaixo).
  - Início: total do mês, divisão entre cartões, conta e Pix, gastos por categoria, faturas abertas e últimos lançamentos.
  - Lançamentos: lista única de cartões, conta e Pix, com filtros por origem, categoria, mês e busca.
  - Categorias: lista própria de sete (Mercado, Casa, Restaurantes, Transporte, Saúde, Assinaturas, Outros). Ampliada para onze em 2026-11-15 (ver abaixo).
  - Pix manual: lançamento próprio, sem depender de conta conectada. Removido em 2026-11-15 (ver abaixo).
  - Cartões e contas: uma conexão por banco, com a última atualização, fatura, limite, fechamento, vencimento e saldo da conta.
  - Métricas: gasto por mês, divisão por origem, maiores gastos e categorias do mês contra o anterior.
  - Cada pessoa vê só a própria conta; não há visão da família junta.
- Biblioteca de UI: Taiga UI no lugar do Angular Material. O dono achou o visual fraco e escolheu depois de comparar a tela de Início montada em Material, PrimeNG, Spartan UI e Taiga UI. Substitui a escolha do Angular Material; a identidade "Azul calmo" continua valendo, então as cores próprias do Taiga (como os gradientes dos cartões) seguem a paleta da marca. A migração do `frontend/` foi feita no mesmo dia (2026-11-14).
- Conexão com o banco: botão "Conectar banco" na tela Cartões e contas, que abre a janela oficial da Pluggy (Pluggy Connect); o NoAzul guarda a conexão sozinho. Descartado: colar o Item ID copiado do meu.pluggy.ai (2026-11-15).
- Cor por banco: o quadrado da marca e a miniatura de cada cartão usam a cor do banco (Itaú laranja, Nubank roxo…), em Cartões e contas e nas faturas do Início. Substitui, só nesses dois lugares, a regra de que as cores dos cartões seguem a paleta da marca; o resto continua "Azul calmo". Banco sem cor conhecida fica neutro na marca e azul no cartão (2026-11-15).
- Categorias passam a ser onze: Mercado, Restaurantes, Transporte, Saúde, Assinaturas, Casa, Compras, Lazer e viagem, Educação, Tarifas e impostos e Outros. A categoria da Pluggy de cada lançamento é traduzida por uma tabela fixa (rule `api/.claude/rules/lancamentos.md`); o que não se encaixa vai para Outros (2026-11-15).
- Gasto é só saída: compras no cartão e débitos da conta. Ficam de fora as entradas (pagamento de fatura no cartão, estorno, Pix recebido, rendimento), o pagamento de fatura feito pela conta, a transferência entre contas da mesma pessoa e a aplicação em investimento. Estorno não desconta (2026-11-15).
- Sem lançamento manual: o "Lançar Pix" saiu do menu, de Lançamentos e da API (a origem "Pix" e o card "Pix lançados" do Início também). Os Pix que saem das contas conectadas já vêm da Pluggy, e lançar à mão contaria duas vezes. Gasto em dinheiro vivo e de banco não conectado fica fora do NoAzul. Início e Lançamentos passam a dividir entre cartões e conta (2026-11-15).
- Trocar categoria e "não é gasto": em Lançamentos, clicar na categoria abre a lista das onze mais "Não é gasto". A escolha vale para todos os lançamentos da pessoa com a descrição idêntica, os que já existem e os que chegarem depois. "Não é gasto" tira dos totais do Início, das Métricas e de Lançamentos, mas o lançamento continua na lista, apagado (2026-11-15).
- Categorias da pessoa: além das onze, cada pessoa cria as próprias pelo menu da categoria em Lançamentos ("Nova categoria"), e elas funcionam como qualquer outra (filtro, Início, Métricas), presas à descrição para sempre. Renomear e apagar ficam em "Minhas categorias"; apagar pede confirmação e devolve os lançamentos para a categoria automática (2026-11-15).
- Nome do lançamento: fica a descrição do banco, e a pessoa pode dar outro nome clicando nela em Lançamentos. O nome vale para todos com a mesma descrição, para sempre, e aparece no Início e nas Métricas. Descartado: usar o nome do estabelecimento da Pluggy (vem em menos de 1 a cada 5 gastos, quase sempre a razão social) (2026-11-15).
- Guia do Meu Pluggy: Cartões e contas sem banco explica os 3 passos (criar conta no meu.pluggy.ai, conectar os bancos lá, voltar e escolher "Meu Pluggy"), e "Conectar banco" mostra os mesmos passos antes de abrir a Pluggy (2026-11-15).
- O template de exemplo (`Items`) saiu da API e do frontend; as features de referência passam a ser `BankConnections` e `Transactions` (2026-11-15).
- Histórico: a primeira sincronização de cada conta traz 12 meses; depois, a diária relê os últimos 30 dias (2026-11-15).
- Filtro global por banco (2026-11-15): um seletor "tenant" na barra de topo escolhe um banco conectado ou "Todas as contas", e a escolha vale em Início, Lançamentos e Métricas (Cartões e contas sempre mostra todos). É por conexão (um banco inteiro, não conta a conta), persiste por pessoa entre sessões e some com menos de dois bancos. Descartado: filtrar por conta individual e deixar a escolha só por navegação.
- Reconectar e desconectar ficam no menu "Gerenciar" de cada banco, em Cartões e contas. Desconectar apaga do NoAzul a conexão, os cartões, as contas e todos os lançamentos do banco, e apaga também o item na Pluggy; a confirmação avisa isso antes (2026-11-15).
- Cada pessoa usa o próprio Meu Pluggy e conecta só os próprios bancos; o NoAzul usa um único `client_id`/`client_secret`, da aplicação do dono no dashboard da Pluggy (2026-11-15).
- Sem selo "Pendente" em Lançamentos e no Início: no cartão a Pluggy marca como pendente tudo o que está na fatura aberta, mesmo já aprovado. O pendente continua contando nos totais, como antes (2026-11-23).
- Deploy, tudo na região São Paulo (sa-east-1): AWS Lightsail (Ubuntu, 2 GB, Docker Compose com API + Postgres + Caddy, imagem buildada no GitHub Actions e publicada no GHCR) para `api.noazul.space` e AWS Amplify para o site em `noazul.space`. Domínio e DNS na Cloudflare (o Registro.br foi descartado pelo DNS ruim de configurar). Banco no mesmo Lightsail, com snapshot diário; descartado o banco gerenciado (~US$ 22/mês). Migrations em produção no boot (uma instância só). Backup diário no S3. Mesmo molde do Noise Vault. Passo a passo em `DEPLOY.md` (2026-11-15).

## Como o agente trabalha aqui

1. Antes de qualquer task: ler este arquivo, o `CLAUDE.md` do projeto tocado e a rule do domínio.
2. Decisão de produto, marca, cor, layout, lib de UI ou comportamento **não** se preenche com preferência própria: pergunta-se ao dono, com alternativas e recomendação, e espera-se a decisão. Silêncio não é aprovação.
3. Não provisionar infraestrutura nem publicar nada sem aprovação explícita e separada.
4. Verificação é por execução real, com o roteiro da rule ou da spec. Doc e código vão no mesmo commit.
5. Divergência entre doc e código se registra (em "Armadilhas conhecidas" do projeto), não se apaga.
6. Push e PR seguem a skill `abrir-pr` (`.claude/skills/abrir-pr/SKILL.md`), que chama o subagente `revisor` (`.claude/agents/revisor.md`, só leitura, veredito APROVADO/BLOQUEADO) antes de subir. A `main` é protegida por ruleset: só o dono (`Caio26Gualberto`) faz push direto; todo o resto entra por PR, com `ci-ok` verde e aprovação do dono, merge por squash.
7. A fonte da verdade das skills e subagentes é `.claude/skills/` e `.claude/agents/`. Para outras ferramentas (Cursor, Codex), `.agents/` tem uma cópia fina de cada um que aponta para a do `.claude/`, e o `AGENTS.md` aponta para elas. Skill ou agente novo ou alterado: atualizar as duas cópias (a fina só muda se o nome ou a descrição mudar).
8. **Um assunto por branch.** Quem não é o dono trabalha sempre numa branch própria do assunto, nunca na `main`. Pedido sem relação com o assunto da branch atual: avise, commite o pendente e crie uma branch nova a partir da `main` atualizada (`git fetch origin && git switch -c <tipo>/<assunto> origin/main`). Se o pedido novo depende da branch atual, pergunte se entra nela ou espera o merge.
9. **Código em inglês, produto em pt-BR.** Nome de classe, método, variável, tipo, arquivo, pasta e valor interno (enum, id de categoria) em inglês: `TransactionsPage`, `finance.service.ts`, `'groceries'`. Fica em pt-BR o que a pessoa vê: textos do i18n, caminhos de URL (`/lancamentos`) e dados de exemplo (nome de loja). Documentação e mensagens de commit seguem em pt-BR.

## Hierarquia de regras

1. Rule do domínio (`<projeto>/.claude/rules/*.md`).
2. `CLAUDE.md` do projeto.
3. Este arquivo.
4. Spec (para comportamento), se o projeto tiver.
5. Convenção do framework.

Uma rule que contradiz o `CLAUDE.md` do projeto diz por quê; se não diz, está errada.

## Ambientes

Development (local) e Production. Segredos só em `api/src/NoAzul.Api/appsettings.Development.json` e no `.env` da raiz (os dois fora do Git) e nas variáveis do ambiente de produção.

O banco local é um Postgres só deste projeto, em container (`docker-compose.yml` da raiz), na porta **5440** para não bater com outros bancos da máquina. A senha fica no `.env` da raiz (modelo em `.env.example`) e tem que ser a mesma da connection string do `appsettings.Development.json`. O passo a passo para rodar está no `README.md`. Os arquivos `frontend/src/environments/*.ts` são versionados e só guardam a URL pública da API.
