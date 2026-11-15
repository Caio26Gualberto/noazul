---
paths:
  - "**/Dockerfile"
  - "infra/**"
  - ".github/workflows/deploy-api.yml"
  - "amplify.yml"
  - "**/NoAzul.Api/Program.cs"
  - "**/appsettings.Production.json"
---

# Decisões — Hospedagem em produção

Decidido pelo dono em 2026-11-15: AWS, tudo na região **São Paulo (`sa-east-1`)**, no mesmo molde do Noise Vault (`C:\Projetos\Pessoal\Headbanger`, rule `api/.claude/rules/hospedagem.md` de lá). O passo a passo é o `DEPLOY.md` da raiz; esta rule guarda o porquê.

| Peça | Onde | Observação |
|---|---|---|
| API + Hangfire + Postgres | **Lightsail** em São Paulo, 2 GB | `docker compose` (`infra/lightsail/`) com API, Postgres e Caddy (HTTPS) |
| Site | **Amplify** em São Paulo, monorepo `frontend/` | `amplify.yml` da raiz |
| Backup | **S3** `noazul-backups`, `pg_dump -Fc` diário às 4h30 UTC, lifecycle de 30 dias | mais o snapshot automático do Lightsail |
| DNS | **Cloudflare**, onde `noazul.space` foi comprado | todo registro **DNS only** |

## Domínio: `noazul.space`

Comprado pelo dono na Cloudflare em 2026-11-15. O `noazul.com` já tinha uso; `.com.br` saiu porque o dono não quer o DNS do Registro.br. Domínio do Cloudflare Registrar não troca de servidores de nome, então não há Route 53.

| Endereço | Aponta para |
|---|---|
| `noazul.space` | Amplify (CNAME na raiz, flattening da Cloudflare) |
| `www.noazul.space` | redirect do Amplify para `noazul.space`, para um endereço só (CORS, sessão) |
| `api.noazul.space` | IP estático do Lightsail, com o Caddy tirando o certificado |

Nuvem laranja (proxy) na frente da API quebra o certificado do Caddy, e na frente do Amplify só duplica CDN.

## A API não é compilada no servidor

`.github/workflows/deploy-api.yml` builda a imagem no GitHub Actions a cada push na `main` que toque `api/` ou `infra/lightsail/`, publica em `ghcr.io/caio26gualberto/noazul-api` (`latest` e o sha) e entra no Lightsail por SSH (`LIGHTSAIL_HOST`, `LIGHTSAIL_SSH_KEY`) para `git reset --hard origin/main`, `docker compose pull api` e `up -d`. Por isso 2 GB bastam. O `build:` continua no compose como saída de emergência.

- O workflow só roda com a variable `LIGHTSAIL_ENABLED=true`, para o push de antes do servidor não falhar.
- O pacote do GHCR é privado; o servidor lê com um token clássico `read:packages` (`docker login ghcr.io`). O repositório é público, então o clone não precisa de deploy key.
- O servidor é preparado uma vez por `infra/lightsail/bootstrap.sh` (curl … | bash); o `.env` fica só no servidor, com as chaves no formato do .NET (`JWT__SecretKey`), lido por `env_file`.
- Descartado (primeira versão do `DEPLOY.md`, no mesmo dia): compilar no servidor e clonar com deploy key. O dono pediu o padrão do Noise Vault.

## Migrations em produção rodam no boot

O compose liga `Database__MigrateOnStartup=true`. É uma instância só, o servidor não tem o SDK do .NET para `dotnet ef`, e o `MigrateAsync` é idempotente. A frase "`MigrateOnStartup = true` só em desenvolvimento" do `api/CLAUDE.md` vale para mais de uma instância subindo ao mesmo tempo, que é quando duas migrations concorrem. Volta a valer se um dia houver duas.

- Descartado: script SQL idempotente gerado na máquina do dono e aplicado à mão. Funcionava (visto em 2026-11-15), mas era um passo manual a cada migration.

## O resto

- **Caddy** repassa `X-Forwarded-Proto`; o `ForwardedHeaders` da API já confia em qualquer proxy (`KnownProxies` limpo), então o IP real chega ao rate limit.
- **Log** do Docker com rotação (5 × 50 MB por container).
- **Cadastro**: `Auth__AllowRegistration=true` só até as duas contas existirem; depois `false`.
- **Seed**: não cria admin em `Production`, e o NoAzul não precisa de um.
- **Hangfire**: sem dashboard em produção (`jobs-hangfire.md`); a leitura diária é acompanhada pelo log `BANK_CONNECTIONS_SYNCED`.
- **Amplify** fica, ao contrário do Noise Vault, que foi para S3 + CloudFront por causa da prévia de links (Open Graph pelo `User-Agent`). O NoAzul não tem página pública para prévia.

## Roteiro de verificação

O do `DEPLOY.md`, passo 10. Antes de subir mudança no compose, dá para testar localmente com outro nome de projeto e nomes de container próprios (o `noazul-postgres` do compose de produção colide com o do desenvolvimento): um override com `container_name` diferentes, `docker compose -p noazul-smoketest -f docker-compose.yml -f <override> up -d postgres api`, `/health` → `Healthy` pela rede do compose, e `down -v` no fim.
