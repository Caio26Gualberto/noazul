# Deploy do NoAzul na AWS (São Paulo), pelo console

Roteiro do primeiro deploy, feito pelo painel da AWS, na região **América do Sul (São Paulo), `sa-east-1`**, no mesmo molde do Noise Vault (`C:\Projetos\Pessoal\Headbanger`). As decisões por trás de cada peça estão em `api/.claude/rules/hospedagem.md`; aqui fica só o passo a passo. Siga na ordem: cada bloco usa algo criado no anterior.

O que vai existir no fim:

| Peça | Serviço | Endereço |
|---|---|---|
| API, Hangfire, Postgres, Caddy | Lightsail (1 instância de 2 GB, Docker) | `api.noazul.space` |
| Site | Amplify (um app, monorepo `frontend/`) | `noazul.space` (`www` redireciona) |
| Backup do banco | S3 privado, `pg_dump` diário | sem endereço |
| Imagem da API | GitHub Container Registry, publicada pelo GitHub Actions | `ghcr.io/caio26gualberto/noazul-api` |
| DNS | Cloudflare (onde o domínio foi comprado) | zona `noazul.space` |

Arquivos do repositório usados aqui: `infra/lightsail/` (compose, `Caddyfile`, `.env.example`, `bootstrap.sh`, `backup.sh`), `.github/workflows/deploy-api.yml`, `api/Dockerfile` e `amplify.yml` na raiz. O `frontend/src/environments/environment.ts` já aponta para `https://api.noazul.space`.

**Custo aproximado:** Lightsail 2 GB ~US$ 12/mês, snapshot automático ~US$ 1–2/mês, S3 do backup em centavos, Amplify de centavos a poucos dólares (duas pessoas), mais o domínio por ano.

## 0. Antes de abrir o console

Tenha em mãos:

- a conta AWS com o usuário IAM administrador do dia a dia (nunca o root);
- o painel da **Cloudflare**, onde `noazul.space` foi comprado em 2026-11-15: todo registro de DNS entra lá, sempre **DNS only** (nuvem cinza);
- o **Client ID e o Client Secret da Pluggy**, os mesmos do `appsettings.Development.json`;
- um segredo novo para o JWT, gerado no seu terminal: `openssl rand -base64 48`. **Nunca** reaproveite o do desenvolvimento;
- uma senha longa para o Postgres: `openssl rand -hex 24`.

No canto superior direito do console, escolha **South America (São Paulo)**. Tudo abaixo é nessa região.

## 1. DNS: a zona na Cloudflare

O domínio já está na Cloudflare, e a zona existe. Os registros entram nos passos 4 e 8. Domínio do Cloudflare Registrar não troca de servidores de nome, então o Route 53 fica de fora.

## 2. S3: o bucket do backup

1. **S3 → Create bucket.** Nome `noazul-backups`, região São Paulo, **Block all public access** ligado (o padrão), versionamento desligado. Crie.
2. No bucket, **Management → Create lifecycle rule**: nome `apagar-30-dias`, escopo prefixo `postgres/`, ação **Expire current versions of objects** com **30 dias**.

## 3. IAM: o usuário do backup

O Lightsail não tem IAM role como o EC2, então o `backup.sh` usa a chave de um usuário com o mínimo.

1. **IAM → Users → Create user** `noazul-backup`, sem acesso ao console.
2. **Add permissions → Create inline policy → JSON**:
   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": ["s3:PutObject", "s3:GetObject"],
         "Resource": "arn:aws:s3:::noazul-backups/postgres/*"
       },
       {
         "Effect": "Allow",
         "Action": "s3:ListBucket",
         "Resource": "arn:aws:s3:::noazul-backups",
         "Condition": { "StringLike": { "s3:prefix": "postgres/*" } }
       }
     ]
   }
   ```
   Nome `noazul-backup-s3`.
3. No usuário, **Security credentials → Create access key → Other**. Guarde o Access key ID e o Secret: usa no passo 7.

## 4. Lightsail: o servidor da API

### 4.1 Criar a instância

1. **Lightsail → Instances → Create instance.** Região São Paulo (`sa-east-1`, zona A). Plataforma Linux, blueprint **OS Only → Ubuntu 24.04 LTS**. Plano **2 GB RAM** (dual-stack, com IPv4). Nome `noazul-api`. Crie.
   - 2 GB sobra, porque o servidor não compila nada: a imagem vem pronta do GitHub (passo 6).
2. **Networking → IPv4 networking → Attach static IP**: crie `noazul-api-ip` e anexe. Anote o IP.
3. **Networking → IPv4 Firewall**: deixe `SSH 22` e `HTTP 80` e adicione **HTTPS 443** (TCP). O `443 UDP` (HTTP/3 pelo Caddy) é opcional.
4. Na Cloudflare, **Add record** tipo **A**, Name `api`, IPv4 o IP estático, **DNS only** (nuvem cinza). Com o proxy laranja, o Caddy não consegue tirar o certificado.
5. **Lightsail → instância → Snapshots → Automatic snapshots: Enable**, horário de madrugada. É o backup do disco inteiro.

### 4.2 Preparar o servidor (uma vez, pelo SSH do navegador)

É a única sessão manual no servidor. Na página da instância, clique em **Connect using SSH** e cole:

```bash
curl -fsSL https://raw.githubusercontent.com/Caio26Gualberto/noazul/main/infra/lightsail/bootstrap.sh | bash
```

O `bootstrap.sh` atualiza o Ubuntu, instala Docker e AWS CLI, cria 2 GB de swap, clona o repositório em `~/noazul` e copia o `.env.example` para `.env`. Leva uns 3 minutos. No fim, ele lista o que falta. Faça nesta ordem:

1. `nano ~/noazul/infra/lightsail/.env` e preencha:

   | Variável | Valor |
   |---|---|
   | `POSTGRES_PASSWORD` | a senha do passo 0 |
   | `JWT__SecretKey` | o segredo do passo 0 |
   | `Cors__AllowedOrigins__0` | `https://noazul.space` (já vem preenchido) |
   | `Auth__AllowRegistration` | `true` só até as duas contas existirem (passo 9) |
   | `Pluggy__ClientId` / `Pluggy__ClientSecret` | os da Pluggy |

2. `docker login ghcr.io -u Caio26Gualberto`, com um **token clássico** do GitHub só com `read:packages` (GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)). É o que deixa o servidor baixar a imagem, que é privada. O token fica em `~/.docker/config.json`. Se você já tem o token do Noise Vault, ele serve aqui também.
3. Saia do SSH e entre de novo, para o grupo `docker` valer.

## 5. Os segredos do pipeline no GitHub

Em **GitHub → noazul → Settings → Secrets and variables → Actions**:

| Tipo | Nome | Valor |
|---|---|---|
| Secret | `LIGHTSAIL_HOST` | o IP estático do passo 4.1 |
| Secret | `LIGHTSAIL_SSH_KEY` | a chave privada SSH inteira, com as linhas `-----BEGIN ... KEY-----`. No Lightsail: **Account → SSH keys → Download** a chave padrão de São Paulo (`LightsailDefaultKey-sa-east-1.pem`). É a mesma do Noise Vault, se ele usa a chave padrão |
| Variable | `LIGHTSAIL_ENABLED` | `true` |

Enquanto `LIGHTSAIL_ENABLED` não existir, o workflow "Deploy API" é pulado. Por isso os pushes de antes do servidor não falham.

## 6. O pipeline que sobe a API

A API **não é compilada no servidor**: o workflow `.github/workflows/deploy-api.yml` builda a imagem no GitHub Actions a cada push na `main` que toque `api/` ou `infra/lightsail/`. Ele publica em `ghcr.io/caio26gualberto/noazul-api` (`latest` e o sha do commit) e entra no Lightsail por SSH para `git reset --hard origin/main`, `docker compose pull api` e `up -d`.

1. **Actions → Deploy API → Run workflow.** O job "Build and push image" leva uns 5 minutos na primeira vez. O "Deploy to Lightsail" termina com o `docker compose ps` mostrando os três containers de pé.
2. O compose liga `Database__MigrateOnStartup=true`: as migrations rodam no primeiro boot e em todo boot seguinte, sem efeito quando não há nada novo.
3. O Caddy tira o certificado de `api.noazul.space` sozinho na primeira requisição. Teste no navegador: `https://api.noazul.space/health` responde `Healthy`.

Se der errado, no SSH: `cd ~/noazul/infra/lightsail && docker compose logs --tail 100 api`. Variável faltando no `.env`: corrija e rode `docker compose up -d`. O `build:` continua no compose como saída de emergência: `docker compose build api && docker compose up -d api` compila no próprio servidor se o GitHub estiver fora do ar.

## 7. Backup diário do banco

No SSH:

```bash
mkdir -p ~/.aws && cat > ~/.aws/credentials <<'EOF'
[default]
aws_access_key_id = <Access key ID do noazul-backup>
aws_secret_access_key = <Secret access key>
EOF
chmod 600 ~/.aws/credentials
echo -e "[default]\nregion = sa-east-1" > ~/.aws/config
~/noazul/infra/lightsail/backup.sh
(crontab -l 2>/dev/null; echo "30 4 * * * /home/ubuntu/noazul/infra/lightsail/backup.sh >> /home/ubuntu/backup.log 2>&1") | crontab -
```

O primeiro `backup.sh` já deve imprimir `backup ok`. Confira em **S3 → noazul-backups → postgres/**. O cron roda às 4h30 UTC (1h30 em São Paulo), antes da leitura das 8h.

**Restaurar** (faz parte da verificação, não deixe para quando precisar):

```bash
cd ~/noazul/infra/lightsail
aws s3 cp s3://noazul-backups/postgres/<arquivo>.dump /tmp/restore.dump
docker compose exec -T postgres pg_restore -U noazul -d noazul --clean --if-exists < /tmp/restore.dump
```

## 8. Amplify: o site

1. **AWS Amplify (São Paulo) → Create new app → GitHub.** Autorize o GitHub e escolha o repositório `noazul`, branch `main`.
2. Marque **My app is a monorepo** e informe a pasta `frontend`. O Amplify lê o `amplify.yml` da raiz (já aponta `appRoot: frontend`, Node 24 e a saída `dist/frontend/browser`). Não há variável de ambiente: a URL da API está no `environment.ts`.
3. Nome do app `noazul`. **Save and deploy.** O primeiro build leva uns 3 minutos e publica numa URL `*.amplifyapp.com`.
4. **Hosting → Rewrites and redirects → Manage redirects.** Apague o que vier por padrão e deixe a regra da SPA, tipo `200 (Rewrite)`. Source (uma linha só) e target:
   ```
   </^[^.]+$|\.(?!(css|gif|ico|jpg|jpeg|js|png|txt|svg|woff|woff2|ttf|map|json|webmanifest|webp)$)([^.]+$)/>
   /index.html
   ```
   Sem ela, recarregar `/lancamentos` dá 404.
5. **Hosting → Custom domains → Add domain.** Digite `noazul.space` (ele não acha a zona, porque o DNS é da Cloudflare; siga em frente), com o certificado gerenciado pelo Amplify. Configure:
   - `noazul.space` → branch `main`;
   - `www` → **Redirect to** `https://noazul.space` (marque redirect, não branch).

   Ao salvar, o Amplify mostra os registros (**View DNS records**). Na Cloudflare, todos **CNAME** e **DNS only**:
   - o de verificação do certificado (nome comprido começando com `_`);
   - o da raiz: Name `@`, Target o `xxxx.cloudfront.net` que o Amplify mostra (a Cloudflare aceita CNAME na raiz);
   - `www`, com o mesmo Target.

   Leva de 10 a 30 minutos para ficar "Available". Se ficar parado em "Pending verification" por mais de uma hora, confira se algum registro ficou com a nuvem laranja.

Daqui em diante, todo push na `main` publica o site sozinho.

## 9. Primeiro acesso

1. Abra `https://noazul.space`, crie a sua conta e peça para o seu pai criar a dele.
2. **Feche o cadastro:** no SSH, troque `Auth__AllowRegistration=false` no `.env` e rode `cd ~/noazul/infra/lightsail && docker compose up -d api`. A tela de login deixa de mostrar "Criar conta".
3. Em Cartões e contas, cada um segue o guia do Meu Pluggy e conecta os próprios bancos.

O seed não cria conta de admin em produção, e o NoAzul não precisa de uma.

## 10. Verificação ponta a ponta

1. `https://api.noazul.space/health` → `Healthy`.
2. `https://noazul.space` abre; recarregar em `/lancamentos` continua na tela; `https://www.noazul.space` vai para `https://noazul.space`.
3. Login e cadastro funcionam (sem erro de CORS no console do navegador).
4. "Conectar banco" abre o guia e depois a janela da Pluggy; o banco aparece com cartões e contas em alguns segundos.
5. No dia seguinte, depois das 8h: `docker compose logs api | grep BANK_CONNECTIONS_SYNCED`.
6. `backup.log` com `backup ok` e o arquivo no S3. Restaure uma vez (passo 7).

## 11. Operação do dia a dia

**Nova versão da API:** `git push` na `main`. O workflow "Deploy API" builda a imagem, publica e troca o container; acompanhe em **Actions**. A migration roda no boot, e a API fica fora do ar os segundos da troca. **Voltar atrás:** cada imagem também sai com a tag do commit. No SSH:

```bash
cd ~/noazul/infra/lightsail
docker pull ghcr.io/caio26gualberto/noazul-api:<sha-anterior>
docker tag ghcr.io/caio26gualberto/noazul-api:<sha-anterior> ghcr.io/caio26gualberto/noazul-api:latest
docker compose up -d api
```

Migration já aplicada não se desfaz sozinha: se a versão nova criou tabela, restaure o backup antes.

**Nova versão do site:** `git push` na `main`, e o Amplify builda e publica. Para voltar atrás, `git revert` e push, ou **Redeploy this version** num build antigo do Amplify.

**Jobs:** o dashboard do Hangfire não existe em produção (`api/.claude/rules/jobs-hangfire.md`). A leitura diária aparece no log como `BANK_CONNECTIONS_SYNCED`, e cada conexão que falhar como `BANK_CONNECTION_SYNC_FAILED`.

**Logs:** `docker compose logs --tail 200 api`. Cada evento é uma linha `LEVEL data (contexto) EVENTO: mensagem | ...`. Um 500 aparece como `UNHANDLED_EXCEPTION`, com o `X-Trace-Id`. O Docker guarda até 5 arquivos de 50 MB por container.

**Espaço em disco:** `docker system prune -f` de vez em quando.

## Problemas comuns

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| `/health` falha e o log do Caddy fala em certificado | nuvem laranja na Cloudflare, ou 80/443 fechadas | registro `api` com nuvem cinza; firewall com 80 e 443 |
| "Deploy to Lightsail" falha no `docker compose pull` com `denied` | o servidor não está logado no GHCR | passo 4.2, item 2 |
| "Deploy API" nem roda | falta a variable `LIGHTSAIL_ENABLED=true` | passo 5 |
| Login dá "erro de conexão" e o console mostra CORS | `Cors__AllowedOrigins__0` diferente do endereço do site | confira o `.env` (`https://noazul.space`, sem barra no fim) e rode `docker compose up -d api` |
| Recarregar `/lancamentos` dá 404 | falta o rewrite da SPA | passo 8.4 |
| A janela da Pluggy não abre no site | o painel da Pluggy pode exigir o domínio liberado | no painel da Pluggy, procure a lista de domínios/URLs permitidos da aplicação e adicione `https://noazul.space` |
| `backup.sh` falha com `AccessDenied` | chave errada ou política sem o prefixo | passo 3 e `~/.aws/credentials` |
| Ninguém consegue criar conta | `Auth__AllowRegistration=false` | esperado depois do passo 9 |

## Checklist

- [ ] Região São Paulo no console
- [ ] Bucket `noazul-backups` com lifecycle de 30 dias e usuário `noazul-backup`
- [ ] Lightsail `noazul-api` 2 GB, IP estático, 80/443, snapshots automáticos
- [ ] `A api` → IP, nuvem cinza
- [ ] `bootstrap.sh`, `.env` preenchido, `docker login ghcr.io`
- [ ] Secrets `LIGHTSAIL_HOST` e `LIGHTSAIL_SSH_KEY`, variable `LIGHTSAIL_ENABLED`
- [ ] "Deploy API" verde e `/health` → `Healthy`
- [ ] Backup no cron, testado e restaurado uma vez
- [ ] Amplify com rewrite da SPA, `noazul.space` e `www` → redirect
- [ ] Duas contas criadas e `Auth__AllowRegistration=false`
- [ ] Bancos conectados e `BANK_CONNECTIONS_SYNCED` no log do dia seguinte
