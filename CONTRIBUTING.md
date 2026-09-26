# Contribuir a Solqaryn

## Gate de inicio — antes de tocar código

Cada conversación/sesión nueva debe demostrar que está en el proyecto correcto.

Con acceso local:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\iniciar-sesion-ia.ps1
```

Resultado esperado:

```text
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
```

Con acceso remoto, verificar los mismos datos mediante GitHub. Si no coinciden, detenerse: solo el contexto canónico de Solqaryn autoriza cambios aquí.

Después leer únicamente:

1. `AGENTS.md`;
2. `PROJECT_CONTEXT.md`;
3. `TASKS.md`;
4. última entrada relevante de `CHANGELOG_AI.md`;
5. `PROJECT_INDEX.md` si se necesita localizar el área.

No volver a indexar todo el repositorio ni releer archivos ya documentados si no cambiaron.

## Rama de trabajo

- `main`: referencia productiva congelada.
- `dev`: **única rama de trabajo e integración autorizada**.
- No crear ramas temporales sin autorización expresa de Javier Mejía.
- PR #2 `dev -> main` es histórico y está `CLOSED + MERGED`; no reabrirlo. Un nuevo PR/merge hacia `main` requiere autorización nueva y explícita de Javier Mejía; auto-merge permanece deshabilitado.

## Preparación local

Después del gate, si el checkout está limpio y detrás de remoto:

```bash
git fetch origin
git switch Desarrollo
git pull --rebase origin Desarrollo
git status --short --branch
```

Si hay cambios locales ajenos, preservarlos y resolver el conflicto explícitamente.

## Implementación

- Cambios pequeños y localizados.
- Archivo objetivo + dependencias directas.
- Evitar refactors no solicitados.
- No tocar Producción.
- No subir secretos ni temporales.
- Usar únicamente contexto canónico y verificable de Solqaryn.
- Actualizar memoria/arquitectura solo cuando el cambio realmente la invalide.

## Evidencia obligatoria

Cada changeset debe:

1. incluir una entrada breve en `CHANGELOG_AI.md`;
2. actualizar `TASKS.md` si cambió el estado/pendiente;
3. actualizar contexto/índice/arquitectura solo cuando exista cambio real;
4. actualizar colaborativos si cambian reglas/accesos/gobierno;
5. reportar validaciones reales y SHA publicado.

El hook `pre-commit` local bloquea commits fuera de Solqaryn/`dev` y commits sin `CHANGELOG_AI.md`.

## Validación

Aplicar la validación proporcional de `AGENTS.md`.

Para cambios documentales/gobierno local no se exige build de aplicación. Para cambios transversales, seguridad, persistencia, migraciones o cierre formal de fase, ejecutar la suite aplicable completa.

Un commit puede usar `[skip ci]` únicamente bajo la regla estricta de cambios administrativos/locales definida en `AGENTS.md`.

## Publicación

1. revisar diff;
2. comprobar repo/rama/HEAD;
3. commit descriptivo;
4. push a `origin/dev`;
5. handoff compacto con evidencia.

## Rendimiento

- No repetir `fetch/pull/status` sin necesidad.
- No releer archivos sin cambios.
- No escanear módulos no relacionados.
- Tras reconexión, recuperar estado; no reiniciar análisis.
- Si una tarea puede terminarse con menos archivos/comandos, preferir esa ruta sin sacrificar validación.

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


