# Colaboración IA — SOLQARYN

```text
PROJECT_ID=SOLQARYN
PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
CONTEXT_MODE=CURRENT_STATE_ONLY
```

## Autoridad

Toda colaboración sobre SOLQARYN parte exclusivamente de:

1. `docs/PROJECT_SCOPE_LOCK.md`;
2. `AGENTS.md`;
3. `docs/VAEP_AUTHORITY.md` cuando aplique ejecución/automatización;
4. HEAD vivo de `dev`;
5. `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md`;
6. el único Google Doc **PLAN MAESTRO SOLQARYN** para roadmap y arquitectura objetivo.

Planes, fases, filas, gates, handoffs, prompts, receipts o snapshots históricos son evidencia únicamente y no pueden gobernar trabajo nuevo.

## Forma de trabajo

- Identificar el componente y abrir sólo sus dependencias directas.
- Revalidar HEAD antes de escribir y antes de publicar.
- Un solo writer por scope material cuando aplique lease operativo.
- Implementar el cambio mínimo correcto y validar proporcionalmente.
- REVIEW_FIRST, pruebas/gates causales y P0/P1=0 se exigen cuando el flujo operativo lo requiera.
- Registrar cada changeset intencional en `CHANGELOG_AI.md`.
- Actualizar contexto/arquitectura únicamente cuando cambie la realidad vigente.

## Git y ambientes

- Trabajo ordinario: `dev`.
- No force-push, reset destructivo ni reescritura de historia compartida.
- `main`, QA/PROD, datos productivos, secretos, dominios, certificados e infraestructura productiva requieren autorización explícita vigente del propietario.
- No crear ramas adicionales ni habilitar auto-merge sin autorización.
- Preservar trabajo concurrente y resolver divergencias sin sobreescritura.

## Automatizaciones

Las diez automatizaciones canónicas y su ownership se gobiernan exclusivamente desde `docs/VAEP_AUTHORITY.md`.

Mientras permanezcan pausadas por decisión del propietario:

- no activarlas;
- no crear sustitutas;
- no eliminar ni reemplazar tareas canónicas;
- no reconstruir colas o secuencias desde planes históricos.

## Handoff mínimo

Cuando una sesión necesite continuidad, basta con dejar:

```text
Objetivo:
Scope/archivos:
HEAD/commit:
Validaciones reales:
Pendiente o bloqueo vigente:
```

El receptor debe releer el estado vivo; ningún handoff sustituye al Plan Maestro vigente, Git, CI o las autoridades canónicas.

## Regla de limpieza

No reintroducir documentos operativos paralelos, versiones numeradas del roadmap, planes locales alternativos ni snapshots históricos como fuentes de decisión. Git conserva la auditoría; el árbol vivo conserva únicamente gobierno y estado actuales.
