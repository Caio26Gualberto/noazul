# NoAzul

Organização financeira pessoal: gastos de cartão e conta vindos da Pluggy e métricas do mês.

Fase atual: autenticação (cadastro com trava, login, perfil, troca de senha) e as telas de gastos com dados reais da Pluggy.

## O que precisa estar instalado

- .NET SDK 10
- Node.js 24
- Docker Desktop

## Rodar no computador

São três terminais, nesta ordem.

**1. Banco** (na raiz do projeto)

```bash
docker compose up -d
```

Sobe um Postgres só do NoAzul na porta 5440. Na primeira vez, copie `.env.example` para `.env` e escolha uma senha; a mesma senha vai na connection string de `api/src/NoAzul.Api/appsettings.Development.json`.

**2. API** (em `api/src/NoAzul.Api`)

```bash
dotnet run
```

Fica em `http://localhost:5041`. As tabelas são criadas sozinhas na primeira subida. A documentação dos endpoints abre em `http://localhost:5041/swagger`.

**3. Site** (em `frontend`)

```bash
npm install
npm start
```

Abre em `http://localhost:4200`.

Para parar o banco: `docker compose stop`.

## Configuração local da API

`api/src/NoAzul.Api/appsettings.Development.json` não vai para o Git. Ele precisa ter:

| Chave | Para quê |
|---|---|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;Port=5440;Database=noazul;Username=noazul;Password=<a do .env>` |
| `Database:MigrateOnStartup` | `true`, para criar e atualizar as tabelas ao subir |
| `JWT:SecretKey` | chave longa e aleatória que assina os tokens |
| `Cors:AllowedOrigins` | `["http://localhost:4200"]` |
| `Auth:AllowRegistration` | `true` para a tela "Criar conta" funcionar |
| `Seed:AdminPassword` | senha da conta `admin@noazul.local`, criada sozinha fora de produção |
| `Pluggy:ClientId` e `Pluggy:ClientSecret` | credenciais da aplicação no dashboard da Pluggy; sem elas o botão "Conectar banco" responde que a integração não está configurada |

## Conectar os bancos (Pluggy)

Uma vez, pelo dono:

1. Criar a conta em https://dashboard.pluggy.ai, criar uma aplicação e ativar nela o conector **MeuPluggy**.
2. Copiar o `Client ID` e o `Client Secret` da aplicação para `Pluggy:ClientId` e `Pluggy:ClientSecret` no `appsettings.Development.json` e reiniciar a API.

Por pessoa:

1. Criar a conta grátis em https://meu.pluggy.ai e conectar lá os próprios bancos (uma vez por banco).
2. No NoAzul, em Cartões e contas, clicar em "Conectar banco", escolher **MeuPluggy** e autorizar. Repetir para cada banco.

Os dados chegam logo depois de conectar e, depois, uma vez por dia às 08:00. Em desenvolvimento, a janela também mostra os bancos de teste da Pluggy.

## Cadastro de contas

O cadastro tem trava. Com `Auth:AllowRegistration` em `false` (o padrão), a API recusa novas contas e a aba "Criar conta" some da tela de login. Em produção, ligue só para criar as contas e desligue em seguida.

## Onde ler mais

- `CLAUDE.md`: mapa do projeto e decisões fechadas.
- `api/CLAUDE.md` e `frontend/CLAUDE.md`: como cada parte é organizada.
