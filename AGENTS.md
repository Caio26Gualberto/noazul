# AGENTS.md — NoAzul

Instruções para qualquer agente de IA (Cursor, Codex, Copilot e outros) neste repositório.

1. As convenções do projeto estão em `CLAUDE.md` (raiz) e no `CLAUDE.md` de cada projeto (`api/`, `frontend/`), com as decisões em `<projeto>/.claude/rules/`. Valem para qualquer agente, não só para o Claude: leia antes de mexer.
2. **Push e PR:** sempre que pedirem para dar push, abrir PR ou subir mudanças, siga `.claude/skills/abrir-pr/SKILL.md`. A `main` é protegida: só o dono (`Caio26Gualberto`) faz push direto; todo o resto entra por PR.
3. **Um assunto por branch:** veja "Como o agente trabalha aqui" no `CLAUDE.md`. Pedido sem relação com a branch atual → commite o pendente e crie uma branch nova a partir da `main` antes de começar.
4. **Revisão:** antes de subir, o diff passa pelo revisor descrito em `.claude/agents/revisor.md` (a skill de PR explica como). Use o modelo mais forte disponível.
5. A fonte da verdade das skills e agentes é `.claude/skills/` e `.claude/agents/`. Os de `.agents/` só apontam para lá, para outras ferramentas os encontrarem.
