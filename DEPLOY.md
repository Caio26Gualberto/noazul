# Deploy do NoAzul

Passo a passo para colocar o NoAzul no ar. Decidido pelo dono em 2026-11-15: **AWS Lightsail** para a API e o banco, **AWS Amplify** para o site, domínio **noazul.space** comprado na **Cloudflare**, que também cuida do DNS.

```
noazul.space ─────────► Amplify (site Angular, HTTPS da AWS)
                               │ o navegador chama
api.noazul.space ─────► Lightsail (Ubuntu + Docker Compose)
                          ├─ Caddy  (HTTPS automático, portas 80/443)
                          ├─ API .NET (porta 8080 só dentro do Docker; Hangfire roda dentro)
                          └─ Postgres (só dentro do Docker, sem porta aberta)
```

Os arquivos usados aqui já estão no repositório:

| Arquivo | Para quê |
|---|---|
| `deploy/docker-compose.yml` | Postgres + API + Caddy de produção |
| `deploy/Caddyfile` | HTTPS e proxy de `api.noazul.space` para a API |
| `deploy/.env.example` | modelo das variáveis (o `deploy/.env` de verdade fica só no servidor) |
| `amplify.yml` | build do site no Amplify (pasta `frontend/`) |
| `frontend/src/environments/environment.ts` | já aponta para `https://api.noazul.space` |

**Custo aproximado:** Lightsail 2 GB ~US$ 12/mês, snapshots automáticos ~US$ 1–2/mês, Amplify de centavos a poucos dólares por mês (build e tráfego de duas pessoas), mais o domínio por ano.

---

## 1. Domínio na Cloudflare

1. Na Cloudflare, **Domain Registration → Register Domains**, compre `noazul.space`. A zona de DNS é criada sozinha.
2. Deixe a aba **DNS** aberta: você volta nela nos passos 3 e 8.

> Em 2026-11-15 a consulta pública (RDAP) já mostrava `noazul.space` como registrado. Se não foi você quem comprou, escolha outro nome e troque `noazul.space` em `deploy/.env` e em `frontend/src/environments/environment.ts`.

## 2. Servidor no Lightsail

1. No console da AWS, abra o **Lightsail → Create instance**.
   - **Região:** `Virginia (us-east-1)`. O Lightsail não tem São Paulo, e para duas pessoas a distância não faz diferença.
   - **Plataforma:** Linux/Unix → **OS Only → Ubuntu 24.04 LTS**.
   - **Plano:** **2 GB de RAM** (dual-stack, com IPv4). Com 1 GB o build da API e o Postgres juntos não cabem.
   - **Nome:** `noazul`.
2. Depois de criada, aba **Networking**:
   - **Create static IP** e anexe na instância `noazul`. Anote o IP.
   - Em **IPv4 Firewall**, deixe `SSH (22)`, `HTTP (80)` e **adicione `HTTPS (443)`**. Se quiser, restrinja o SSH ao seu IP.
3. Aba **Snapshots** → ligue **Automatic snapshots** (um por dia). É o backup do servidor inteiro, banco incluso.

## 3. DNS da API

Na Cloudflare, **DNS → Records → Add record**:

| Tipo | Nome | Conteúdo | Proxy |
|---|---|---|---|
| `A` | `api` | o IP estático do passo 2 | **DNS only** (nuvem cinza) |

A nuvem precisa estar **cinza**: o Caddy tira o certificado HTTPS falando direto com o servidor. Com a nuvem laranja, o certificado falha.

## 4. Preparar o servidor

No Lightsail, clique em **Connect using SSH** (ou use a chave `.pem` baixada da conta e `ssh ubuntu@<IP>`). Depois, rode:

```bash
# memória extra para o build da API não travar
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile
sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu
exit
```

Entre de novo (para o grupo `docker` valer) e confira com `docker run --rm hello-world`.

### Baixar o código

O repositório é privado, então o servidor precisa de uma chave só de leitura:

```bash
ssh-keygen -t ed25519 -C "noazul-lightsail" -f ~/.ssh/noazul_deploy -N ""
cat ~/.ssh/noazul_deploy.pub
```

No GitHub: repositório `noazul` → **Settings → Deploy keys → Add deploy key**, cole a chave e **não** marque "Allow write access". Depois:

```bash
cat >> ~/.ssh/config <<'EOF'
Host github.com
  IdentityFile ~/.ssh/noazul_deploy
EOF
git clone git@github.com:Caio26Gualberto/noazul.git ~/noazul
```

## 5. Variáveis de produção

```bash
cd ~/noazul/deploy
cp .env.example .env
chmod 600 .env
openssl rand -hex 24      # use como POSTGRES_PASSWORD
openssl rand -base64 48   # use como JWT_SECRET_KEY
nano .env
```

Preencha:

| Variável | Valor |
|---|---|
| `SITE_DOMAIN` | `noazul.space` |
| `API_DOMAIN` | `api.noazul.space` |
| `POSTGRES_PASSWORD` | a saída do `openssl rand -hex 24` |
| `JWT_SECRET_KEY` | a saída do `openssl rand -base64 48` (**nunca** a do `appsettings.Development.json`) |
| `PLUGGY_CLIENT_ID` / `PLUGGY_CLIENT_SECRET` | os mesmos do dashboard da Pluggy que você usa no desenvolvimento |
| `ALLOW_REGISTRATION` | `true` só até as duas contas existirem (passo 9) |

O `deploy/.env` nunca vai para o Git (o `.gitignore` já ignora `.env`).

## 6. Banco e migrations

Em produção a API **não** aplica migration sozinha (`Database:MigrateOnStartup` é `false`). Elas entram por um script SQL que pode rodar quantas vezes for preciso:

1. **No seu computador**, com a API local parada (senão o build do `dotnet ef` falha com o arquivo em uso), na pasta `api/`:
   ```bash
   dotnet ef migrations script --idempotent \
     --project src/NoAzul.Infra.Data --startup-project src/NoAzul.Api \
     -o noazul-migrations.sql
   scp noazul-migrations.sql ubuntu@<IP>:~/
   ```
2. **No servidor:**
   ```bash
   cd ~/noazul/deploy
   docker compose up -d postgres
   docker compose exec -T postgres psql -U noazul -d noazul -v ON_ERROR_STOP=1 < ~/noazul-migrations.sql
   ```

As tabelas do Hangfire a API cria sozinha quando sobe.

## 7. Subir a API

```bash
cd ~/noazul/deploy
docker compose up -d --build
docker compose ps
docker compose logs -f api      # Ctrl+C para sair
```

Confira:

```bash
curl https://api.noazul.space/health    # tem que responder "Healthy"
```

Se o HTTPS não sair, veja `docker compose logs caddy` e a seção "Problemas comuns".

## 8. Site no Amplify

1. No console da AWS, abra o **Amplify → Create new app → GitHub**, autorize e escolha o repositório `noazul`, branch `main`.
2. Marque **"My app is a monorepo"** e informe a pasta `frontend`. O Amplify lê o `amplify.yml` da raiz; confira se o diretório de saída aparece como `dist/frontend/browser`.
3. **Save and deploy.** O primeiro build leva alguns minutos.
4. Para as rotas do Angular (`/lancamentos`, `/cartoes`…) funcionarem ao recarregar a página: **Hosting → Rewrites and redirects → Manage → Add rewrite**:
   - Source: `</^[^.]+$|\.(?!(css|gif|ico|jpg|js|png|txt|svg|woff|woff2|ttf|map|json|webp)$)([^.]+$)/>`
   - Target: `/index.html`
   - Type: `200 (Rewrite)`
5. **Hosting → Custom domains → Add domain** → `noazul.space`, com `www` também. Como o DNS está na Cloudflare, escolha a configuração **manual**: o Amplify mostra os registros `CNAME`.
6. Na Cloudflare, crie cada registro que o Amplify pediu, **todos com a nuvem cinza (DNS only)**:
   - o `CNAME` de verificação do certificado (nome comprido com `_`);
   - `www` → o endereço `*.cloudfront.net` que o Amplify mostrar;
   - `@` (o domínio puro) → o mesmo endereço `*.cloudfront.net`. A Cloudflare aceita `CNAME` no domínio puro (CNAME flattening).
7. Espere o Amplify mostrar o domínio como **Available**. Pode levar de minutos a algumas horas.

Daqui em diante, todo push na `main` publica o site sozinho.

## 9. Primeiro acesso

1. Abra `https://noazul.space`, crie a sua conta e peça para o seu pai criar a dele.
2. **Feche o cadastro:** no servidor, troque `ALLOW_REGISTRATION=false` no `deploy/.env` e rode:
   ```bash
   cd ~/noazul/deploy && docker compose up -d api
   ```
   A tela de login deixa de mostrar "Criar conta".
3. Em Cartões e contas, cada um segue o guia do Meu Pluggy e conecta os próprios bancos.
4. No dia seguinte, depois das 8h, confira a leitura diária:
   ```bash
   docker compose logs api | grep BANK_CONNECTIONS_SYNCED
   ```

O painel do Hangfire (`/hangfire`) não existe em produção, de propósito (`api/.claude/rules/jobs-hangfire.md`). Acompanhe os jobs pelos logs.

## 10. Atualizar depois

**Site:** não precisa fazer nada. Push na `main` → o Amplify faz o build e publica.

**API:**

```bash
cd ~/noazul && git pull
# só se o commit trouxe migration nova: refaça o passo 6 (o script é idempotente)
cd deploy && docker compose up -d --build api
docker compose logs -f api
```

## 11. Backup

- Os **snapshots automáticos** do Lightsail (passo 2) guardam o servidor inteiro, um por dia.
- Se quiser um dump do banco também:
  ```bash
  mkdir -p ~/backups
  crontab -e
  # todo domingo às 4h (UTC), guarda 8 semanas:
  0 4 * * 0 cd /home/ubuntu/noazul/deploy && docker compose exec -T postgres pg_dump -U noazul noazul | gzip > /home/ubuntu/backups/noazul-$(date +\%F).sql.gz && find /home/ubuntu/backups -name '*.sql.gz' -mtime +56 -delete
  ```
- Para restaurar um dump: `gunzip -c arquivo.sql.gz | docker compose exec -T postgres psql -U noazul -d noazul`.

## Problemas comuns

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| `curl https://api.noazul.space/health` falha e o log do Caddy fala em certificado | nuvem laranja na Cloudflare, ou porta 80/443 fechada | registro `api` com nuvem **cinza**; firewall do Lightsail com 80 e 443 |
| Site abre, mas o login dá "erro de conexão" e o console do navegador mostra CORS | `SITE_DOMAIN` diferente do endereço do site | confira `deploy/.env` (sem `https://`, sem barra no fim) e rode `docker compose up -d api` |
| Recarregar `/lancamentos` dá 404 | falta o rewrite do Amplify | passo 8.4 |
| Build da API morre sem mensagem | faltou memória | confira o swap (`free -h`) |
| A API não sobe e reclama de tabela inexistente | migration não aplicada | passo 6 |
| A janela da Pluggy não abre no site | o dashboard da Pluggy pode exigir o domínio liberado | no dashboard da Pluggy, procure a lista de domínios/URLs permitidos da aplicação e adicione `https://noazul.space` |
| Ninguém consegue criar conta | `ALLOW_REGISTRATION=false` | é o esperado depois do passo 9; para abrir de novo, troque para `true` e suba a API |

## Checklist

- [ ] Domínio `noazul.space` na Cloudflare
- [ ] Lightsail 2 GB, IP estático, portas 80/443, snapshots automáticos
- [ ] `A api` → IP, nuvem cinza
- [ ] Swap, Docker, deploy key, repositório clonado
- [ ] `deploy/.env` com segredos novos e `chmod 600`
- [ ] Migrations aplicadas
- [ ] `https://api.noazul.space/health` → `Healthy`
- [ ] Amplify com rewrite de SPA e domínio `noazul.space` + `www`
- [ ] Duas contas criadas e `ALLOW_REGISTRATION=false`
- [ ] Bancos conectados e `BANK_CONNECTIONS_SYNCED` no log do dia seguinte
