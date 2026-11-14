---
name: abrir-pr
description: Subir mudanças do NoAzul para o GitHub. Use sempre que pedirem para dar push, abrir PR, subir as mudanças ou mandar para o GitHub. Decide entre push direto (só o dono) e branch + PR (todo mundo), roda as checagens dos projetos tocados e abre o PR preenchendo o template.
---

# Abrir PR

A `main` do NoAzul é protegida: só o dono, **`Caio26Gualberto`**, faz push direto nela. Todo o resto entra por PR, com o CI (`ci-ok`) verde e a aprovação do dono. Os merges são por squash, então o título do PR vira o commit na `main`.

## 1. Quem está pedindo

```bash
gh api user --jq .login
```

- **`Caio26Gualberto`**: é o dono. Se ele pediu push, rode as checagens do passo 3, faça o push direto na `main` e pare (sem revisor, a menos que ele peça). Se pediu PR, siga o fluxo completo.
- **Qualquer outro usuário**: nunca faça push na `main`, nem que peçam. Siga o fluxo completo.
- Se o `gh` não estiver logado, peça para a pessoa rodar `gh auth login` antes de continuar.

Se um push na `main` for recusado com `GH013` ou "protected branch", é o ruleset funcionando: não tente contornar, siga o fluxo de PR.

## 2. Branch

- Se estiver na `main` com mudanças, crie a branch antes de commitar: `git switch -c <tipo>/<assunto-curto>`, com `tipo` sendo `feat`, `fix`, `ajuste`, `docs` ou `ci`. Ex.: `feat/editar-perfil`.
- Se já estiver numa branch que não é a `main`, use ela.
- Atualize com a `main` antes de subir: `git fetch origin && git merge origin/main`. Resolva conflitos se houver.

## 3. Checagens dos projetos tocados

Veja o que mudou em relação à `main` (`git diff --name-only origin/main...HEAD` e as mudanças ainda não commitadas). Rode **só** o que o diff toca, que é o mesmo que o CI vai rodar:

| Mudou | Rodar |
|---|---|
| `api/` | em `api/`: `dotnet build NoAzul.slnx -c Release -warnaserror` |
| `frontend/` | em `frontend/`: `npm run lint`, `npm run typecheck`, `npm run build` |

Se algo falhar, corrija e rode de novo. Não abra PR com checagem falhando. Não deixe servidor, app ou container rodando depois.

## 4. Revisão antes de subir

Chame o subagente **`revisor`** (`.claude/agents/revisor.md`), informando a base (`origin/main`) e o autor (o login do passo 1). Ele pode rodar em paralelo com as checagens do passo 3. Se a sua ferramenta não tiver subagentes, faça você mesmo a revisão seguindo esse arquivo à risca, relendo o diff do zero.

- **BLOQUEADO:** corrija cada bloqueio e chame o revisor de novo. Na segunda rodada em diante, diga a ele quais bloqueios foram corrigidos e onde, para ele conferir as correções e o que elas tocaram, sem refazer tudo.
- Se um bloqueio depende do dono (decisão de produto, infra, custo), não tente resolver: pare e avise a pessoa que isso precisa ser combinado com o dono antes.
- **APROVADO:** siga. Guarde a linha de atenção do dono (se houver) e as observações para o passo 6.

Não suba nada sem o veredito APROVADO.

## 5. Commit e push da branch

- Commits em português, mensagem simples dizendo o que mudou. Vários commits na branch não tem problema: o squash junta tudo.
- `git push -u origin <branch>`.

## 6. PR

Se já existe PR aberto para a branch (`gh pr view`), o push já atualizou ele. Só ajuste a descrição se o escopo mudou (`gh pr edit --body-file`), e pule para o passo 7.

Senão, leia `.github/pull_request_template.md` e preencha **todas** as seções, num tom simples e informal, sem jargão:

- **Título:** curto e descritivo, dizendo o que foi feito. Ex.: `Criando Editar perfil no frontend`.
- **O que foi feito:** o que mudou, explicado de forma simples, mas cobrindo **toda** a mudança.
- **Fluxo:** o caminho do usuário no app, numerado. Se não muda nada pro usuário, escreva "Sem mudança pro usuário".
- **Onde mexeu:** marque `[x]` nos projetos tocados.
- **Observações do revisor:** a linha de atenção do dono (se houver) e as observações do veredito final, como vieram. Sem observações: "Nenhuma".

Tire os comentários `<!-- -->` do template. Salve o corpo num arquivo temporário e abra:

```bash
gh pr create --base main --title "<título>" --body-file <arquivo>
```

## 7. Fim

Mostre o link do PR e avise que o CI vai rodar e que o merge depende da aprovação do dono.
