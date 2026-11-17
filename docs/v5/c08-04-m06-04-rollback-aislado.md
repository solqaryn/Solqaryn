# C08-04.M06.04 — Ensayo seguro de reversión de entrega test-only

**Autor:** DEV04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`), `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R2 por ensayo de recuperación y cambios de GitHub Actions. **Alcance estricto:** versionado de tests/documentación; sin módulos realmente habilitados, base de datos, migraciones, API ni proveedor conectado.

## A0 y plan de rollback

El lote C08-04.M05 demuestra por código y UI que los módulos sectoriales visibles están **No conectado**. No existe una activación real ni una operación de desactivación productiva que pueda revertirse de forma honesta. Por tanto el ensayo ejecutable, realmente no destructivo, revierte en un **worktree efímero de GitHub-hosted runner** el commit histórico exacto M05.05 de pruebas de interfaz `3a5aa69c38b716336da150650fdf881ebb5320f8` sin crear commits, cambiar `dev` ni tocar bases de datos. El script usa `git revert --no-commit` sólo dentro del worktree temporal, verifica que su árbol de índice coincide exactamente con el árbol del padre del commit y elimina el worktree al salir. No usa `reset`, `force-push` ni reescritura de historia.

Los únicos cuatro archivos que la reversión puede modificar en el worktree son `.github/workflows/v18-c08-04-m05-05-negative.yml`, `CHANGELOG_AI.md`, `docs/v5/c08-04-m05-05-negative-acceptance.md` y `frontend/e2e/c08-04-m05-05-negative.spec.ts`. Se exige que ningún archivo de `backend/src`, `frontend/src`, migraciones ni datos cambie. El ensayo NO valida un rollback real de código de producto ni restauración MySQL; esas capacidades siguen **NO_CERTIFICADO** y requerirían entorno/contratos adicionales.

## A1 – A3, pruebas falsables y límites

El workflow `v18-c08-04-m06-04-rollback.yml` se dispara por `dev` y ejecuta dos pruebas dirigidas en Ubuntu GitHub-hosted: (1) negativa: entrada con SHA no aprobada se rechaza con exit 64; (2) positiva/frontera: `git revert --no-commit` del SHA aprobado en worktree efímero, comparación **exacta** con el árbol original del padre, lista exacta de cuatro paths y veto de rutas runtime/datos. Ambos tests deben realmente ejecutar con exit 0 en SHA publicado; un `skipped` no es PASS. El script no lee secretos de proveedor ni datos de tenants. JWT/RBAC, A/B real, MySQL 8.4 y desactivación de módulos no son aplicables a este diff test-only y siguen sin certificar para producción. Un error de checkout, SHA, parent tree o ruta inesperada hace fallar el job y conserva VALIDANDO.

## A4–A5, observabilidad y continuidad

Exigir checks del SHA publicado `verify-solqaryn-project-scope`, `V5 - Sin encabezados vacios ni atribucion cruzada`, backend, seguridad y el job de ensayo en `completed/success`, además de readback PLAN/COLA/BITACORA. Preservar el historial de CI fallida y no contar `queued`, `in_progress`, `cancelled` o `skipped` como PASS. En caso de fallo, reparar por hipótesis nueva sin degradar gates. Sólo después cerrar LISTO_DEV con A0–A4 reales y solicitar A5 independiente de otra Task Javier. La tarea no activa ni desactiva módulos reales, no acredita eliminación/restitución de datos legítimos en MySQL y jamás autoriza QA/main/PROD. **Rollback del propio changeset:** commit `git revert` ordinario, sin reset ni force-push.
