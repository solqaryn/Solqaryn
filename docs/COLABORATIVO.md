# Espacio de Coordinación Colaborativa — Solqaryn

Este documento define el handoff entre Javier Mejía, Codex, AntiG/Antigravity y ChatGPT. El estado técnico vivo se consulta en Git, `TASKS.md` y `CHANGELOG_AI.md`; no se congelan aquí cifras de tests/builds que puedan quedar obsoletas.

## 1. Identidad y gate de sesión

```text
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
```

Toda conversación/sesión nueva comienza confirmando esos tres valores. Si alguno no coincide, no se modifica nada con estas reglas.

Con acceso local: `scripts/iniciar-sesion-ia.ps1`.

Con acceso remoto: comprobación equivalente mediante GitHub.

## 2. Fuentes canónicas

- `PROJECT_CONTEXT.md`: contexto técnico/identidad.
- `PROJECT_INDEX.md`: índice dirigido.
- `ARCHITECTURE.md`: arquitectura.
- `TASKS.md`: pendientes.
- `CHANGELOG_AI.md`: evidencia de cambios.
- `AGENTS.md`: reglas obligatorias.

Si existe contradicción entre texto histórico/memoria y estas fuentes, prevalecen la evidencia Git y las fuentes canónicas más recientes de `dev`.

## 3. Equipo y acceso

| Integrante | Proyecto local PC | GitHub | Rol principal |
|---|---:|---:|---|
| Javier Mejía (`jmejia31`) | Sí | Sí | Propietario/decisión final y Owner secundario/de recuperación de la organización `solqaryn` |
| Codex | Sí, sólo cuando Javier lo autoriza | Sí | Fuera del flujo por defecto; implementación/pruebas por orden explícita |
| AntiG / Antigravity | No operativo por defecto | Sí | `RESERVED_INACTIVE`; sin scheduler, handoff ni certificación |
| ChatGPT | No | Sí, con conector autorizado | Arquitectura/revisión/coordinación/cambios remotos |
| Alex Morales (`morales35alex`) | Según su entorno | Sí | Colaborador externo `write`; no Owner/Admin organizacional |
| Otros agentes | No por defecto | Solo con conector autorizado | Según asignación |

Nadie debe asumir acceso local que no esté documentado.

## 4. Aislamiento entre proyectos

- Esta coordinación aplica exclusivamente a Solqaryn.
- Usar únicamente rutas, ramas, planes y decisiones verificadas de Solqaryn.
- Una conversación no cambia de proyecto por inferencia.
- Un cambio explícito de proyecto obliga a ejecutar el gate del proyecto destino antes de escribir.

## 5. Rama y entornos

- `dev`: única rama de trabajo.
- `main`: congelada.
- PR #2: histórico, `CLOSED + MERGED`; no reabrir. Cualquier nuevo merge a `main` exige autorización nueva y explícita.
- No ramas temporales.
- No auto-merge.
- Producción no se modifica.

## 6. Evidencia y handoff mínimo

Cada changeset registra `CHANGELOG_AI.md`. Si cambia el estado operativo, también `TASKS.md`.

Handoff:

```text
Proyecto confirmado:
Agente:
Objetivo:
Archivos/área:
Validaciones reales:
Commit:
Pendiente/bloqueo:
```

No repetir arquitectura completa; referenciar `PROJECT_CONTEXT.md`.

## 7. Protocolo FULL FLASH / bajo consumo

1. Gate de identidad.
2. Leer memoria canónica una vez.
3. No releer archivo si no cambió.
4. No reindexar repositorio por prompt.
5. Analizar objetivo + dependencias directas.
6. Usar búsquedas dirigidas.
7. Validación proporcional.
8. Changeset coherente, evitando microparches sin valor.
9. Tras reconexión recuperar estado; no repetir diagnóstico.
10. Detener exploración al resolver objetivo.
11. Actualizar documentos solo cuando cambie su contenido real.

## 8. Guardrails locales

- `.githooks/pre-commit`: bloquea repo/rama incorrectos y exige `CHANGELOG_AI.md`.
- `.githooks/post-commit`: auto-push solo si `origin` es Solqaryn y la rama es `dev`.
- `scripts/iniciar-sesion-ia.ps1`: diagnóstico corto de sesión.

## 9. Mejora continua

Las mejoras de bajo riesgo y directamente relacionadas pueden aplicarse. Las transversales se registran en `TASKS.md` para ejecución controlada. El objetivo es reducir latencia, tokens, trabajo repetido y errores sin sacrificar validación ni trazabilidad.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Regla vinculante: este archivo solo puede interpretarse con contexto de SOLQARYN. Está prohibido consultar o usar skills, documentación, chats, repositorios, memorias o reglas fuera de SOLQARYN salvo autorización explícita del propietario para la fuente/alcance concreto o una entrada `ACTIVE` en la allowlist versionada. La disponibilidad técnica no equivale a permiso. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/Solqaryn`. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.


